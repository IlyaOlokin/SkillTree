# Editor tools, checks and builds

Campaign tooling update, 2026-10-08: the external campaign adapter now models
numeric local gems, socket allocation, production shop purchases/stock and gold
completion rewards. The three-attribute `balanced-shops.json` preset selects
strength, dexterity and intelligence/lightning; five runs per strategy means
15 campaigns. See [campaign shops and local gems](../../Tools/BalanceSimulation/README.md#campaign-shops-and-local-gems-2026-10-08)
for policy and coverage. Earlier campaign records exclude these additions.

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

### Isolated balance runner (2026-10-04)

The [isolated balance simulator](../../Tools/BalanceSimulation/README.md) lives outside
Assets. `Run.ps1` uses `Runtime/FixedBattle.cs` for independent numeric builds;
`RunCampaign.ps1` uses `Runtime/Campaign.cs` for actual XP, allocation, boss rewards,
simple items and location progression. Production combat advances at 1/60 second
with disposable preview-scene actors. It requires MainScene in Edit Mode, restores
Random state and does not use the profile-save path.

`Internal/UnityBridge.ps1` caches each adapter delegate by source hash in the Editor
AppDomain. Later calls compile a small dispatcher; source changes/domain reloads
invalidate the cache. `Runtime/EditorEffectOwnership.cs` owns transient effect
modifiers for immediate Edit Mode cleanup. Large payloads are atomically written
through `Runtime/EditorExports.cs`, avoiding large CLI response serialization.

[Reports/BalanceSimulation](../../Reports/BalanceSimulation/README.md) is the only
result home. Finished runs contain gzip JSON, a portable offline HTML stats/tree
viewer and Markdown summaries. `report.json.work` contains unfinished metadata,
graph and one export per completed campaign; full publication deletes it.
`Recover.ps1` validates/assembles saved campaigns offline, with explicit partial
opt-in. It cannot resume a partly played bot. `RebuildReport.ps1` regenerates viewers
and migrates legacy raw JSON after round-trip equality validation.

The default three-build preset prioritizes strength/physical, dexterity/physical
and intelligence/lightning damage, with two-edge lookahead and earned-point
spending. Policies are greedy and exclude shops, gems, infinite nodes and special
node mechanics. Fixed battles exclude XP/rewards. See the runner's scope rather
than interpreting these comparisons as optimal builds or complete balance coverage.

Pipeline's five-second operation/response limit still applies. Chunk budgets stop
at full stage boundaries; an unusually expensive single stage can still exceed it.
The 2026-10-04 reorganization compiled both external adapters and ran three fixed
fights plus three bounded two-location campaigns. Archive equality and JavaScript
syntax were checked; no game/player build or new test suite was run. Viewer
appearance/live interaction remain unverified due to blocked local-file browsing.
The retained 30-bot report was used for a same-data compression comparison, then
restored to uncompressed JSON/HTML at the owner's request; it was not replayed.
The owner subsequently removed previous run folders. `Presets/balanced-push.json`
defines five balanced policies (strength, dexterity, intelligence/fire/lightning/cold)
with equal offensive/defensive category weights; these are policy comparisons.

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

## Adaptive balance-tool follow-up, 2026-10-04

The isolated campaign adapter now performs controlled post-defeat tree search with
frozen level/XP/points and identical per-round wave seeds. It records proposals,
score deltas, commits and normal-stage confirmation; it retains winning layouts and
growth priorities. This is experimental respec support outside Assets, not a change
to production allocation/refund/save behavior. Probe XP/gold/rewards/progress are
suppressed, and trial wave/time totals are separate in reports. The bounded pilot
compiled through Unity CLI: two substituted nodes improved the frozen stage-7
result from failure to victory, then normal progression confirmed it. Full adaptive
campaign execution is recorded in the report directory when completed.

Decision-policy v2, 2026-10-08: candidate stats now use an isolated production
recalculation and representative current-stage opponents. Nodes use a bounded
three-node path search; gems require positive projected utility, with caps and
mechanic prerequisites. See [decision scope and verification](../../Tools/BalanceSimulation/README.md#counterfactual-decision-policy-v2-2026-10-08).
Compilation and scoped metric checks passed; live v2 campaigns are not verified.
The earlier 15-campaign report records v1 and was not rerun or overwritten.

Live v2 follow-up, 2026-10-08: [15 balanced campaigns](../../Reports/BalanceSimulation/20261008-192754-223-campaign/verification.md)
completed after Pipeline restart, with mean cleared stages 51 / 95 / 15 for
strength / dexterity / intelligence-lightning. All 64 purchases had positive v2
scores; no BarrierCapacity gem was purchased. Final snapshot gold/socket counts
and temporary actor cleanup were checked. The catalog also records 238 attribute
nodes changed from 2 to 1, so the comparison is not algorithm-only. This supersedes
the earlier live-execution-pending note; other policy limits remain.
