# Progression campaign comparison

[Open interactive stats and skill tree](report.html). Select a campaign and a stage boundary. Build stats exclude temporary combat effects; all numeric values are raw production stat values.

Fresh authored player defaults. XP comes from actual enemy kills; nodes are allocated as points arrive. Boss rewards, including gold, are claimed and simple supported items are consumed. Campaigns with shopLog support numeric local gems: bots visit unlocked shops when an active empty socket needs a gem, buy affordable stock through ShopService and insert through InventorySocketService. Gold, stock and socket contents persist across stages. Repeated failures retain earned XP and investment. Influence/bridge gems and UI navigation remain outside coverage. Older reports without shopLog exclude shops and gems.

| Strategy | Runs | Route completed | Mean stage cleared | Maximum stage | Mean final level | Mean nodes | Mean free points | Deaths | Rewards used |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 1 | 0 | 5,0 | 5 | 12,0 | 13,0 | 0,0 | 1 | 1 |
| intelligence-lightning-balanced | 1 | 0 | 5,0 | 5 | 11,0 | 12,0 | 0,0 | 1 | 1 |
| strength-balanced | 1 | 1 | 10,0 | 10 | 22,0 | 24,0 | 0,0 | 0 | 2 |

Location results count only normal-progression runs that reached that location. Boss attempts include normal retries; adaptive probes are separate.

| Strategy | Location | Runs reached | Runs completed | Waves | Deaths | Timeouts | Boss wins / attempts |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | level-1 | 1 | 1 | 5 | 0 | 0 | 1 / 1 |
| dexterity-balanced | level-2 | 1 | 0 | 3 | 1 | 0 | 0 / 0 |
| intelligence-lightning-balanced | level-1 | 1 | 1 | 5 | 0 | 0 | 1 / 1 |
| intelligence-lightning-balanced | level-2 | 1 | 0 | 3 | 1 | 0 | 0 / 0 |
| strength-balanced | level-1 | 1 | 1 | 5 | 0 | 0 | 1 / 1 |
| strength-balanced | level-2 | 1 | 1 | 13 | 0 | 0 | 1 / 1 |

Probe simulated combat: 0,0 s. Throughput includes both normal and probe combat.
Waves: 34. Simulated combat: 437,7 s. Compute: 1,975 s. Full execution: 11,068 s. Compute speedup: 221,6x; end-to-end: 39,5x.

## Per-seed progression

| Build | Seed | Stage cleared | Final level | Allocated nodes | Free points | Deaths | Stop reason |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| strength-balanced | 31001 | 10 | 22 | 24 | 0 | 0 | Route completed |
| dexterity-balanced | 31001 | 5 | 12 | 13 | 0 | 1 | Stage retry limit: level-2/6 |
| intelligence-lightning-balanced | 31001 | 5 | 11 | 12 | 0 | 1 | Stage retry limit: level-2/6 |

## Best-reaching bot stats

One bot per strategy, highest cleared stage then lowest seed. Values are the final reset-boundary production stats, after all retries and XP gains, rather than its stats on first entering that stage. Full tree, all stats and earlier boundaries are in the HTML viewer.

| Build | Seed | Level | Strength | Dexterity | Intelligence | Maximum health | Physical damage | Lightning damage | Attack speed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 31001 | 12 | 3,00 | 11,00 | 3,00 | 121,00 | 17,40 | 0,00 | 0,499 |
| intelligence-lightning-balanced | 31001 | 11 | 3,00 | 3,00 | 5,00 | 101,00 | 0,00 | 34,08 | 0,400 |
| strength-balanced | 31001 | 22 | 33,00 | 5,00 | 5,00 | 223,20 | 33,84 | 0,00 | 0,400 |
