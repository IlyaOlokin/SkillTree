# Project Map

[Home](Home.md)

This is a navigation map, not a claim that every asset or code path has been audited.
The source survey covers the project's own scripts, existing documentation,
build configuration and regression-test entry points. Third-party libraries and
generated caches are outside the documentation scope.

## Application structure

The project uses Unity **6000.4.6f1**, as recorded in
`ProjectSettings/ProjectVersion.txt`. Build Settings enable `MainMenu` followed by
`MainScene`, both under `Assets/Scenes`.

`Assets/Scripts/DI/GameSceneInstaller.cs` is the gameplay dependency-registration
entry point. It binds the player, tree, enemy spawner, inventory, shop, wallet,
save coordinator, tutorial service and supporting UI/services through Zenject.
Consult the actual bindings before adding a second instance of an existing service.

## Systems and dependencies

| Area | Entry points under `Assets/Scripts` | Documentation status |
| --- | --- | --- |
| Character development | `SkillTree/MainSkillTree.cs`, `SkillTree/Node.cs`, `Battle/UnitLevel.cs` | [Skill tree](Systems/SkillTree.md): first source-reviewed overview |
| Gems and sockets | `Items/Gems`, `Inventory/GemPlacementService.cs`, `SkillTree/SocketNode.cs` | [Gems](Systems/Gems.md): first source-reviewed overview; [bridge reference](Reference/BridgeGem.md) |
| Stats and modifiers | `StatCalculator.cs`, `Battle/BaseUnitModifiers.cs`, `SkillTree/Modifiers` | [Stats and modifiers](Systems/StatsAndModifiers.md): source-reviewed calculation and ownership contracts |
| Combat and effects | `Battle/Unit.cs`, `Battle/AttackProcessor.cs`, `Battle/BattleTickSystem.cs`, `Battle/Effects` | [Combat and effects](Systems/CombatAndEffects.md): source-reviewed pipeline and lifecycle; [effect event contract](Reference/EffectApplicationEvents.md) |
| Defences and combat resources | `Battle/Evasion.cs`, `Battle/Armor.cs`, `Battle/Resistance.cs`, `Battle/Block.cs`, `Battle/Barrier.cs`, `Battle/Health.cs`, `Battle/MysticHealth.cs` | [Defences and resources](Systems/DefencesAndResources.md): source-reviewed formulas, resource consumption and death thresholds |
| Ailments and attack debuffs | `Battle/Effects`, `Battle/AttackEffectPayload.cs` | [Ailments and debuffs](Systems/AilmentsAndDebuffs.md): source-reviewed application, stacking, duration and consumption; remaining buffs are not exhaustively catalogued |
| Reactive resources and buffs | `Battle/Effects`, `SkillTree/Modifiers` | [Reactive combat effects](Systems/ReactiveCombatEffects.md): named effect families and consumption boundaries; individual modifier assets still require targeted inspection |
| Locations and generated enemies | `Battle/Locations`, `Battle/EnemySystem` | [Locations and enemies](Systems/LocationsAndEnemies.md): source-reviewed progression and generation; [authoring rules](Locations/LocationCreationGuide.md), [content history](Locations/ContentHistory.md) |
| Inventory and items | `Inventory`, `Items` | [Inventory and items](Systems/InventoryAndItems.md): source-reviewed slots, ownership and use |
| Economy and loot | `Currency`, `ShopSystem`, `DropSystem` | [Economy and loot](Systems/EconomyAndLoot.md): source-reviewed wallet, shops, gold and reward delivery |
| Saves and profiles | `SaveSystem/GameSaveCoordinator.cs`, `SaveSystem/SaveDataModels.cs`, `SaveSystem/SaveProfileManager.cs` | [Saves and profiles](Systems/SavesAndProfiles.md): source-reviewed lifecycle and recovery |
| Menus and presentation | `MenuTree`, `UI`, `TooltipSystem`, `Visual`, `Camera`, `VFX` | [Menus and screen flow](Systems/MenusAndScreenFlow.md), [HUD and tooltips](Systems/HudAndTooltips.md), [bar contour](UIBarContour.md), [magic root](Reference/ProceduralMagicRootUI.md); visual references distinguish historical checks from this pass |
| Tutorials | `Tutorials` | [Tutorial runtime](Systems/Tutorials.md): queue, events, pause and persistence reviewed; [authoring/setup guide](Tutorials/README.md) reconciled and translated |
| Battle mini-games | `Battle/MiniGames` | [Battle mini-games](Systems/BattleMiniGames.md): source-reviewed spawning, result lifecycle, clocks and rewards |
| Localization | `Localization/GameLocalization.cs`, `MenuTree/MenuLanguageNodeAction.cs` | [Localization](Systems/Localization.md): lookup, formatting, language settings and refresh boundaries reviewed; translation completeness pending |
| Audio | `AudioSystem`, `MenuTree/MenuVolumeZone.cs` | [Audio](Systems/Audio.md): cues, pooling, music and volumes reviewed; scene routing/imports/listening checks pending |

The documented cross-system path is:

**inventory → socket → tree modifiers/power/connectivity → player stat recalculation → attack snapshot → damage and effects**.
Tree and inventory state also enter the same profile snapshot through the save
coordinator. Read [inventory ownership](Systems/InventoryAndItems.md) and
[snapshot boundaries](Systems/SavesAndProfiles.md) when changing either end of that path.

[Locations](Systems/LocationsAndEnemies.md) connect stage progression to generated
enemies and completion rewards. [Economy and loot](Systems/EconomyAndLoot.md)
connects those rewards and enemy gold to inventory, wallet and shop purchase history.

## Content and tooling

Unit geometry and painterly shader controls are described in
[Unit shape rendering](Reference/UnitShapeRendering.md).

| Location | Purpose |
| --- | --- |
| `Assets/EnemyConfig` | Enemy and location content, affixes and balance configuration |
| `Assets/Resources/Items` | Built-in item definitions, including the bridge gem |
| `Assets/Localization` | Localization assets |
| `Assets/Prefabs`, `Assets/Scenes` | Authored objects and scene wiring |
| `Assets/Editor` | Tree editing/analyzing tools, enemy editors and CI build script; [tooling guide](Reference/EditorAndBuildTools.md) |
| `Reports/BalanceSimulation` | Single home for final balance artifacts; dated gzip datasets, offline stats/tree viewers and unfinished recovery data; [report convention](../Reports/BalanceSimulation/README.md) |
| `Tools/BalanceSimulation` | External Runtime adapters, Internal orchestration, offline recovery and progression campaigns with XP/allocation/rewards; [scope and measured throughput](../Tools/BalanceSimulation/README.md) |
| `Tests/CombatRegression` | .NET 9 combat checks using production files and Unity/game stubs |
| `Tests/SaveRegression` | .NET 9 save regression checks using stubs and disposable saves |
| `.github/workflows` | Manual Windows/WebGL build and itch.io deployment workflows |
| `Docs/Reference` | Detailed contracts and procedures: [effect application events](Reference/EffectApplicationEvents.md), [bridge gem](Reference/BridgeGem.md), [editor/build workflows](Reference/EditorAndBuildTools.md) and [procedural UI reference](Reference/ProceduralMagicRootUI.md); the old Documentation path is a compatibility link for the procedural UI reference |

The location guide refers to `Tools/Locations/check_locations.py`. That path was
absent during this survey. Do not report that check as passed or create a replacement
implicitly; restore or locate the intended tool when a location-content task needs it.

## Coverage limits and future maintenance

Recorded findings and follow-up checks are collected in the [issue backlog](Backlog.md).

The planned source overview and Markdown language migration are complete as of
2026-09-28. This map now routes every previously listed area to an overview or
specialized reference. It is not an exhaustive implementation or asset audit.

Future work should be driven by actual changes or explicit verification tasks:

- Validate scene/prefab assignments, camera/Canvas/VFX composition and UI appearance
  in Unity; the overview cannot prove all components are wired or active.
- Audit translation completeness, font coverage, actual mixer routing, clip imports
  and target-device performance when those areas are changed.
- Playtest balance and integration; inspect specific reactive modifier producers
  and authored assets before changing triggers or numeric rules.
- Restore/locate the missing location checker for content validation, and confirm
  runner/tool availability before using build or deployment workflows.
- Treat source-observed issues documented on system pages as findings, not fixes
  already applied or approved design requirements.

The documentation passes ran link/anchor/source-path checks, not Unity builds,
playtests or deployments. Historical render checks remain explicitly historical.
