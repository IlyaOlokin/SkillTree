# Progression campaign comparison

Adaptive search uses free experimental respecs on isolated copies. Probes freeze level, XP, point budget and wave seeds; they award no progression or items. Winning/better trees are committed and replayed in normal progression. Probe waves are reported separately.

| Strategy | Probe waves | Distinct candidates evaluated | Changed trees committed | Confirmed stage wins after search |
| --- | ---: | ---: | ---: | ---: |
| dexterity-balanced | 630 | 160 | 47 | 36 |
| intelligence-cold-balanced | 459 | 122 | 25 | 16 |
| intelligence-fire-balanced | 444 | 107 | 23 | 17 |
| intelligence-lightning-balanced | 376 | 94 | 20 | 12 |
| strength-balanced | 774 | 198 | 41 | 17 |

[Open interactive stats and skill tree](report.html). Select a campaign and a stage boundary. Build stats exclude temporary combat effects; all numeric values are raw production stat values.

Fresh authored player defaults. XP comes from actual enemy kills; nodes are allocated as points arrive. Boss rewards are claimed and simple supported items are consumed. Repeated failures retain earned XP and investment. Unsupported mechanics/items are excluded or retained; this is not a full UI/shop/gem playthrough.

| Strategy | Runs | Route completed | Mean stage cleared | Maximum stage | Mean final level | Mean nodes | Mean free points | Deaths | Rewards used |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 5 | 0 | 42,0 | 44 | 95,0 | 101,6 | 0,0 | 169 | 38 |
| intelligence-cold-balanced | 5 | 0 | 14,0 | 14 | 51,4 | 53,4 | 0,0 | 134 | 10 |
| intelligence-fire-balanced | 5 | 0 | 14,0 | 14 | 52,8 | 54,8 | 0,0 | 143 | 10 |
| intelligence-lightning-balanced | 5 | 0 | 14,0 | 14 | 51,6 | 53,6 | 0,0 | 125 | 10 |
| strength-balanced | 5 | 0 | 25,0 | 29 | 75,0 | 79,2 | 0,0 | 197 | 26 |

Location results count only normal-progression runs that reached that location. Boss attempts include normal retries; adaptive probes are separate.

| Strategy | Location | Runs reached | Runs completed | Waves | Deaths | Timeouts | Boss wins / attempts |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | level-1 | 5 | 5 | 25 | 0 | 0 | 5 / 5 |
| dexterity-balanced | level-2 | 5 | 5 | 89 | 10 | 0 | 5 / 7 |
| dexterity-balanced | level-3 | 5 | 5 | 99 | 7 | 1 | 5 / 11 |
| dexterity-balanced | level-4 | 5 | 5 | 84 | 3 | 0 | 5 / 8 |
| dexterity-balanced | level-5 | 5 | 5 | 90 | 5 | 0 | 5 / 10 |
| dexterity-balanced | level-6 | 5 | 5 | 121 | 17 | 0 | 5 / 16 |
| dexterity-balanced | level-7 | 5 | 3 | 290 | 60 | 1 | 3 / 30 |
| dexterity-balanced | level-8 | 3 | 0 | 230 | 67 | 0 | 0 / 57 |
| intelligence-cold-balanced | level-1 | 5 | 5 | 25 | 0 | 0 | 5 / 5 |
| intelligence-cold-balanced | level-2 | 5 | 5 | 108 | 17 | 0 | 5 / 6 |
| intelligence-cold-balanced | level-3 | 5 | 0 | 397 | 117 | 0 | 0 / 97 |
| intelligence-fire-balanced | level-1 | 5 | 5 | 25 | 0 | 0 | 5 / 5 |
| intelligence-fire-balanced | level-2 | 5 | 5 | 110 | 17 | 0 | 5 / 6 |
| intelligence-fire-balanced | level-3 | 5 | 0 | 426 | 126 | 1 | 0 / 97 |
| intelligence-lightning-balanced | level-1 | 5 | 5 | 25 | 0 | 0 | 5 / 5 |
| intelligence-lightning-balanced | level-2 | 5 | 5 | 85 | 7 | 0 | 5 / 5 |
| intelligence-lightning-balanced | level-3 | 5 | 0 | 407 | 118 | 0 | 0 / 99 |
| strength-balanced | level-1 | 5 | 5 | 25 | 0 | 0 | 5 / 5 |
| strength-balanced | level-2 | 5 | 5 | 78 | 5 | 0 | 5 / 6 |
| strength-balanced | level-3 | 5 | 5 | 174 | 32 | 1 | 5 / 31 |
| strength-balanced | level-4 | 5 | 3 | 329 | 88 | 0 | 3 / 81 |
| strength-balanced | level-5 | 3 | 3 | 75 | 10 | 0 | 3 / 13 |
| strength-balanced | level-6 | 3 | 0 | 218 | 62 | 0 | 0 / 58 |

Probe simulated combat: 36917,5 s. Throughput includes both normal and probe combat.
Waves: 3535. Simulated combat: 50784,5 s. Compute: 816,073 s. Full execution: 1454,871 s. Compute speedup: 107,5x; end-to-end: 60,3x.

## Per-seed progression

| Build | Seed | Stage cleared | Final level | Allocated nodes | Free points | Deaths | Stop reason |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| strength-balanced | 101 | 29 | 86 | 91 | 0 | 53 | Stage retry limit: level-6/30 |
| strength-balanced | 102 | 19 | 62 | 65 | 0 | 27 | Stage retry limit: level-4/20 |
| strength-balanced | 103 | 29 | 82 | 87 | 0 | 42 | Stage retry limit: level-6/30 |
| strength-balanced | 104 | 29 | 86 | 91 | 0 | 49 | Stage retry limit: level-6/30 |
| strength-balanced | 105 | 19 | 59 | 62 | 0 | 26 | Stage retry limit: level-4/20 |
| dexterity-balanced | 101 | 44 | 96 | 103 | 0 | 30 | Stage retry limit: level-8/45 |
| dexterity-balanced | 102 | 39 | 90 | 96 | 0 | 27 | Stage retry limit: level-7/40 |
| dexterity-balanced | 103 | 44 | 101 | 108 | 0 | 43 | Stage retry limit: level-8/45 |
| dexterity-balanced | 104 | 44 | 101 | 108 | 0 | 38 | Stage retry limit: level-8/45 |
| dexterity-balanced | 105 | 39 | 87 | 93 | 0 | 31 | Stage retry limit: level-7/40 |
| intelligence-fire-balanced | 101 | 14 | 50 | 52 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 102 | 14 | 51 | 53 | 0 | 26 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 103 | 14 | 55 | 57 | 0 | 32 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 104 | 14 | 54 | 56 | 0 | 31 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 105 | 14 | 54 | 56 | 0 | 30 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 101 | 14 | 49 | 51 | 0 | 22 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 102 | 14 | 50 | 52 | 0 | 23 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 103 | 14 | 53 | 55 | 0 | 25 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 104 | 14 | 53 | 55 | 0 | 26 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 105 | 14 | 53 | 55 | 0 | 29 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 101 | 14 | 50 | 52 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 102 | 14 | 51 | 53 | 0 | 25 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 103 | 14 | 53 | 55 | 0 | 31 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 104 | 14 | 51 | 53 | 0 | 25 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 105 | 14 | 52 | 54 | 0 | 29 | Stage retry limit: level-3/15 |

## Best-reaching bot stats

One bot per strategy, highest cleared stage then lowest seed. Values are the final reset-boundary production stats, after all retries and XP gains, rather than its stats on first entering that stage. Full tree, all stats and earlier boundaries are in the HTML viewer.

| Build | Seed | Level | Strength | Dexterity | Intelligence | Maximum health | Physical damage | Lightning damage | Attack speed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 101 | 96 | 22,00 | 81,00 | 20,00 | 179,55 | 173,70 | 0,00 | 0,533 |
| intelligence-cold-balanced | 101 | 50 | 11,00 | 11,00 | 33,00 | 148,50 | 0,00 | 8,64 | 0,400 |
| intelligence-fire-balanced | 101 | 50 | 11,00 | 11,00 | 33,00 | 148,50 | 0,00 | 9,00 | 0,400 |
| intelligence-lightning-balanced | 101 | 49 | 10,00 | 10,00 | 31,00 | 148,50 | 0,00 | 77,62 | 0,400 |
| strength-balanced | 101 | 86 | 153,00 | 18,00 | 18,00 | 585,45 | 158,21 | 0,00 | 0,456 |

## Same-seed baseline comparison

Only seeds 101–105 from the prior baseline are compared with these five adaptive
runs per policy. Values are mean highest completed stage.

| Policy | Baseline | Adaptive | Difference |
| --- | ---: | ---: | ---: |
| Strength | 17 | 25 | +8 |
| Dexterity | 51 | 42 | -9 |
| Intelligence/fire | 14 | 14 | 0 |
| Intelligence/lightning | 14 | 14 | 0 |
| Intelligence/cold | 14 | 14 | 0 |

Search produced 156 changed layouts and 41 normal-stage wins after changed-layout
commits. There were 98 wins after any search, including unchanged incumbents that
had already earned XP. Local-stage improvements do not guarantee better campaign
progression: the first adaptive version regressed dexterity and left all elemental
ceilings unchanged. See [verification and per-seed comparison](verification.md).
