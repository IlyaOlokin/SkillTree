# SkillTree Knowledge Base

Start with the [project map](ProjectMap.md) to find the system relevant to your task.
Use the [issue backlog](Backlog.md) for recorded problems, conditional risks and
remaining verification tasks, with provisional priorities and closure criteria.
This vault is being documented incrementally. A page marked **source-reviewed**
describes inspected code; it does not imply a new Unity playtest or approved balance.

## Understand the game

The documented path now connects character development to combat:

- [Skill tree and progression](Systems/SkillTree.md): allocation, activity, refunds,
  node power, zones and persistence.
- [Gems and sockets](Systems/Gems.md): inventory transfers, local modifiers,
  influence distances and bridges.
- [Stats and modifiers](Systems/StatsAndModifiers.md): arithmetic, calculation
  phases, power contexts, runtime bindings and attack snapshots.
- [Combat and effects](Systems/CombatAndEffects.md): tick phases, attack order,
  damage receipt, events and effect lifecycle.
- [Defences and resources](Systems/DefencesAndResources.md): evasion, armor,
  resistance, block/parry, barriers, health and mystic absorption.
- [Ailments and debuffs](Systems/AilmentsAndDebuffs.md): application chances,
  damage pools, Chill/Freeze, Overcharge and temporary stat penalties.
- [Inventory and items](Systems/InventoryAndItems.md): stacking, selection, item use
  and the inventory side of socket exchanges.
- [Saves and profiles](Systems/SavesAndProfiles.md): snapshots, autosave, recovery,
  profile resets, stable IDs and browser persistence.
- [Locations and enemies](Systems/LocationsAndEnemies.md): unlocks, stage progress,
  wave generation, bosses and completion rewards.
- [Economy and loot](Systems/EconomyAndLoot.md): wallet, shops, gold rolls,
  item delivery and reward claims.
- [Menus and screen flow](Systems/MenusAndScreenFlow.md): menu graph actions,
  profile entry, gameplay modes and modal boundaries.
- [Tutorials](Systems/Tutorials.md): event queue, eligibility, presentation,
  pause ownership and saved progress.
- [Localization](Systems/Localization.md): tables, fallbacks, formatting,
  language selection and text refresh.
- [Audio](Systems/Audio.md): cue lookup, source pooling, music transitions,
  mixer parameters and volume settings.
- [HUD and tooltips](Systems/HudAndTooltips.md): resource displays, ownership,
  pinned/nested tooltips and rendering references.
- [Battle mini-games](Systems/BattleMiniGames.md): activators, clocks, results,
  unlock rules and temporary rewards.
- [Reactive combat effects](Systems/ReactiveCombatEffects.md): stored attack
  resources, temporary buffs and next-hit consumption boundaries.

For development workflows, use [editor tools, checks and builds](Reference/EditorAndBuildTools.md).

<<<<<<< HEAD
Live balance observations: [2026-10-04 first-location playtest](Reference/BalancePlaytest20261004.md).
Progression simulations: [adaptive five-build campaigns](../Reports/BalanceSimulation/20261004-135038-237-campaign/verification.md),
[balanced baseline](../Reports/BalanceSimulation/20261004-124706-435-campaign/verification.md)
and [historical references](Reference/BalanceCampaign20261004.md).
=======
For the current content-design proposal, see [50 Big-node candidates](../Design/BigNodes50.md):
10 ordinary stat sets, 20 existing modifier combinations and 20 simple new modifier
proposals. This Russian design artifact is not an approved balance specification or
implemented scene content.
>>>>>>> main

These are related systems, so they live in `Systems`. Add future system pages
there instead of creating a folder for each mechanic.

## Follow a task

| Task | Start here | Then follow |
| --- | --- | --- |
| Change allocation, refunds or node state | [Skill tree](Systems/SkillTree.md) | [Gems](Systems/Gems.md) if bridges or sockets are involved |
| Add or change a gem | [Gems](Systems/Gems.md) | [Skill tree](Systems/SkillTree.md) for power and connectivity |
| Fill or balance clusters | [Filling guide](SkillTree/SkillTreeFillingGuide.md) | [Skill tree](Systems/SkillTree.md) for implementation |
| Change infinite nodes | [Skill tree](Systems/SkillTree.md) | [Infinite node reference](SkillTree/InfiniteNode.md) |
| Change bridge placement | [Gems](Systems/Gems.md) | [Bridge reference](Reference/BridgeGem.md) |
| Change stats or modifiers | [Stats and modifiers](Systems/StatsAndModifiers.md) | [Combat](Systems/CombatAndEffects.md) for attack-local behavior |
| Change attacks, damage or effects | [Combat and effects](Systems/CombatAndEffects.md) | [Effect event contract](Reference/EffectApplicationEvents.md) for reactions |
| Change defence or health resources | [Defences and resources](Systems/DefencesAndResources.md) | [Combat order](Systems/CombatAndEffects.md) and [stat arithmetic](Systems/StatsAndModifiers.md) |
| Change ailments or attack debuffs | [Ailments and debuffs](Systems/AilmentsAndDebuffs.md) | [Effect event contract](Reference/EffectApplicationEvents.md) and [defences](Systems/DefencesAndResources.md) |
| Change items, stacks or selection | [Inventory and items](Systems/InventoryAndItems.md) | [Gems](Systems/Gems.md) for socket exchanges |
| Change saves, profiles or content IDs | [Saves and profiles](Systems/SavesAndProfiles.md) | [Inventory](Systems/InventoryAndItems.md) and [tree](Systems/SkillTree.md) for restored state |
| Change location progression or enemy generation | [Locations and enemies](Systems/LocationsAndEnemies.md) | [Authoring rules](Locations/LocationCreationGuide.md) for content changes |
| Change gold, shops or reward delivery | [Economy and loot](Systems/EconomyAndLoot.md) | [Inventory](Systems/InventoryAndItems.md) and [saves](Systems/SavesAndProfiles.md) |
| Change menu navigation or modal flow | [Menus and screen flow](Systems/MenusAndScreenFlow.md) | [Tutorials](Systems/Tutorials.md) for pause/input ownership |
| Add or change tutorial content | [Tutorial authoring](Tutorials/README.md) | [Tutorial runtime](Systems/Tutorials.md) for triggers and persistence |
| Change localized text or language selection | [Localization](Systems/Localization.md) | [Tutorial authoring](Tutorials/README.md) for tutorial content |
| Change sound playback or volume controls | [Audio](Systems/Audio.md) | [Menus](Systems/MenusAndScreenFlow.md) and [saves](Systems/SavesAndProfiles.md) |
| Change HUD or tooltip behavior | [HUD and tooltips](Systems/HudAndTooltips.md) | [Bar contour](UIBarContour.md) and [magic root](Reference/ProceduralMagicRootUI.md) for rendering |
| Change mini-game spawning or rewards | [Battle mini-games](Systems/BattleMiniGames.md) | [Combat lifecycle](Systems/CombatAndEffects.md) and [screen flow](Systems/MenusAndScreenFlow.md) |
| Change Pain, Vengeance or temporary buffs | [Reactive effects](Systems/ReactiveCombatEffects.md) | [Modifier ownership](Systems/StatsAndModifiers.md) |
| Use editor tools, tests or build workflows | [Editor and build tools](Reference/EditorAndBuildTools.md) | Relevant content guide and system page |
| Work on another area | [Project map](ProjectMap.md) | Relevant source files and existing reference |

[Balance report directory and viewer](../Reports/BalanceSimulation/README.md) contains dated
compressed simulation outputs, offline bot stats/tree inspection and saved-run recovery.

## Documentation conventions

- Write all new and revised documentation in English. Preserve rule meaning and
  numeric data when translating or consolidating older references.
- Describe current behavior separately from design requirements and proposals.
- Use relative Markdown links between pages, with a route back to this page.
- Link related systems where the dependency matters; avoid duplicate explanations.
- Keep authoritative content rules in their existing guides. System pages explain
  implementation and link to those rules rather than inventing new balance policy.
- Put source paths and verification limits on each technical page. Source paths
  outside this `Docs` vault are repository references for an IDE or file browser;
  they are not additional Obsidian notes.
- Update the affected page when behavior changes. Do not mark a planned check as
  passed, or assume a historical validation report applies to current code.

## Current coverage

The planned overview pass is complete: **seventeen system pages**, plus authoring,
visual and tooling references. Latest pass: **2026-09-28**; each page retains its own
review date and scope. Markdown content in the vault is now in English. Historical
location tuning is consolidated, the tutorial guide is reconciled, and the magic-root
reference lives inside the vault with a compatibility link at its old path.

This is a source-based knowledge base, not a claim that every scene, prefab,
modifier asset or gameplay scenario has been tested. See the
[coverage limits](ProjectMap.md#coverage-limits-and-future-maintenance) for the
remaining verification work. Keep these pages current as implementation changes.
