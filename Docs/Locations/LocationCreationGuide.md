# Location and enemy authoring guide

[Home](../Home.md) · [Runtime behavior](../Systems/LocationsAndEnemies.md) · [Content history](ContentHistory.md)

Read before creating, extending, balancing or reviewing locations, enemy pools,
bosses, affixes or level power. The owner's explicit request takes precedence.
Existing content is a working baseline, not approved balance. Change it only
within the current task. Translated and reconciled on **2026-09-28**; the owner's
rules and numeric exceptions are preserved.

## Structure and registration

- A location is a map node; a stage is an absolute difficulty level; a wave is
  one battle within a stage.
- Content lives in `Assets/EnemyConfig`. Each location has its own folder with
  a LocationDefinition, EnemyConfigDatabase and EnemyBossDefinition. Additional
  boss definitions are allowed for different bosses.
- Assign unique IDs and GUIDs, stage ranges, rewards and unlock requirements.
  Register locations in LocationCatalog and bosses through the database's `bossBalance`.
- Level7's missing icon/background GUID references are intentional. Preserve
  them when copying; do not replace or clear them without a request. Other
  references must resolve.

## Themes and pools

- Themes are desirable but optional; express them through the enemy composition.
- Use a small number of meaningful pools with varied random combinations, rather
  than splitting the content into many fixed enemies.
- Check compatibility of modules and effects that are selected independently.
- Respect CoreProfile budgets: a category with zero allocation receives no
  budget-derived stats from its module or added weights. Account for module multipliers.

## Affixes

- For stats supported by weights, use `addedStatWeights`, except for the elemental
  resistance exception below. Verify support in EnemyStatPackageBuilder, not just StatType.
- Direct `ModifierType.Added` is forbidden except for owner-approved
  `FireResistance`, `ColdResistance`, `LightningResistance` and `ElementalResistance`
  affixes (clarification dated 2026-09-17). Direct Added is allowed for these even
  when weights are supported and the defence budget is zero. A value of `0.35`
  adds 35 percentage points before the final stat cap; it is not a 35% increase
  of existing resistance. This exception does not cover maximum resistances or
  other stats. Added weights and internal budget-to-stat conversion are allowed.
  Effects must remain relevant from early through late game.
- For stats without weight support, use simple `Increased` or `More` modifiers.
- Prefer a clear effect on one stat or a small related group. Do not duplicate
  a boost through both weight and modifier without a reason.
- A weight is not a percentage of the final stat: consider category allocation,
  normalization, conversion rules, caps and rounding.
- Add affixes to appropriate pools, set `moreExperience` and readable names.
  Add new keys to Enemies Shared Data and RU/EN/DE translations.

## Affix Roll Settings

For every location after Level7, copy its entire current block. This is the
baseline, not a later-game balance control. Tuning these settings is intended
for early locations. Develop later difficulty through power, enemy composition,
affix effects and boss settings.

## Stages and bosses

- The final stage of every battle location must end with a boss; use
  `lastWaveOnly` for its final wave.
- Ten-stage locations may have bosses at stages five and ten or only at ten.
  Locations do not have to share a fixed length.
- Boss schedules use absolute stage numbers. Configure boss-wave enemy count
  separately from its power multiplier.

## Branching

- Branches, merges and overlapping difficulty ranges are allowed.
- `unlockPrerequisites` uses OR: completing any listed location is sufficient;
  an empty list makes the location immediately available. Check graph reachability.
- Shared power and boss configs apply by absolute stage to every location using
  them. Use separate configs for different rules at the same stage, rather than
  conflicting entries in one shared config.

## Power

- Keep EnemyLevelPowerConfig growth linear, with small drops immediately after
  bosses followed by renewed linear growth.
- Do not replace this with exponential growth or a fixed percentage increase.
  The numerical step and drop are not prescribed; explain chosen values.
- Cover every stage. When changing shared curves, consider parallel locations
  and their different boss schedules.

## Verification

After content edits, the intended read-only validator is
`Tools/Locations/check_locations.py`, using Python 3.10+ and no external packages.
`--levels` filters location numbers, `--json` selects report format and `--root`
sets the project root. Without a filter it checks all LevelN folders.

**Availability checked 2026-09-28:** the checker is absent from this checkout.
Locate or restore the intended tool before relying on this workflow. Do not claim
that it passed, or silently replace it with a different validator. Documentation-only
edits require link/source checks, not a Unity build or content rebalance.

The recorded checker contract covers references, reachability, final bosses, pools,
affixes, Level7 settings and power coverage. Curve warnings require manual judgment.
Recorded exit codes: 0 = no errors, 1 = content errors, 2 = read/format failure.
Read warnings too; intentional visual references are excluded. This contract is
historical documentation, not a fresh verification of the missing implementation.

Text-field validation does not replace Unity imports and combat checks. When an
Editor is available, check imports and console after content edits, and appearance
for visual edits. State verification limits explicitly. Changes to the checker
also require its tests in `Tools/Locations/test_check_locations.py`.
