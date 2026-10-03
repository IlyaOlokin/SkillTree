# Location content history: Level11–20

[Home](../Home.md) · [Authoring rules](LocationCreationGuide.md) · [Runtime behavior](../Systems/LocationsAndEnemies.md)

Status: **historical content record**, consolidated and translated on 2026-09-28
from the former Level11–15 and Level16–20 notes. Values below preserve those notes;
this pass did not re-audit the corresponding assets or playtest their balance.
Use the authoring guide for current rules and inspect assets before editing balance.

## Recorded progression

The two batches extended Level10 sequentially, with ten stages per location and
one boss on the last wave of the final stage. Each final reward was one item of
the same type as the preceding batch (Level10 for the first batch, Level15 for the
second). Pool weights were 4/4/2 for Level11–15 and 3/4/3 for Level16–20, increasing
the specialist share from 20% to 30% in the latter batch.

| Location | Absolute stages | Theme / threats | Boss power multiplier |
| --- | --- | --- | --- |
| Level11 — Ember Bastion | 61–70 | Fire, physical/fire damage, ignite, armour | 1.10 |
| Level12 — Frozen Ravine | 71–80 | Cold, physical/cold damage, chill, evasion | 1.15 |
| Level13 — Bloodsteel Keep | 81–90 | Physical, bleed, armour and block | 1.20 |
| Level14 — Thunder Vault | 91–100 | Lightning, physical/lightning damage, overcharge, barriers | 1.25 |
| Level15 — Convergence Citadel | 101–110 | Fire guards, cold hunters, overcharge specialists | 1.30 |
| Level16 — Steam Crucible | 111–120 | Fire/cold, ignite/chill | 1.35 |
| Level17 — Stormscar Ramparts | 121–130 | Physical/lightning, bleed/overcharge | 1.40 |
| Level18 — Glacial Tempest | 131–140 | Cold/lightning, chill/overcharge | 1.45 |
| Level19 — Cindersteel Foundry | 141–150 | Fire/physical, ignite/bleed | 1.50 |
| Level20 — Prismatic Throne | 151–160 | Pairs of the three elements, overcharge/ignite/chill | 1.55 |

Bosses used Utils with nonzero defence and ailment budgets. For Level16–20,
respectively, their recorded combinations were cold/fire with barrier,
physical/lightning with block, cold/lightning with barrier, physical/fire with
regeneration, and cold/lightning with block. Each boss wave contained one enemy.

Normal and DamageDealer attack pools retained mandatory defence-module references
but gained no budget-derived defence stats when defenceWeight was zero; block
affixes were excluded from those profiles. Ailment modules were compatible with
every independently selected attack variant in their pool. Level16–20 did not
change speed-module multipliers. Both batches copied the complete current Level7
Affix Roll Settings block at authoring time.

## Recorded power curve

Stages 1–60 were preserved when Level11–15 was added. Stage 60 → 61 dropped
447 → 435, followed by +8 on ordinary transitions and −12 immediately after bosses
70, 80, 90 and 100; stage 110 reached 747. The drop was one and a half ordinary
steps, intended as a short respite. This was initial tuning requiring playtests.

The second batch preserved stages 1–110 and continued the same +8 / −12 pattern:
stage 110 → 111 was 747 → 735, and stage 160 reached 1047. Boss multipliers rose
by 0.05 per location. These numerical choices are historical, not mandatory values
for future locations.

## Weighted affixes introduced with Level11–15

Labels here describe effects, not verified localization keys. The original notes
reported additions to Enemies Shared Data and RU/EN/DE tables.

| Effect | Added weights | moreExperience |
| --- | --- | --- |
| Ignite power | IgnitePower: 0.3 | 0.15 |
| Chill power | ChillPower: 0.3 | 0.15 |
| Overcharge power | OverchargePower: 0.3 | 0.15 |
| Bleed power | BleedPower: 0.3 | 0.15 |
| Block chance | BlockChance: 0.2 | 0.15 |
| Ailment protection | IgniteMitigation, ChillDurationReduction, OverchargeAvoidanceChance, BleedMitigation: 0.1 each | 0.20 |

These six used addedStatWeights, not direct Added. The first four were attached
to matching ailment modules; block to guards, specialists and bosses; protection
to all new pools. The recorded conversion used capped budget shares for block
and ailment protection, and linear conversion for ailment power. Weights were
not final stat percentages. Earlier pools/affixes were reported unchanged.
Level16–20 reused these and existing affixes with the same compatibility rules.

## Resistance clarification, 2026-09-17

The later addition covered all three pools and boss pools of Level11–20:
general elemental resistance +0.20 and thematic fire/cold/lightning resistance
+0.35. Physical pools received the general resistance option. These four affixes
used direct `ModifierType.Added`, not weights, under the owner's explicit exception.
They work with zero defence budget, add resistance before the final cap, have
`moreExperience` 0.10 each, and were reported translated into RU/EN/DE.

This supersedes any interpretation of the earlier “no direct Added” statement as
a blanket restriction on these resistance affixes. The precise current exception
is maintained in the [authoring guide](LocationCreationGuide.md#affixes).

## Historical validation references

The original notes listed checker invocations for `--levels 11 12 13 14 15` and
`--levels 16 17 18 19 20`. Listing those commands is not evidence they passed.
The checker is currently absent; see [verification](LocationCreationGuide.md#verification).
Unity import and actual combat balance remain separate checks.
