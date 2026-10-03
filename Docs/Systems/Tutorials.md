# Tutorial progression and presentation

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Authoring and setup](../Tutorials/README.md)

Status: **source-reviewed, 2026-09-28**. Runtime code and the tutorial catalog were
inspected. Scene wiring, visual layout and gameplay were not tested in Unity.

## Responsibilities

| Owner | Responsibility |
| --- | --- |
| TutorialDefinition / TutorialCatalog | Stable IDs, localized content, triggers, prerequisites and catalog order |
| TutorialService | Pure queue/progress state, eligibility and current presentation |
| TutorialEventAdapter | Translate combat/progression events into reports |
| TutorialWindow | Show one topic, block input, own pause and acknowledge closing |
| GameSaveCoordinator | Restore, suspend and save tutorial progress with the profile |

`GameSceneInstaller` binds one service from its assigned catalog, or an empty
definition array if none is assigned, and installs the event adapter. The service
rejects missing definitions, empty/duplicate IDs, missing prerequisite IDs and
dependency cycles during construction.

## Remember first, display when eligible

`Report(eventId, value = 0, eventLocationId = null)` is ignored until the service
is ready, or when skip-all is active. Triggers use OR, exact event ID, `value >=
minimumValue` and an optional exact event-location filter. A match adds a pending
tutorial even when its display conditions are not satisfied yet. Duplicate reports
do not duplicate pending or completed topics.

Pending order follows event arrival; catalog order breaks ties within one event.
`TryBeginNext` scans for the first eligible pending ID, skipping blocked or unknown
IDs rather than letting them stall the whole queue. Eligibility uses current
player level, current excluded locations and **all** completed prerequisites.
Event-location filtering and current-location eligibility are different checks.

Beginning a topic leaves its ID pending. Closing marks it completed and removes
it from pending. Closing with skip-all clears the queue and suppresses future
topics too, including content added to the catalog later. Cancelling presentation
only clears `Current`; scene teardown does not acknowledge the topic.

## Current event producers

| Event | Value / source |
| --- | --- |
| `player.level` | New player level from UnitLevel |
| `location.level` | Selected absolute stage while battle is active; also reported once when the adapter observes a newly active battle |
| `wave.cleared` | Cleared wave number before automatic progression can reset it |
| `player.hit.elemental` | EnemyUnit hit with positive fire/cold/lightning remaining after barrier receipt |
| `player.hit.mystic` | EnemyUnit hit with positive light/darkness remaining after barrier receipt; not based on HP loss |
| `player.ailment.received` | Player `OnEffectAdded` reports Bleed, Ignite, Chill or Overcharge, including stack updates |
| `world_map.opened` | LocationFlowController returns to the map after `level-1` is completed; event location is explicitly `level-1` |

The ailment event does not require an EnemyUnit source. It listens to effect-list
notifications, not the source-side effect event. A fully barrier-absorbed elemental
or mystic hit does not trigger its corresponding hit topic. An evaded attack does
not reach the receipt callback. See [combat](CombatAndEffects.md) and
[effect events](../Reference/EffectApplicationEvents.md) for these distinct boundaries.

## Catalog snapshot

The inspected `Assets/Scripts/Tutorials/TutorialCatalog.asset` references these
definitions in this order. This table records current authoring, not mandatory
rules for future content.

| ID | Trigger | Required completed topic |
| --- | --- | --- |
| `combat_basics` | `location.level >= 1`, event location `level-1` | None |
| `choose_class` | `location.level >= 1`, event location `level-1` | `combat_basics` |
| `experience` | `player.level >= 2`, any location | None |
| `damage_types` | `location.level >= 3`, event location `level-1` | None |
| `ailments` | `player.ailment.received >= 0`, event location `level-2` | `damage_types` |
| `world_map` | `world_map.opened >= 0`, event location `level-1` | None |

## Presentation and pause ownership

In `LateUpdate`, TutorialWindow refreshes context and checks readiness, its own
transition/pause state and configured active blocking windows. It then acquires
a BattleTickSystem pause lock, remembers `Time.timeScale`, sets it to zero and
disables only the explicitly configured enabled gameplay-input behaviours.
The fullscreen dimmer blocks pointer input; it does not intercept raw hotkeys.

Animations run on unscaled DOTween time. Text/localization is resolved on showing
each topic. Closing is disabled during transitions. Ready topics chain without
releasing pause between them. The chained path calls `TryBeginNext` directly and
does not recheck `blockingWindows`; those blockers gate the initial LateUpdate
entry, not every transition in an already-open chain.

Reset kills the tween, hides the view, releases its own combat lock, restores the
saved time scale and input components, cancels presentation, and restores the
previous EventSystem selection if still active. Disable, destruction and profile
reset use this cleanup. Other combat pause owners remain effective.

There is no shared time-scale ownership manager here. Another system changing
time scale during this modal can conflict with restoring the remembered value.
Check overlapping modals and systems using unscaled clocks when integrating a view.
See [menus and screen flow](MenusAndScreenFlow.md) for the other pause owners.

## Persistence and load boundary

The saved data contains `skipAll`, completed IDs and ordered pending IDs. Restore
deduplicates pending IDs, removes completed ones, clears context/current state and
raises ProfileReset before becoming ready. Unknown IDs are retained in progress;
unknown pending topics are skipped until a matching definition exists.

The save coordinator suspends reporting during load/reset. Restored player levels
do not themselves replay a level-up event, but the adapter explicitly catches an
already-active battle and reports its selected stage after readiness. Therefore
loading is not a blanket guarantee that no tutorial report can occur. Stable IDs
and the saved completed/pending sets prevent repeat completion topics.

## Verification and sources

Recommended checks: one event matching several topics; dependent topics queued
before prerequisites; excluded locations; duplicate reports; save while a topic
is visible; reload into an active battle; skip-all then add content; modal chains;
teardown during tweening; restoration of input and independent pause owners.
These were not run in this documentation-only pass.

- `Assets/Scripts/Tutorials/TutorialService.cs`
- `Assets/Scripts/Tutorials/TutorialProgress.cs`
- `Assets/Scripts/Tutorials/TutorialDefinition.cs`
- `Assets/Scripts/Tutorials/TutorialCatalog.cs`
- `Assets/Scripts/Tutorials/TutorialEventAdapter.cs`
- `Assets/Scripts/Tutorials/TutorialWindow.cs`
- `Assets/Scripts/Battle/Locations/LocationFlowController.cs`
- `Assets/Scripts/DI/GameSceneInstaller.cs`
- `Assets/Scripts/SaveSystem/GameSaveCoordinator.cs`

Related: [saves](SavesAndProfiles.md), [locations](LocationsAndEnemies.md),
[authoring/setup](../Tutorials/README.md).
