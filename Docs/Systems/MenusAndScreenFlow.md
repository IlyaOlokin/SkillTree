# Menus and screen flow

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Tutorials](Tutorials.md)

Status: **source-reviewed, 2026-09-28**. Covers menu-node actions, gameplay modes,
death/completion windows and modal boundaries. Scene event assignments, complete
HUD/tooltip rendering and visual effects remain outside this page's source pass.
Companion pages cover [HUD/tooltips](HudAndTooltips.md), [mini-games](BattleMiniGames.md),
[volume controls](Audio.md) and [language selection](Localization.md).
No Unity playtest or scene appearance check was performed.

## Main menu graph

`MenuTreeController` owns a graph reachable from its root, caches nodes and forwards
node changes. Nodes reachable from another controller's graph produce a warning
and are not assigned to this controller. `RefreshGraph` rebuilds subscriptions and
ownership after graph changes.

This graph has its own `MenuNode` and actions, separate from the character's
[skill tree](SkillTree.md). Allocation requires an unallocated nonpersistent node,
an allocated path to the root, zone permission and any additional allocation
condition. Mouse input ignores clicks over EventSystem UI; left-click allocates
and right-click requests deallocation through the owning controller.

A state change emits node notifications before invoking its action. Each action
runs its override first and then its serialized UnityEvent. Inspect scene/prefab
event assignments before changing a transition: the C# subclass is not necessarily
the only configured behaviour.

| Action | Behaviour |
| --- | --- |
| MenuSceneNodeAction | Load the configured scene/mode on allocation; warn for an empty scene name |
| MenuSaveProfileNodeAction | Activate/create the selected profile slot, then optionally focus the camera before scene load |
| MenuFocusNodeAction | Focus a configured target, a node focus component or its transform; deallocation focus is optional |
| MenuBackNodeAction | Request ResetToRoot on the owning graph |
| MenuLanguageNodeAction | Resolve/select a locale and persist its code through CloudSettingsService; initial locale selection waits for localization initialization |

Profile slot numbers exposed by the action are one-based; its service receives
`max(0, slotNumber - 1)`. An unsuccessful activation prevents the scene transition.
An empty scene name can still leave a profile activated. Camera auto-resolution
only chooses a controller when exactly one exists. Profile lifetime and save
contracts are detailed in [saves and profiles](SavesAndProfiles.md).

## Deallocation and single-selection zones

`MenuLimitedZone` enforces a single selected member. Selecting another member
deallocates the previous one; direct deallocation of a member is refused. This
supports settings choices that must retain a selection.

The tree's ResetToRoot restores authored default allocation states for eligible
nodes, preserving persistent roots and zone-protected selections. Its name does
not mean every non-root node is always cleared.

**Implementation nuance:** TryDeallocateNode checks `node.CanDeallocate()` before
running optional dependent-branch collapse. That node check refuses disconnection
when allocated neighbours would lose their root path. The collapse option therefore
does not guarantee that right-clicking a parent can close an allocated branch.
Verify intended UX on the actual graph before changing this contract.

## Gameplay modes and transitions

`LocationFlowController` switches the configured Map/Battle/Shop roots and publishes
OnModeChanged. Its startup path enters the selected location for a fresh profile;
with start-in-map enabled, an existing profile returns to the map. Location
selection and entering the selected location are separate operations.

| Destination | Combat work |
| --- | --- |
| Battle | Activate player, request stat recalculation, reset combat resources, resume ordinary ticks, enter battle |
| Shop | Pause ticks, exit battle, activate/recalculate/reset player, show shop mode, publish OnShopEntered |
| Map | Pause ticks, exit battle, activate/recalculate/reset player, show map mode; report map tutorial after first-location completion |

`ShopWindowPresenter` rebuilds slots from OnShopEntered and delegates purchases to
ShopService. Screens display state; [economy and loot](EconomyAndLoot.md) owns
purchase validation, stock and reward-delivery rules. Location unlocks, stage
progress and wave generation are described in [locations](LocationsAndEnemies.md).

## Death, completion and tutorials

The death window subscribes to player death and pauses battle ticks before showing.
Restart closes it, activates/resets the player, restarts the current stage and
resumes ordinary ticks. Exit returns to the map, or exits battle and leaves ticks
paused if no location flow controller exists. Map entry and battle deactivation
also close the window. Its animation uses an ordinary DOTween sequence, whereas
the tutorial animation explicitly uses unscaled time.

The completion window listens to the spawner's first-completion event and pauses
ticks. That event may represent a newly completed boss stage inside a location,
not only its final stage. Reward collection gates exit; delivery and the current
failed-insertion issue are documented in [economy and loot](EconomyAndLoot.md).

Tutorials use a distinct owner-based combat pause plus time-scale/input handling.
Death/completion/map/shop use ordinary Pause/Resume calls. A Resume cannot remove
the tutorial's owner lock. Add competing window roots to TutorialWindow blockers;
there is no general modal stack automatically coordinating all these controllers.
See [tutorial presentation](Tutorials.md#presentation-and-pause-ownership) for chain
behaviour and cleanup.

## Verification and source entry points

Recommended checks: overlapping graph ownership; root connectivity; branch close
with allocated children; switching zone choices; default reset; profile activation
failure; camera callback before scene load; fresh/existing profile startup; death
restart/exit; completion rewards; overlapping tutorial/death/completion windows.
These are suggested scenarios, not executed tests for this documentation pass.

- `Assets/Scripts/MenuTree/MenuTreeController.cs`
- `Assets/Scripts/MenuTree/MenuNode.cs`
- `Assets/Scripts/MenuTree/MenuNodeInputHandler.cs`
- `Assets/Scripts/MenuTree/MenuLimitedZone.cs`
- `Assets/Scripts/MenuTree/MenuNodeAction.cs`
- `Assets/Scripts/MenuTree/MenuSceneNodeAction.cs`
- `Assets/Scripts/MenuTree/MenuSaveProfileNodeAction.cs`
- `Assets/Scripts/MenuTree/MenuFocusNodeAction.cs`
- `Assets/Scripts/MenuTree/MenuBackNodeAction.cs`
- `Assets/Scripts/MenuTree/MenuLanguageNodeAction.cs`
- `Assets/Scripts/Battle/Locations/LocationFlowController.cs`
- `Assets/Scripts/UI/PlayerDeathWindowController.cs`
- `Assets/Scripts/UI/LocationCompleteWindowController.cs`
- `Assets/Scripts/UI/ShopWindowPresenter.cs`
