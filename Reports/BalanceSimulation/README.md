# Balance simulation reports

[Project documentation](../../Docs/Home.md) · [Runner](../../Tools/BalanceSimulation/README.md) · [Report index](index.html)

This is the single home for balance results. Default runs create dated
`YYYYMMDD-HHMMSS-fff-campaign/` or `...-fixed-battles/` folders. Each finished run
contains `report.html`, `report.md`, `report.json.gz` and a small `report.label.txt`
used by the index. Generated runs are ignored by Git; copy the folder to share it.
The index is rebuilt when a report is published or regenerated.

Open `index.html` in a normal browser, then choose a run. HTML embeds the same
compressed dataset as the gzip archive, so it works offline without Unity, a server
or network access. The viewer uses native [DecompressionStream](https://developer.mozilla.org/en-US/docs/Web/API/DecompressionStream)
with gzip; use a current Chrome, Edge or Firefox. An unsupported browser shows an
error. JSON archives can also be unpacked with a normal gzip-compatible tool.

Choose a bot/seed and stage boundary to see actual numeric stats, allocated and
active nodes, Power, authored graph positions and purchase order. Click a node for
its SaveId, cost and modifier details. Drag to pan, use the wheel/buttons to zoom
and enable the whole tree for context. Logs show only events already observed at
the selected boundary. Node selection highlights the selected node.

Campaign schema 2 stores `treeCatalog` and per-bot `snapshots`: reset-boundary stats,
node states, level, free points and allocation/reward/wave-log cursors. These snapshots
exclude temporary combat effects; they are not the precise instant of death.
Fixed schema 3 stores initial stats and static tree inspections. Compression changes
storage, not those fields. Stat labels use production StatType names/raw units,
including fractional chances and numeric masks. Modifier contribution values are
inputs, not an additive decomposition of final damage or health.

Unfinished campaign data stays in `report.json.work/`: metadata, graph catalog and
one export per completed bot. Successful full publication deletes it. Offline
`Recover.ps1` validates saved strategy/seed pairs; `-AllowPartial` is required to
publish an incomplete dataset, which is visibly labelled and retains its work.
Recovery assembles completed bots; it does not resume a partly played campaign.
Fixed-battle failures retain batches for manual inspection only.

`RebuildReport.ps1` regenerates the selected viewer from its dataset without Unity.
Legacy raw JSON is removed only after comparing it with the unpacked new archive.
Standalone catalog exports also live here; they are raw JSON and are not final runs.
All runner output paths must stay under this root.

The owner removed previous run folders on 2026-10-04 before requesting fresh
five-build balanced campaigns. New runs use the compressed format described above.
Historical measurements in Docs describe earlier executions whose raw artifacts
are no longer available. Stable reference pages preserve old documentation paths.

Adaptive runner schema 2 also records search evaluations, committed refits and
normal-stage confirmation events. Snapshots marked PROBE freeze XP/level and point
budget; their composition is shown directly, rather than presented as historical
purchases. Select a search-table row to view that candidate's stats/tree. Earlier
allocation revisions remain in the dataset. Trial wave counts are separate from
normal-progression wave tables. A free experimental tree respec is a simulator
search capability, not a change to gameplay refund rules.

Campaign snapshots authored since 2026-10-08 additionally contain gold, socket gem
contents and a shop-log cursor. The viewer includes shop visits, purchase outcomes,
prices and socket insertions. Older reports without these fields keep their
original shop/gem exclusions. See [policy scope](../../Tools/BalanceSimulation/README.md#campaign-shops-and-local-gems-2026-10-08).

## Strategy averages and heat map

The HTML viewer offers a strategy selector with equal-weight averages across its
bots. Each bot contributes its last non-probe reset-boundary snapshot once;
longer runs and repeated deaths do not receive additional weight. It shows mean,
minimum and maximum results and scalar stats, with the stat sample count. TypeMask
stats display observed mask variants instead of an invalid arithmetic mean.

Heat-map frequency is the fraction of the strategy's bots whose final tree contains
a node; the activity selector instead counts final active nodes. Roots are shown
for graph context. Colours range from grey (0%) through blue to yellow (100%);
clicking a node shows counts and seeds. This is final build consistency, not node
purchase frequency over time, causal effectiveness or an averaged playable build.
Runs may end at different levels/stages. Existing individual-bot views remain.

Verification: JavaScript syntax check passed. The summary module executed against
the real 15-bot dataset with a lightweight in-memory DOM; all three strategy means
and MaximumHealth means matched independent calculations, and strategy/heat-map
mode switching completed without script errors. Embedded viewer data matches the
archive. Browser visual interaction was not checked: the in-app browser blocks
local file URLs. No additional simulations or Unity build were run for this view.
