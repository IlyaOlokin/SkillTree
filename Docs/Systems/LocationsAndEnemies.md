# Locations and generated enemies

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Content rules](../Locations/LocationCreationGuide.md)

Status: **source-reviewed, 2026-09-28**. Covers location progress, wave generation
and completion handoff. No Unity playtest or asset-by-asset balance audit was run.
The [authoring guide](../Locations/LocationCreationGuide.md) remains authoritative
for content; [content history](../Locations/ContentHistory.md) records older tuning.

## Terms and ownership

A location is a map node. A stage is an absolute difficulty level (usually named
`level` in code). A wave is one encounter inside that stage. A location can be a
battle or a shop; entering a shop does not create an enemy wave.

| Owner | Responsibility |
| --- | --- |
| `LocationDefinition` / `LocationCatalog` | Identity, map content, prerequisites, database or shop, stage rewards and catalog lookup |
| `LocationFlowController` | Map/Battle/Shop transitions, battle tick pause/resume and player combat reset |
| `EnemyLocationProgressService` | Selected location/stage, unlocked frontier, completed stages and claimed reward IDs |
| `EnemySpawner` | Active enemy pool, wave context, deaths, wave clear and respawn |
| `WaveFactory` / `EnemyFactory` | Wave composition and per-enemy stat packages |
| `LocationCompleteWindowController` | Completion window and reward collection handoff |

## Unlocking and progress

Prerequisites are **OR**, not AND: any completed prerequisite unlocks the location;
an empty list unlocks immediately. A location is complete when its recorded highest
completed absolute stage reaches its database's `MaxWaveLevel`.

Despite its name, `CompletedLevelCount` stores the highest completed absolute stage,
not the number of stages completed in the location. `RegisterCompletedLevel` returns
true only when this frontier increases. Selecting a stage clamps it between the
database starting stage and the unlocked frontier. Selecting an older stage disables
automatic progression; selecting the frontier enables it.

Progress is stored per location ID. Save capture includes selection, unlocked and
completed frontiers, and claimed reward IDs; it does not serialize live enemies or
partial-wave combat. On restore, stage bounds are clamped to the current database.
See [saves and profiles](SavesAndProfiles.md) before changing IDs or stage ranges.

## From stage to enemy

`EnemySpawner` builds a context from the selected absolute stage and the next wave
number. Boss rules can supply boss count, forced enemy count and a boss affix cap.
`WaveFactory` combines level power with the wave power multiplier, then assembles
enemy packages. Ordinary waves use a normalized resource budget and randomized
weights; forced boss-wave counts bypass the ordinary budget stopping rule.

`EnemyFactory` scales enemy power by the generated definition's wave weight and
passes the definition, rarity, budgets, affix settings and generation modifiers to
the stat-package builder. Global modifiers are appended. Enemy composition is
therefore the result of several configurations, not just a stage's power value.
Use [stats and modifiers](StatsAndModifiers.md) for arithmetic and the authoring
guide for zero-budget categories and compatible module pools.

The spawner initializes as many packages as its available unit pool can hold,
disposes unused packages, and supplies the active enemy set to the attack resolver.
Pool capacity is a runtime limit even if content requests a larger wave.

`EnemyPool` selects separate Inspector position lists for one, two and three
enemies before activation, using the spawn count after the pool capacity limit.
The former `spawnPositions` assignments are preserved as the three-enemy list.
Assign the new one- and two-enemy lists in the Inspector; missing entries fall
back to the corresponding three-enemy position for existing scene compatibility.
Enemies are not rearranged after deaths. This change was source-reviewed on
2026-09-30; no Unity playtest was run.

## Death, wave clear and completion

An active enemy death is handled once: remove it, unsubscribe and resolve gold.
Only the last active enemy clears the wave. Reaching the stage's wave quota records
completion; automatic progression resets combat and unlocks the next stage.
Normal continuation schedules a respawn using scaled `WaitForSeconds`.

The event named `OnLocationCompletedFirstTime` is raised for a **boss wave that
newly completes a stage**. It is not restricted to the final stage of the location.
That path suppresses automatic selection/respawn and opens the completion window,
which pauses battle ticks. Content with a mid-location boss must account for this
behavior; the event name should not be treated as a final-location-only contract.

Pending rewards match the completed absolute stage exactly and exclude already
claimed IDs. Each ID is derived from location ID, stage, item definition ID and
amount. Changing any of these can make an old reward appear unclaimed; duplicate
entries with the same values share an ID. Claiming records the ID in the selected
location's progress. For delivery and its current failure handling, read
[economy and loot](EconomyAndLoot.md).

## Verification when changing this area

- Check first entry, replay of an earlier stage and return to the frontier.
- Check OR prerequisites, branching/overlapping ranges and restore after a catalog change.
- Check an ordinary clear, a mid-location boss and the final boss separately.
- Check pool capacity, forced boss count, pause/resume and leaving during respawn.
- Check reward delivery, repeat completion and save/reload around claiming.

These are recommended checks, not tests executed in this documentation pass.
Content validation requirements and the currently missing checker are recorded in
the [authoring guide](../Locations/LocationCreationGuide.md#verification).

## Source entry points

- `Assets/Scripts/Battle/Locations/LocationDefinition.cs`
- `Assets/Scripts/Battle/Locations/LocationFlowController.cs`
- `Assets/Scripts/Battle/EnemySystem/EnemyLocationProgressService.cs`
- `Assets/Scripts/Battle/EnemySystem/EnemySpawner.cs`
- `Assets/Scripts/Battle/EnemySystem/WaveFactory.cs`
- `Assets/Scripts/Battle/EnemySystem/EnemyFactory.cs`
- `Assets/Scripts/UI/LocationCompleteWindowController.cs`

Related: [combat](CombatAndEffects.md), [inventory](InventoryAndItems.md),
[economy and loot](EconomyAndLoot.md), [saves](SavesAndProfiles.md).
