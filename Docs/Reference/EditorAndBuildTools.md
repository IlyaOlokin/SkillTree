# Editor tools, checks and builds

[Home](../Home.md) · [Project map](../ProjectMap.md)

Status: **source-reviewed entry-point map, 2026-09-28**. Build script/workflows and
editor entry points inspected. No editor tool, test suite, build or deployment was
executed for this documentation pass. Inspector internals are not exhaustively audited.

## Recorded performance investigations

- [allocationTest / allocationTest2 analysis](PerformanceCaptureAllocationTest.md):
  complete single/path allocation and refund segments, queue-dispatch and text costs,
  first-use audio/hierarchy hitches, remaining save spikes and proposed redesigns.

- [NoAllocationEffect Editor capture](PerformanceCaptureNoAllocationEffect.md):
  allocation segment with 2,812 NodeVisual callbacks per frame and approximately
  10.4 ms each in MainSkillTree.Update and aggregate NodeVisual.LateUpdate.

- [NoAllocationEffect Editor capture](PerformanceCaptureNoAllocationEffect.md):
  allocation segment with 2,812 NodeVisual callbacks per frame and approximately
  10.4 ms each in MainSkillTree.Update and aggregate NodeVisual.LateUpdate.

- [2026-10-01 Editor Profiler capture](PerformanceCapture20261001.md): measured
  skill-tree CPU render preparation, synchronous autosave hitches and allocations.
  This is one Editor recording, not a player-build performance gate.

## Content tools

| Entry point | Purpose / boundary |
| --- | --- |
| Skill Tree Tool editor tool | Scene node editing; includes mouse input, Delete/Backspace and prefab-selection arrows; inspect current config before use |
| Tools/Skill Tree Analyzer | Node/modifier statistics; supports node-power-aware analysis; not automatic approval of content balance |
| Tools/Modifier Creator | Modifier-asset authoring; shared assets can affect several nodes |
| EnemyGenerationEditors | Inspectors for profiles, attack/speed/defence/utility modules and enemy databases |
| EnemyLevelPowerConfigEditor | Inspector for stage-power content |
| NodeConnectionRendererEditor / MenuConnectionRendererEditor | Connection rendering inspectors; skill-tree menus can strip or force-create spline objects in open scenes |
| TutorialWindowEditor | Edit-mode preview; changes scene objects with Undo and marks the scene dirty, without completing tutorials |
| ProceduralMagicRootUIEditor | Inspector controls for saved visual styles; see [rendering reference](ProceduralMagicRootUI.md) |

Read the [tree content guide](../SkillTree/SkillTreeFillingGuide.md) or
[location guide](../Locations/LocationCreationGuide.md) before content work.
The intended location checker remains absent; its guide records the limitation.
Connection cleanup commands mutate scenes and should not be treated as read-only
analysis. Preserve save IDs, prefab overrides and ongoing user changes.

`Assets/Editor/CodexTwoClusters.cs` is a specialized helper, not the normal editor
workflow. It registers an editor update callback and uses `Temp/codex-two-*` files
for dump, plan, completion and framing requests, including scene/asset writes.
Do not create its request files merely to inspect the tree. Its presence is recorded
here; it was not removed because this pass does not establish whether another task
still uses it.

## Compiler warning maintenance (2026-10-03)

The Unity 6000.4 warning pass replaced deprecated `FindFirstObjectByType` fallback
lookups with `FindAnyObjectByType`, keeping each active/inactive filter. Explicit
Inspector references still take priority where present; fallback searches must not
rely on instance-ID order when multiple candidates exist. Unsorted
`FindObjectsByType` calls now use the overload without `FindObjectsSortMode`.
`NodePair`, `MenuNodePair` and the tree editor compare `GetEntityId()` values through
`CompareTo`; pair hashes use `EntityId.GetHashCode`. These are in-session identities,
not persisted save IDs. No scene, prefab or gameplay asset was rewritten.

Runtime static ownership is reset before scene startup for
[GameAudio](../Systems/Audio.md#runtime-ownership) and
[TooltipTermDatabase](../Systems/HudAndTooltips.md#tooltip-ownership-and-nesting).
UDR0001/UDR0004 suppressions cover only inspected false positives, with inline
comments and immediate `restore` directives:

- The connection-renderer inspectors unsubscribe in `OnDisable`.
- [GameSaveCoordinator](../Systems/SavesAndProfiles.md#autosave-and-profile-switching)
  unsubscribes in Zenject `Dispose`, not a MonoBehaviour callback.
- `MissingNodeSaveIdAssignment` and `CodexTwoClusters` use `InitializeOnLoad`
  subscriptions for the lifetime of the Editor domain. Their Editor state must
  survive Play Mode transitions; runtime initializers are not appropriate here.

The Input Manager deprecation warning requires a separate input migration. The
project currently enables both input backends and still calls `UnityEngine.Input`;
changing that setting alone would break those consumers. It was left unchanged.
BL-029 remains open: the warning cleanup does not establish whether the specialized
cluster helper is still needed or safe against stale request files.

Verification: Unity CLI `recompile` only refreshed assets and retained diagnostics
from the previous compile after the last pragma-only edits. A direct
`CompilationPipeline.RequestScriptCompilation()` through CLI `eval` completed
successfully; `recompile_status` reported `completed`, `failed: false`, and
`console_status` reported **0 current warnings and 0 errors**. The captured log
buffer still contains historical messages and is not the current Console count.
`git diff --check` and relative documentation-link checks passed. No test suite,
player build, scene rebuild or gameplay playtest was run.

## Existing automated checks

| Command from repository root | Scope |
| --- | --- |
| `dotnet run --project Tests/CombatRegression` | Selected production combat files with Unity/game stubs; arithmetic, callbacks and ownership cases |
| `dotnet run --project Tests/SaveRegression` | Save behavior with stubs and disposable save data; consult its README for exact coverage |

These require .NET 9 and do not replace Unity compilation, scene wiring, rendering,
browser persistence or performance tests. Documentation-only changes use Markdown
link/anchor checks, source-path existence checks and diff whitespace checks.
Never label a listed command or an older report as a test executed now.

## Build entry points

`Assets/Editor/CI/BuildScript.cs` exposes BuildScript.BuildWindows and
BuildScript.BuildWebGL for batch execution. Both use enabled Build Settings scenes,
set PlayerSettings.bundleVersion from `-buildVersion` (default 0.0.0), delete and
recreate their target output directory, and use BuildOptions.None.

- Windows target: StandaloneWindows64, output `Builds/Windows/Game.exe`.
- Web target: WebGL, output `Builds/WebGL`.
- `-buildNumber` defaults to 0 and is logged; the inspected code does not apply
  it to a platform build-number setting.
- Missing scenes or an unsuccessful report cause EditorApplication.Exit(1);
  success exits the Editor with code 0. This is a batch entry point, not a harmless
  method to invoke in an interactive Editor session.

## Repository workflows

`.github/workflows/build.yml` is manually dispatched with a version. Separate
Windows and WebGL jobs run on a self-hosted Windows/unity6 runner, use the configured
Unity 6000.4.6f1 executable, retrieve Git LFS content and invoke the corresponding
build method. They upload build logs and version-named artifacts; build retention
is 30 days. No regression-test step is present in the inspected workflow.

`.github/workflows/deploy-itch.yml` is a separate manual deployment of artifacts
from a supplied build run ID and matching version. It requires ITCH_USER/ITCH_GAME
repository variables, BUTLER_API_KEY and the configured Butler executable. It pushes
the Windows and WebGL channels with the supplied user version; it does not rebuild.
Review artifact identity before dispatching. This document does not authorize a
deployment or assert runner credentials/tool availability.

## Source entry points

- `Assets/Editor/SkillTreeEditorTool.cs`
- `Assets/Editor/SkillTreeAnalyzerWindow.cs`
- `Assets/Editor/ModifierCreatorWindow.cs`
- `Assets/Editor/EnemyGenerationEditors.cs`
- `Assets/Editor/EnemyLevelPowerConfigEditor.cs`
- `Assets/Editor/NodeConnectionRendererEditor.cs`
- `Assets/Editor/MenuConnectionRendererEditor.cs`
- `Assets/Editor/CodexTwoClusters.cs`
- `Assets/Editor/CI/BuildScript.cs`
