# Progression campaign comparison

[Open interactive stats and skill tree](report.html). Select a campaign and a stage boundary. Build stats exclude temporary combat effects; all numeric values are raw production stat values.

Fresh authored player defaults. XP comes from actual enemy kills; nodes are allocated as points arrive. Boss rewards are claimed and simple supported items are consumed. Repeated failures retain earned XP and investment. Unsupported mechanics/items are excluded or retained; this is not a full UI/shop/gem playthrough.

| Strategy | Runs | Route completed | Mean stage cleared | Maximum stage | Mean final level | Mean nodes | Mean free points | Deaths | Rewards used |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 10 | 0 | 58,0 | 79 | 111,9 | 120,5 | 0,0 | 629 | 96 |
| intelligence-cold-balanced | 10 | 0 | 15,0 | 19 | 55,3 | 56,5 | 1,0 | 353 | 22 |
| intelligence-fire-balanced | 10 | 0 | 14,0 | 14 | 53,9 | 55,9 | 0,0 | 342 | 20 |
| intelligence-lightning-balanced | 10 | 0 | 14,0 | 14 | 51,0 | 53,0 | 0,0 | 246 | 20 |
| strength-balanced | 10 | 0 | 17,0 | 29 | 46,5 | 49,1 | 0,0 | 378 | 36 |

Location results count only runs that reached that location. Boss attempts include retries.

| Strategy | Location | Runs reached | Runs completed | Waves | Deaths | Timeouts | Boss wins / attempts |
| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | level-1 | 10 | 10 | 50 | 0 | 0 | 10 / 10 |
| dexterity-balanced | level-10 | 4 | 4 | 202 | 31 | 0 | 4 / 17 |
| dexterity-balanced | level-11 | 4 | 4 | 226 | 41 | 0 | 4 / 17 |
| dexterity-balanced | level-12 | 4 | 0 | 374 | 102 | 0 | 0 / 61 |
| dexterity-balanced | level-2 | 10 | 10 | 224 | 47 | 0 | 10 / 30 |
| dexterity-balanced | level-3 | 10 | 10 | 296 | 53 | 1 | 10 / 33 |
| dexterity-balanced | level-4 | 10 | 10 | 252 | 36 | 0 | 10 / 40 |
| dexterity-balanced | level-5 | 10 | 10 | 201 | 17 | 1 | 10 / 23 |
| dexterity-balanced | level-6 | 10 | 10 | 276 | 43 | 0 | 10 / 46 |
| dexterity-balanced | level-7 | 10 | 10 | 559 | 91 | 1 | 10 / 80 |
| dexterity-balanced | level-8 | 10 | 4 | 601 | 160 | 0 | 4 / 149 |
| dexterity-balanced | level-9 | 4 | 4 | 84 | 8 | 0 | 4 / 9 |
| intelligence-cold-balanced | level-1 | 10 | 10 | 50 | 0 | 0 | 10 / 10 |
| intelligence-cold-balanced | level-2 | 10 | 10 | 251 | 51 | 0 | 10 / 18 |
| intelligence-cold-balanced | level-3 | 10 | 2 | 830 | 252 | 2 | 2 / 177 |
| intelligence-cold-balanced | level-4 | 2 | 0 | 162 | 50 | 0 | 0 / 34 |
| intelligence-fire-balanced | level-1 | 10 | 10 | 50 | 0 | 0 | 10 / 10 |
| intelligence-fire-balanced | level-2 | 10 | 10 | 324 | 79 | 0 | 10 / 21 |
| intelligence-fire-balanced | level-3 | 10 | 0 | 863 | 263 | 1 | 0 / 184 |
| intelligence-lightning-balanced | level-1 | 10 | 10 | 50 | 0 | 0 | 10 / 10 |
| intelligence-lightning-balanced | level-2 | 10 | 10 | 181 | 18 | 0 | 10 / 10 |
| intelligence-lightning-balanced | level-3 | 10 | 0 | 777 | 228 | 0 | 0 / 190 |
| strength-balanced | level-1 | 10 | 10 | 50 | 0 | 0 | 10 / 10 |
| strength-balanced | level-2 | 10 | 4 | 317 | 173 | 0 | 4 / 167 |
| strength-balanced | level-3 | 4 | 4 | 164 | 37 | 0 | 4 / 24 |
| strength-balanced | level-4 | 4 | 4 | 226 | 57 | 0 | 4 / 54 |
| strength-balanced | level-5 | 4 | 4 | 126 | 22 | 0 | 4 / 24 |
| strength-balanced | level-6 | 4 | 0 | 305 | 89 | 0 | 0 / 73 |

Waves: 8071. Simulated combat: 110333,4 s. Compute: 622,188 s. Full execution: 1207,820 s. Compute speedup: 177,3x; end-to-end: 91,3x.

## Per-seed progression

| Build | Seed | Stage cleared | Final level | Allocated nodes | Free points | Deaths | Stop reason |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |
| strength-balanced | 101 | 9 | 22 | 23 | 0 | 22 | Stage retry limit: level-2/10 |
| strength-balanced | 102 | 29 | 85 | 90 | 0 | 57 | Stage retry limit: level-6/30 |
| strength-balanced | 103 | 9 | 22 | 23 | 0 | 21 | Stage retry limit: level-2/10 |
| strength-balanced | 104 | 9 | 21 | 22 | 0 | 20 | Stage retry limit: level-2/10 |
| strength-balanced | 105 | 29 | 83 | 88 | 0 | 59 | Stage retry limit: level-6/30 |
| strength-balanced | 106 | 29 | 86 | 91 | 0 | 71 | Stage retry limit: level-6/30 |
| strength-balanced | 107 | 29 | 85 | 90 | 0 | 66 | Stage retry limit: level-6/30 |
| strength-balanced | 108 | 9 | 19 | 20 | 0 | 20 | Stage retry limit: level-2/10 |
| strength-balanced | 109 | 9 | 20 | 21 | 0 | 20 | Stage retry limit: level-2/10 |
| strength-balanced | 110 | 9 | 22 | 23 | 0 | 22 | Stage retry limit: level-2/10 |
| dexterity-balanced | 101 | 44 | 100 | 107 | 0 | 51 | Stage retry limit: level-8/45 |
| dexterity-balanced | 102 | 44 | 100 | 107 | 0 | 45 | Stage retry limit: level-8/45 |
| dexterity-balanced | 103 | 44 | 97 | 104 | 0 | 42 | Stage retry limit: level-8/45 |
| dexterity-balanced | 104 | 79 | 131 | 142 | 0 | 87 | Stage retry limit: level-12/80 |
| dexterity-balanced | 105 | 44 | 98 | 105 | 0 | 39 | Stage retry limit: level-8/45 |
| dexterity-balanced | 106 | 79 | 132 | 143 | 0 | 89 | Stage retry limit: level-12/80 |
| dexterity-balanced | 107 | 44 | 100 | 107 | 0 | 52 | Stage retry limit: level-8/45 |
| dexterity-balanced | 108 | 79 | 130 | 141 | 0 | 88 | Stage retry limit: level-12/80 |
| dexterity-balanced | 109 | 79 | 130 | 141 | 0 | 78 | Stage retry limit: level-12/80 |
| dexterity-balanced | 110 | 44 | 101 | 108 | 0 | 58 | Stage retry limit: level-8/45 |
| intelligence-fire-balanced | 101 | 14 | 54 | 56 | 0 | 35 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 102 | 14 | 52 | 54 | 0 | 28 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 103 | 14 | 54 | 56 | 0 | 35 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 104 | 14 | 53 | 55 | 0 | 30 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 105 | 14 | 54 | 56 | 0 | 33 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 106 | 14 | 54 | 56 | 0 | 35 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 107 | 14 | 53 | 55 | 0 | 34 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 108 | 14 | 53 | 55 | 0 | 36 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 109 | 14 | 53 | 55 | 0 | 35 | Stage retry limit: level-3/15 |
| intelligence-fire-balanced | 110 | 14 | 59 | 61 | 0 | 41 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 101 | 14 | 50 | 52 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 102 | 14 | 50 | 52 | 0 | 22 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 103 | 14 | 53 | 55 | 0 | 25 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 104 | 14 | 52 | 54 | 0 | 26 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 105 | 14 | 51 | 53 | 0 | 25 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 106 | 14 | 52 | 54 | 0 | 27 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 107 | 14 | 49 | 51 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 108 | 14 | 51 | 53 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 109 | 14 | 50 | 52 | 0 | 23 | Stage retry limit: level-3/15 |
| intelligence-lightning-balanced | 110 | 14 | 52 | 54 | 0 | 26 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 101 | 14 | 50 | 52 | 0 | 32 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 102 | 14 | 50 | 52 | 0 | 24 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 103 | 14 | 55 | 57 | 0 | 35 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 104 | 14 | 52 | 54 | 0 | 30 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 105 | 14 | 53 | 55 | 0 | 31 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 106 | 19 | 67 | 67 | 3 | 49 | Stage retry limit: level-4/20 |
| intelligence-cold-balanced | 107 | 14 | 50 | 52 | 0 | 28 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 108 | 14 | 51 | 53 | 0 | 32 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 109 | 14 | 54 | 56 | 0 | 30 | Stage retry limit: level-3/15 |
| intelligence-cold-balanced | 110 | 19 | 71 | 67 | 7 | 62 | Stage retry limit: level-4/20 |

## Best-reaching bot stats

One bot per strategy, highest cleared stage then lowest seed. Values are the final reset-boundary production stats, after all retries and XP gains, rather than its stats on first entering that stage. Full tree, all stats and earlier boundaries are in the HTML viewer.

| Build | Seed | Level | Strength | Dexterity | Intelligence | Maximum health | Physical damage | Lightning damage | Attack speed |
| --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| dexterity-balanced | 104 | 131 | 29,00 | 105,00 | 27,00 | 203,70 | 306,68 | 0,00 | 0,794 |
| intelligence-cold-balanced | 106 | 67 | 14,00 | 14,00 | 39,00 | 150,70 | 0,00 | 42,91 | 0,400 |
| intelligence-fire-balanced | 101 | 54 | 11,00 | 11,00 | 33,00 | 148,50 | 0,00 | 18,36 | 0,400 |
| intelligence-lightning-balanced | 101 | 50 | 11,00 | 11,00 | 33,00 | 148,50 | 0,00 | 77,88 | 0,400 |
| strength-balanced | 102 | 85 | 152,00 | 18,00 | 18,00 | 579,00 | 144,34 | 0,00 | 0,456 |

## Policy coverage note

Cold seeds 106 and 110 ended with 3 and 7 free points. No additional supported
numeric frontier remained within the selected starter route; continuation required
an excluded socket or special modifier node. This limits conclusions about the
full cold tree. See [execution and verification](verification.md) for scope,
stop distributions, throughput and checks. The other 48 bots spent their final points.
