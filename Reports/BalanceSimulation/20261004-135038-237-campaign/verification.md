# Adaptive balanced campaigns, 2026-10-04

[Interactive candidate stats/tree viewer](report.html) · [Summary](report.md) · [Paired comparison data](comparison.json) · [Runner](../../../Tools/BalanceSimulation/README.md)

## Executed scope

```powershell
./Tools/BalanceSimulation/RunCampaign.ps1 -Preset Tools/BalanceSimulation/Presets/adaptive-balanced-push.json -RunsPerStrategy 5 -Seed 101 -ComputeBudgetMs 1000 -UnityCli C:/Users/seldp/AppData/Local/Unity/bin/unity.exe
```

25 campaigns: strength, dexterity, intelligence/fire, intelligence/lightning,
intelligence/cold; seeds 101–105 for every policy. Route: 20 requested locations;
20 normal failures per stage and 180 simulated seconds per wave. Adaptive search:
up to four candidates plus the incumbent per round, three rounds per stage.
After that budget, normal retries retain the chosen tree and earned XP.

The search alternates replacing the final third of the allocation order and
rebuilding after the same starter. Health, offence, barrier and speed priorities
rotate, with deterministic node-score perturbations. All new purchases still use
production allocation/zone predicates. A free experimental respec is used only on
isolated copies; this does not exercise the player refund queue or alter gameplay.

Trials within a round use identical level, XP, total points and wave seeds, preserve
node-specific unlocks/power, and award no XP, gold, items or location progress.
Complete victory wins the selection; otherwise the score is completed waves plus
enemy health removed in the final wave. The best layout/priorities persist. Normal
progression then replays the generation seed to confirm the result with production
XP/drops enabled. A successful trial is separate from normal confirmation.

## Paired progression comparison

The earlier [balanced baseline](../20261004-124706-435-campaign/verification.md)
contains ten seeds; this comparison uses only 101–105, matching the new cohort.
Numbers are mean highest completed stage, not the failed stage.

| Policy | Baseline mean | Adaptive mean | Baseline best | Adaptive best |
| --- | ---: | ---: | ---: | ---: |
| Strength | 17 | 25 | 29 | 29 |
| Dexterity | 51 | 42 | 79 | 44 |
| Intelligence/fire | 14 | 14 | 14 | 14 |
| Intelligence/lightning | 14 | 14 | 14 | 14 |
| Intelligence/cold | 14 | 14 | 14 | 14 |

Strength pairs (101–105): 9→29, 29→19, 9→29, 9→29, 29→19.
Dexterity pairs: 44→44, 44→39, 44→44, 79→44, 44→39.
All elemental pairs were 14→14. No full requested route completed.

The initial adaptive version improves mean strength progression, regresses
dexterity, and does not move the elemental stage ceiling. It optimizes the current
stage and retains its scoring priorities for later growth, so local improvements
can harm later progression. These paired campaign seeds compare policy outcomes;
confirmation reuses a failed seed while the baseline changes seed on ordinary
retries. They are not a strict common-random-number experiment for the entire
campaign. Do not attribute every progression difference to one individual refit.

## Search and coverage

- Normal progression: 3,535 waves; search: 2,683 probe waves.
- 4,760 reset-boundary snapshots, with exact tree order/revision and probe flags.
- 288 search rounds / 1,063 evaluations including 288 incumbents.
- 681 nonduplicate candidate evaluations; 94 duplicate proposals skipped.
- 156 changed layouts committed.
- 98 normal-stage victories after search; 41 were after a changed-layout commit.
  The other confirmations include unchanged incumbents that already benefited from
  XP earned before the search. This distinction avoids crediting all 98 to refits.
- 51 candidate evaluations won their frozen stage; trial wins are not the same
  metric as later normal-stage victories.
- Eight actual locations reached (`level-1` through `level-8`).

The bounded pilot also compiled/executed before the full run: strength seed 101
failed stage 7 with incumbent fitness 2.7776257 at level 14 / point budget 15.
A two-node substitution with health priority won all three waves (fitness 3),
then normal progression confirmed victory. The temporary pilot folder was removed
once its evidence was recorded here.

Gems, sockets, infinite nodes, wisps and special/reactive node modifiers remain
unsupported. Item use retains the existing simple-item subset. This is current
numeric policy coverage, not a complete full-game or optimal-build search.

## Timing and storage

Normal simulated combat: 50,784.503 seconds; probe combat: 36,917.469 seconds.
Compute: 816.073 seconds; command-inclusive execution: 1,454.871 seconds (24.25
minutes), 678 actual CLI calls. Final PowerShell assembly/compression is excluded
from that execution timer. Speedup includes both kinds of simulated combat:
107.47x compute / 60.28x command-inclusive; these are not gameplay FPS measurements.

Data archive: 2,312,120 bytes; self-contained HTML: 3,099,266 bytes. Both contain the
same compressed payload. The completed work directory was automatically removed.
Comparison JSON and Markdown evidence are small additions, not duplicate datasets.

## Verification actually performed

- Unity CLI `eval_file` dynamically compiled/executed the new external C# adapter;
  the pilot and all 25 requested campaigns completed successfully.
- Every expected strategy/seed pair is present exactly once; `isPartial` is false.
- All 1,063 trial records match their round's level, XP, point budget and seed.
  Node costs plus remaining points equal the budget. Trials retain the incumbent's
  campaign wave, reward and allocation-log cursors, confirming no probe progression.
- Every round has an incumbent, commit and normal confirmation; committed fitness
  is never worse than the frozen incumbent. Probe/commit outcomes remain separate.
- Final snapshots match level, free points, current node order and log cursors.
  All snapshot cursors stay inside their logs; final snapshots are normal progression.
- Embedded archive bytes equal `report.json.gz`; generated JavaScript and PowerShell
  syntax were checked. No persistent automated test files were added.
- Unity reported zero temporary simulation actors and zero nonpersistent modifiers;
  one preview scene (existing baseline), Edit Mode, clean scene, source player still
  level 1 / one skill point. Source gameplay/scene/profile data were not edited.
- No player build or unrelated test suite ran. HTML appearance and live interactions
  remain unverified because local-file browsing was blocked during viewer work.
