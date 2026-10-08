# Five balanced-build campaigns, 2026-10-04

[Interactive stats/tree report](report.html) · [Summary](report.md) · [Report directory](../README.md)

## Executed scope

Command from the project root (the installed Unity CLI path was passed explicitly):

```powershell
./Tools/BalanceSimulation/RunCampaign.ps1 -Preset Tools/BalanceSimulation/Presets/balanced-push.json -RunsPerStrategy 10 -Seed 101 -ComputeBudgetMs 1000 -UnityCli C:/Users/seldp/AppData/Local/Unity/bin/unity.exe
```

Five policies: strength/physical, dexterity/physical and intelligence/fire,
intelligence/lightning, intelligence/cold. All use `style: balanced`, equal category
weights for offence/defence, attribute/family priority and two-edge lookahead.
Ten seeds per policy (101–110), an ordered 20-location route, up to 20 failed
attempts per stage and 180 simulated seconds per wave. Scene source defaults were
level 1 / one skill point. Starter SaveIds and modifiers were inspected in MainScene
before execution. Game content, scene and profile saves were not edited.

All 50 campaigns completed execution and stopped at the configured stage retry
limit; none completed the full requested route. Repeated failures retain earned XP,
point allocation and claimed/used simple rewards. Results are greedy-policy evidence,
not an optimal-build search or complete Play Mode gameplay coverage.

## Results

| Policy | Mean stage cleared | Best stage cleared | Mean final level | Waves | Rewards claimed / used |
| --- | ---: | ---: | ---: | ---: | ---: |
| Strength balanced | 17 | 29 | 46.5 | 1,188 | 36 / 36 |
| Dexterity balanced | 58 | 79 | 111.9 | 3,345 | 96 / 96 |
| Intelligence fire balanced | 14 | 14 | 53.9 | 1,237 | 30 / 20 |
| Intelligence lightning balanced | 14 | 14 | 51.0 | 1,008 | 30 / 20 |
| Intelligence cold balanced | 15 | 19 | 55.3 | 1,293 | 32 / 22 |

Total: 8,071 waves, 6,268 reset-boundary snapshots and the 2,812-node graph catalog.
Twelve actual locations were reached (`level-1` through `level-12`). Cleared stage
79 means the strongest bot then attempted stage 80; it does not mean stage 80 won.

Stop distributions:

- Strength: six seeds stopped on `level-2/10`, four on `level-6/30`.
- Dexterity: six seeds stopped on `level-8/45`, four on `level-12/80`.
- Fire and lightning: all ten seeds in each group stopped on `level-3/15`.
- Cold: eight seeds stopped on `level-3/15`, two on `level-4/20`.

## Interpretation limits

Cold seeds 106 and 110 retained 3 and 7 points respectively. Their final snapshots
had no unallocated supported numeric frontier after excluding other root-adjacent
starters, as the current policy does. Nonstarter frontier entries were socket
`cea70ae58c5741bc8548615a9e1c23a6` and node
`bda91dcd6e1f465fb1b7b16afd4a662a`, whose modifiers include
`AddedDamageToHighestElement`; these are unsupported by the adapter. This is a
simulation/policy coverage boundary, not evidence that the full game's cold tree
has nowhere to grow. The other 48 bots spent all final available points. Traversal
may buy off-family numeric travel nodes; family priority is a scoring preference.

Shops, gems, sockets, infinite nodes, wisps and special/reactive node modifiers
remain outside this run's coverage. Some reward items are retained when unsupported;
claimed and used counts therefore differ. No claimed reward was lost on insertion.
Character snapshots exclude temporary combat effects and show reset boundaries,
not the precise death instant. Big progression differences do not by themselves
identify a defective stat or establish a balance change.

## Timing and storage

Production combat simulation: 110,333.406 seconds (about 30.65 hours).
Adapter compute: 622.188 seconds; execution through final checkpoint discard:
1,207.820 seconds (about 20.13 minutes), 616 actual CLI calls. These measurements
exclude final PowerShell report assembly/compression. Compute speedup: 177.33x;
command-inclusive speedup: 91.35x. This is Editor simulation throughput, not FPS.

`report.json.gz`: 2,374,509 bytes; self-contained `report.html`: 3,179,946 bytes.
The same gzip bytes are embedded in HTML. Metadata, Markdown and this evidence
page are small additions. The completed `report.json.work` directory was removed
by the normal publication path; no duplicated per-bot files remain.

## Verification actually performed

- The requested 50 unique strategy/seed pairs are present; `isPartial` is false.
- Every final snapshot matches its campaign's level, free points, allocation log
  cursor, wave log cursor and allocated nonroot node count.
- The archive parses, embedded gzip bytes equal the archive, and Node.js parses
  the generated viewer JavaScript. No new automated test files were written.
- Unity CLI returned zero temporary simulation actors and zero nonpersistent
  modifiers after execution. Preview-scene count was one (existing baseline),
  the Editor stayed in Edit Mode, scene stayed clean, and the source player still
  had level 1 / one point.
- No game/player build or additional test suite was run. Browser appearance and
  live viewer interaction remain unverified because local-file browsing was blocked
  during the earlier viewer work. This report is for opening in a normal browser.
