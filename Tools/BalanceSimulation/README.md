# Isolated balance simulation

[Project documentation](../../Docs/Home.md) · [Report storage](../../Reports/BalanceSimulation/README.md)

The simulator lives outside `Assets`. Unity CLI compiles its external C# adapters
on demand in the open Editor. Production combat runs at its normal 1/60-second
step in a tight loop, using disposable preview-scene actors. The adapters read
scene/definition inputs, restore Random state and do not use profile saves.
They do not become part of the game or its player build.

## Entry points

Use PowerShell 7. Open MainScene in **Edit Mode**, with Unity Pipeline connected.
The `unity` executable must be on PATH, or pass `-UnityCli` with its absolute path.
Run from the project root:

```powershell
# Three current damage policies; allocate points, claim/use rewards, follow up to 20 locations.
./Tools/BalanceSimulation/RunCampaign.ps1 -RunsPerStrategy 10
# Five balanced policies, including all three intelligence elements.
./Tools/BalanceSimulation/RunCampaign.ps1 -Preset Tools/BalanceSimulation/Presets/balanced-push.json -RunsPerStrategy 10
# Adapt the tree after defeats; five policies, five seeds each.
./Tools/BalanceSimulation/RunCampaign.ps1 -Preset Tools/BalanceSimulation/Presets/adaptive-balanced-push.json -RunsPerStrategy 5
# Fifteen balanced campaigns: five each for strength, dexterity, intelligence/lightning.
./Tools/BalanceSimulation/RunCampaign.ps1 -Preset Tools/BalanceSimulation/Presets/balanced-shops.json -RunsPerStrategy 5

# Independent fixed-level fights: strength, dexterity and intelligence/lightning starters.
./Tools/BalanceSimulation/Run.ps1 -RunsPerBuild 10
./Tools/BalanceSimulation/Run.ps1 -RunsPerBuild 20 -Stage 5 -PlayerLevel 6 -BatchSize 5

# Read-only graph/content catalog; output stays in the report directory.
./Tools/BalanceSimulation/Run.ps1 -ExportCatalog Reports/BalanceSimulation/catalog.json

# Rebuild an existing viewer with the current template; no Unity required.
./Tools/BalanceSimulation/RebuildReport.ps1 -Report Reports/BalanceSimulation/RUN/report.json.gz

# Assemble completed saved campaigns after a failure; no Unity required.
./Tools/BalanceSimulation/Recover.ps1 -WorkDirectory Reports/BalanceSimulation/RUN/report.json.work
# An incomplete result requires explicit opt-in, is visibly labelled, and retains saved work.
./Tools/BalanceSimulation/Recover.ps1 -WorkDirectory Reports/BalanceSimulation/RUN/report.json.work -AllowPartial
```

Replace `RUN` with a real folder. Default outputs use dated folders under
`Reports/BalanceSimulation`. `-Output` accepts a `.json` or `.json.gz` filename
under that root; final data is always stored as `.json.gz`. Runners reject existing
outputs/work folders to prevent mixing executions. `RebuildReport.ps1` intentionally
replaces the selected viewer/archive and migrates legacy raw JSON only after equality
validation. It preserves measured results and their original timing metadata.

## Ownership and structure

| Location | Responsibility |
| --- | --- |
| `Run.ps1`, `RunCampaign.ps1` | Public runners; batching, progress and orchestration |
| `Recover.ps1`, `RebuildReport.ps1` | Offline recovery and viewer regeneration |
| `Presets/damage-push.json`, `Presets/balanced-push.json` | Damage/balanced policies; `adaptive-balanced-push.json` enables defeat-driven search |
| `Runtime/FixedBattle.cs`, `Runtime/Campaign.cs` | External production-combat adapters |
| `Runtime/EditorEffectOwnership.cs` | Own temporary effect modifiers safely in Edit Mode |
| `Runtime/EditorExports.cs` | Atomic direct JSON exports with small CLI acknowledgements |
| `Internal/UnityBridge.ps1` | Configuration literals, CLI transport and adapter cache |
| `Internal/Storage.ps1` | Paths, atomic writes, archive reads and completed-work cleanup |
| `Internal/Reports.ps1`, `Internal/AssembleCampaignReport.ps1` | Compression, HTML/index and campaign summaries |
| `Templates/Report.html` | Offline stats/tree viewer |
| `Scratch/` | Ignored temporary CLI source files; removed after each command |
| `Reports/BalanceSimulation/` | Final results and unfinished run data |

Adapters are cached by source hash in the Editor AppDomain. Subsequent chunks
compile a small dispatcher rather than the whole adapter. Source changes or a
domain reload recreate the cache. The delegate retains code, not simulation actors.
Campaign checkpoints contain data only; they are discarded when the runner exits.
Effect-owned transient modifiers need explicit ownership because production effects
normally destroy them through deferred `Destroy`, which is inappropriate in Edit Mode.

Large payloads are written directly from Unity to a unique temporary file and
atomically renamed. The CLI receives only an acknowledgement. If acknowledgement
fails after a completed write, the wrapper reads the complete file. This avoids
transmitting large catalogs/campaigns through Pipeline's response channel.

## Modes and limits

Campaigns start from the scene player's authored level/XP/point defaults. Actual
kills award XP; point spending uses numeric nodes, authored links, activity and
zone rules. Policies use attribute/damage priorities with optional two-edge
lookahead. The default preset is strength/physical damage, dexterity/physical
damage and intelligence/lightning damage. It permits 20 failures per stage and
180 simulated seconds per wave across an ordered 20-location route. Repeated
failures retain earned XP and point budget; adaptive search can redistribute investment. The balanced preset gives offensive and defensive node categories equal weight
while retaining attribute/family priorities and lookahead. These are greedy policies, not optimal
build searches. A requested route does not mean every location was reached.

Location progress enforces unlocks, wave quotas and first-completion rewards.
Same-stage waves preserve combat state; stage/retry boundaries reset it. Supported
simple items are consumed; unsupported items remain in inventory. The reward
adapter mirrors the current completion-window insertion/claim behavior, including
the recorded inventory-full risk [BL-001](../../Docs/Backlog.md).

Fixed battles use explicit node IDs/point budgets and check authored root paths
and zone limits. They exclude XP, rewards and progression. Supply a JSON `-Preset`
with `schemaVersion: 1`, unique `builds` (`name`, `nodeIds`, `pointBudget`) and
`scenarios` (`name`, `locationId`, `stage`, `playerLevel`, `waveNumber`, `maxSeconds`).
Both modes use exact numeric BaseModifier/ModifierPerPlayerLevel implementations.
Campaigns additionally support socket allocation and numeric LocalModifiers gems,
shop purchases and gold completion rewards (see below). Fixed battles still exclude
sockets and shops. Infinite nodes, wisps, special/reactive modifiers, influence/bridge
gems, mini-games and UI-driven decisions are outside current coverage. Catalog support
flags are mode-specific. Unsupported content must not be interpreted as balanced.

## Campaign shops and local gems (2026-10-08)

At stage boundaries, bots fill active empty sockets with supported inventory gems.
If none is available, they visit an unlocked shop using production location selection,
buy affordable in-stock gems through `ShopService.TryBuy`, return to the battle
location/stage, and insert through `InventorySocketService.TryInsertGem`. They do
not farm extra gold or buy without an empty active socket. Existing gems are not
replaced. The current choice uses projected build utility (see policy v2 below);
shop offers are ranked by positive estimated gain per gold.
Socket allocation receives the best available inventory/affordable-shop gem score
in addition to its travel value. This heuristic does not prove optimal placement.

Gold, purchased stock, inventory and socket instances persist in isolated campaign
checkpoints. Socket modifiers enter production `MainSkillTree.CollectAllModifiers`.
Snapshots include gold and socket contents; `shopLog` records visits, transactions
and insertions. The viewer exposes both the log and snapshot socket contents.
Gold completion rewards now credit gold on a successful claim rather than accessing
a null item. Costs and point budgets preserve the current floating-point contract.
No scene, content asset or profile save is edited by this adapter.

Verification: the adapter compiled against installed Unity/game assemblies with
zero errors and ran through live Unity CLI. The [15 balanced campaigns](../../Reports/BalanceSimulation/20261008-185017-292-campaign/verification.md)
completed on seeds 101–105, five each for strength, dexterity and intelligence/lightning.
They bought and inserted 53 gems (strength 2, dexterity 51, intelligence 0).
All intelligence bots stopped before completing Level3, so Shop1 remained locked.
The final gold and socket counts match the last snapshots for all 15 unique bots.
UI navigation, influence/bridge gems and adaptive gem respec behavior were not verified.

Pipeline has a five-second operation/response limit. Campaign
`-ComputeBudgetMs` (250–2000, default 1500) stops only at complete stage boundaries;
a heavy single stage can still exceed it. Fixed `-BatchSize` (default 25) also caps
each batch at 75 fights; reduce it for costly encounters. Compute speedup measures
combat time, while end-to-end speedup includes CLI overhead; neither is a gameplay
FPS measurement. Simulation consumes Editor CPU while running.

## Failure recovery

Campaign work is saved under `report.json.work/`: `run.json` metadata, `catalog.json`
and one atomic `campaign-NNNN.json` envelope per completed bot. No second expanded
copy is kept. The graph is exported before advancing so saved bots can be inspected
after a later failure. Complete publication removes this work folder; failures
retain it. Recovery validates expected strategy/seed pairs and finished states,
then assembles completed exports. It does **not** resume a partially played bot
or an Editor checkpoint after domain reload. Partial reports omit throughput
metrics because saved campaigns do not account for all work in that run.

Fixed-battle failures retain batch exports in the same work-folder convention for
manual inspection; `Recover.ps1` currently supports campaigns only. Failed-command
source files in Scratch are cleaned in `finally`.

## Verification record, 2026-10-04

The reorganized fixed adapter compiled through Unity CLI and ran three starter
fights. The campaign adapter compiled and ran the same three policies on a bounded
two-location route with one attempt per stage. All reached stage 6 / the second
location, allocated earned points and used simple rewards. Successful publication
removed their work folders. No new test suite, game build or content edits were
performed. The temporary verification report folders were removed afterwards.

The former 30-bot damage campaign was used for a same-data storage comparison:
raw JSON + inline JSON HTML occupied 53,278,224 bytes; gzip + embedded gzip HTML
occupied 1,999,153 bytes (96.25% less). JSON values, all snapshots and graph records
matched. The owner subsequently deleted the historical run folders before requesting
new balanced campaigns. These figures are historical; their raw artifacts are gone.
Browser appearance/live viewer interaction remain unverified.

## Latest balanced comparison

[50 balanced campaigns, 2026-10-04](../../Reports/BalanceSimulation/20261004-124706-435-campaign/verification.md)
ran the five-policy balanced preset on seeds 101–110. It recorded 8,071 waves and
reached 12 locations. Dexterity's best cleared stage was 79; two cold bots exhausted
the supported numeric frontier with points remaining, so their result includes a
policy coverage boundary. Full source/defaults, timing and data checks are recorded
with the report. This is current run evidence, separate from historical annotations.

## Adaptive allocation after defeat

`Presets/adaptive-balanced-push.json` enables `adaptive: true`. The runner schedules
a controlled search after a failed normal stage, for up to `adaptiveRoundsPerStage`
rounds (default 3), with up to `adaptiveCandidates` proposals each (default 4).
Normal `maxAttemptsPerStage` remains 20. Once search rounds are exhausted, ordinary
retries continue with the best committed tree and any subsequently earned XP.

Each round first replays the existing tree at the post-failure level/XP and free
points. Candidates keep the same starter, inventory, unlocked nodes and permanent
power, and the same total skill-point budget (free points plus invested node costs).
Odd proposals replace approximately the final third of the allocation order; even
proposals rebuild after the starter. Priorities rotate through health/armour/resistance,
offence, barrier/evasion/resistance and attack-speed/crit/accuracy. A deterministic
per-node score perturbation produces further combinations. All newly bought nodes
still use `CanBeAllocated`, `HasEnoughSkillPoints`, authored connectivity and zone
constraints. This is a free **experimental respec** on disposable copies; it does
not model the UI refund queue or impose a gameplay refund price.

A trial freezes level/XP and allocation during combat. Enemy experience rewards are
zero; gold, campaign wave counts, location progress, inventory and rewards are not
changed. The round fixes the same wave-generation/combat seeds for all proposals.
Selection prefers a complete stage victory, otherwise more completed waves plus
fractional enemy health removed in the final wave. Equal-score candidates retain
the incumbent. Duplicate node sets are logged and skipped. The best tree/remaining
points and its growth priorities are committed; normal progression replays the same
wave-generation seed and records whether the improvement is confirmed. Production
XP growth and drop RNG in that normal replay can differ from the frozen trial, so
a probe victory is distinct from a confirmed normal-stage victory.

Successful layouts persist across stages; later earned points grow from them.
Node-specific power/unlocks remain attached to their nodes through respecs.
Unsupported sockets/special modifiers remain excluded and can limit the search.
A better local stage result does not prove an optimal campaign build.

Campaign schema 2 has additive `searchLog`, `trialWaves`, `trialSimulatedSeconds`,
`allocationRevision` and `learnedFocus` fields. Snapshots distinguish probes from
normal progression, include exact `buildOrder`, revision and search-log cursor.
Allocation logs retain earlier revisions; refit revisions record the installed tree
and its node costs. `allocationCount` is now a log cursor, not a node-count proxy.
The viewer displays the selected revision, exact trial composition, evaluation
scores/deltas, added/removed counts, commit and confirmation events; selecting an
evaluation opens its stats/tree snapshot. Probe wave counts/time are separate from
normal progression, and speedup includes both kinds of simulated combat.

## Latest adaptive comparison

[25 adaptive balanced campaigns](../../Reports/BalanceSimulation/20261004-135038-237-campaign/verification.md)
executed all five policies on seeds 101–105. Search evaluated 681 nonduplicate
candidate layouts and committed 156 changed trees; 41 normal-stage wins followed
changed-layout commits. Same-cohort mean cleared stage changed strength 17→25,
dexterity 51→42, and each element remained 14. This first local-stage search is
functional but not a general progression improvement. The report separates frozen
trial victories, unchanged-incumbent confirmations and normal-stage confirmations.

## Counterfactual decision policy v2 (2026-10-08)

The campaign adapter now projects candidate numeric modifiers through production
`StatCalculator` on a disposable decision actor. This preserves attribute thresholds,
Added/Increased/More arithmetic, player-level scaling, source power and aggregate
expansion. Projected node paths also replace affected BonusZone contributions,
including correct replacement of More factors. The current actor's combat state,
XP, wallet and tree allocation are not changed during evaluation.

A deterministic representative final wave of the current stage supplies opponent
stats (including the boss override when applicable). Generation runs with isolated
Random state, zero XP and no combat/drop callbacks. These are representative authored
threats, not a prediction of the exact next random encounter. A separate deterministic
metric estimates offensive output and survival: damage families, accuracy/evasion,
armor, capped resistance and penetration, crit layers, block/parry, healing,
lifesteal masks, barrier charges/mask/regeneration and mystic negation/cleanse.
Ailment/control contributions and encounter duration are bounded approximations.

Candidates are ranked by weighted logarithmic gain relative to the current build:
balanced gives attack and defence equal weight; damage/defence styles use 80/20.
Existing adaptive focus can shift these weights. The prior attribute/family heuristic
only chooses a shortlist of eight frontier nodes. Up to three linked nodes are
projected together, branching to two second nodes and one third node; future scores
are discounted and divided by total point cost. Execution still checks production
allocation predicates. Locked nodes are excluded from projected paths, but future
zone constraints are not fully solved; this remains a bounded greedy search.
Points are retained if the shortlist has no positive immediate/path utility.

Inventory and shop gems must have positive projected utility. Shop offers use gain
per gold and actual unlocks, affordability and stock. The baseline is refreshed after
each insertion, so saturation and diminishing returns affect the next purchase.
Capacity alone yields no barrier value without integer charges and matching damage
mask; its separate contribution to MysticNegation is retained against mystic damage.
Zero chance/power prerequisites, healing masks and stat caps are respected by the
metric. Already equipped gems are not replaced; influence/bridge gems remain excluded.
Allocation and purchase logs record `decisionPolicy`, `decisionScore`,
`estimatedOffenceGain` and `estimatedDefenceGain`. These are projections, not measured
battle improvements. The viewer labels them as forecasts. Old reports retain their
original choices and may show no forecasts.

Verification: the full adapter compiled against installed Unity/game assemblies
with zero errors (Unity Roslyn csc.dll). Scoped in-memory metric checks passed for
capacity without charges/negation, useful active barriers, capacity/negation synergy
without charges, and BlockChance saturation at 0.9 (float tolerance). No new test
files or suite were added. Live v2 execution was subsequently checked in
[15 balanced campaigns](../../Reports/BalanceSimulation/20261008-192754-223-campaign/verification.md):
mean cleared stages were strength 51, dexterity 95 and intelligence/lightning 15.
All 64 purchases had positive v2 scores; no capacity gems were purchased. Final
snapshots and temporary-object cleanup were checked. An algorithm-only comparison
and a matched throughput benchmark remain unverified: 238 attribute nodes also
changed from 2 to 1 between cohorts. The preceding
15-bot dataset used v1; it is not evidence for v2 or an improvement comparison.
This policy does not learn across campaigns or save model parameters. Persistent
learning would require a separate training loop, progression/cost-based rewards,
and evaluation on seeds excluded from training.
