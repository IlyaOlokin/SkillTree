# Tutorial authoring and window setup

[Home](../Home.md) · [Runtime behaviour](../Systems/Tutorials.md) · [Screen flow](../Systems/MenusAndScreenFlow.md)

Status: **source-reviewed, 2026-09-28**. This guide was translated and reconciled
with the current tutorial code and catalog. Scene wiring and visual layout were
not revalidated in Unity. Queue semantics, event producers, catalog contents and
save behaviour are maintained in the runtime page rather than duplicated here.

## Add or edit a topic

1. Create a definition through **Create > Tutorials > Tutorial**. Use a unique,
   stable ID; changing the title or asset name does not require a new ID.
2. Add it to `Assets/Scripts/Tutorials/TutorialCatalog.asset`. The previous
   `Tutorials/Tutors/TutorialCatalog.asset` path is obsolete. The catalog is assigned
   to GameSceneInstaller's Tutorial Catalog field; content belongs in the catalog.
   Create another catalog through **Create > Tutorials > Catalog** if needed.
3. Set title and paragraph keys, side, triggers, prerequisite topic IDs, excluded
   display locations and minimum player level. Multiple triggers are OR; required
   completed topics are AND. Trigger minimumValue is inclusive and event values
   default to zero. Optional trigger locationId filters where the event happened.
4. Keep catalog order intentional: it resolves ordering when one event matches
   multiple topics. Missing prerequisites, duplicate IDs and dependency cycles
   cause service construction errors.
5. For a new event, add an adapter calling
   `TutorialService.Report("event.id", value, eventLocationId)`. Keep gameplay
   integration out of the presentation and pure service. Do not invoke effect
   factories merely to discover their type; they can allocate runtime modifiers.

Ailment receipt and world-map events are already implemented. Consult the
[current event table](../Systems/Tutorials.md#current-event-producers) before adding
another producer. Mystic-node-specific integrations are outside the reviewed set.

## Connect the window

- Place TutorialWindow on a separately active scene object within the same
  SceneContext injection scope. Its windowRoot must not be the controller itself
  or an ancestor of that controller; hiding the view must not disable the owner.
- Use a top fullscreen Canvas with GraphicRaycaster. Put a fullscreen raycastable
  dimmer Image with CanvasGroup behind the panel, as its sibling rather than its
  parent. Otherwise dimmer alpha also fades the text.
- Assign dimmer, panel, title/body text, close button and skip-all toggle. These
  required references are checked at Start; invalid setup logs an error and
  disables the component. Optional heading/close/toggle labels receive localized text.
- The panel stretches to full height and is anchored left or right per topic.
  `panelWidthPixels` defaults to 480 physical pixels; runtime divides by the root
  Canvas scale factor. Preserve the project's intentional fixed-pixel design.
- Assign `textScroll` for long text so showing a topic resets its scroll to the top.
  Keep the close button and skip-all control outside the scrolling body.
- Add death, location-completion, mini-game and other competing modal roots to
  `blockingWindows`. They defer initial presentation while active.
- Explicitly populate `gameplayInput` with behaviours that read gameplay hotkeys
  outside EventSystem. A dimmer alone does not block Input.GetKeyDown. Do not add
  EventSystem, the window controller, services or components with unintended
  OnDisable side effects. Node/menu pointer handlers already check whether the
  pointer is over UI; TutorialWindow does not discover and disable them automatically.

The prior setup note recorded the MainScene controller at `GlobalCanvas/Tutorial`
and its separate view at `TutorialOverlay/WindowRoot`, using a Constant Pixel Size
CanvasScaler above both cameras. These are historical scene references, not a fresh
hierarchy verification. Inspect actual assignments before moving or replacing UI.
Media playback and guided region highlighting are not implemented by the reviewed
TutorialWindow.

## Editor preview

The custom inspector exposes left/right preview and hide outside Play Mode.
Preview adjusts view objects and marks the scene dirty with Undo support, but does
not report events or alter tutorial progress. Review preview changes before saving
scene layout. Runtime title/body content comes from the selected definition.

## Localization

See [localization runtime](../Systems/Localization.md) for table lookup, fallbacks
and the distinction between text formatting and view refresh.

Title and each paragraphs entry accept a key in the `Tutorial` table or literal
fallback text. Add English entries such as `tutorial.elemental.title` and
`tutorial.elemental.paragraph1`. Shared labels use `ui.tutorial.heading`,
`ui.tutorial.close` and `ui.tutorial.skipAll` with English code fallbacks.

Text is resolved when a topic is shown; TutorialWindow has no locale-change
subscription to refresh an already-visible topic. The old assertion that RU/DE
labels are empty has not been revalidated and is not a current translation-coverage
claim. Check the actual tables when doing localization work. Font sizing is handled
by the view, separately from the topic's localized strings.

## Integration checks

Verify long text and both panel sides, close/toggle interaction, input blocking,
competing modals, consecutive topics without a pause gap, teardown during animation,
and restoration of time scale and selection. Unscaled-time systems and mini-games
need separate checks. The runtime page documents pause ownership and a blocker
check limitation during chained topics.

Closing acknowledges a topic; disabling or destroying the window does not.
Progress is part of the profile snapshot. Test reload with a pending topic and
skip-all enabled. Restored levels are not replayed as level-up events, but the
adapter can report the selected stage after observing an active battle following
load. This replaces the older blanket statement that loading cannot trigger topics.

These checks are recommended for implementation/content changes and were not run
in this documentation-only pass.

## Source entry points

- `Assets/Scripts/Tutorials/TutorialDefinition.cs`
- `Assets/Scripts/Tutorials/TutorialCatalog.cs`
- `Assets/Scripts/Tutorials/TutorialWindow.cs`
- `Assets/Scripts/Tutorials/Editor/TutorialWindowEditor.cs`
- `Assets/Scripts/DI/GameSceneInstaller.cs`
