# Editor tools, checks and builds

The [standalone Balance Lab](BalanceLab.md) launches from
`Tools/BalanceSimulation/LaunchLab.cmd` without Codex. It provides settings,
stop/resume, persisted batch learning, frozen model evaluation and paired balance
comparisons. See its workflow and bounded verification before interpreting results.

The balance agent's [stage-one simulator contract](BalanceAgentStage1.md) records
the corrected campaign tick/kill wiring, two identical bounded runs and explicit
remaining fidelity limits. `Tools/BalanceSimulation/VerifySimulator.ps1` repeats
the live contract diagnostic and reproducibility check without gameplay edits.

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

All-node policy v3, 2026-10-09: the owner removed numeric-only node coverage and
requested a general directional major-node estimate of 3.25 small nodes. Concrete
node subtype and infinite investment are preserved; no individual node ID or
modifier allowlist controls eligibility. See [v3 scope and verification](../../Tools/BalanceSimulation/README.md#all-node-directional-policy-v3-2026-10-09).
Compilation and JavaScript syntax passed; live v3 simulation is not yet verified.

Live all-node v3 follow-up, 2026-10-09: [15 balanced campaigns](../../Reports/BalanceSimulation/20261009-161427-810-campaign/verification.md)
finished after Pipeline restart. Catalog coverage admitted all 2,812 nodes, with
458 major/special allocations and 137 gem purchases/insertions. Mean cleared
stages were 95 / 93 / 93 (STR / DEX / INT-lightning). Final snapshots, invested
counts, archive/viewer bytes and actor cleanup were checked. No repeated infinite
investment appeared; adaptive respec and exhaustive special-modifier coverage
remain unverified. This supersedes the preceding live-v3-pending note.

Representative parity verifier, 2026-10-09: `VerifyPlayMode.ps1` compiles an external
fixture, captures Edit Mode traces, and runs native Awake/Start/Update/destruction
in a disposable Unity project with local packages and gameplay DLL copies.
`ComparePlayTraces.ps1` requires full arrays, exact event counts, bounded float
serialization error and actual expected transitions. [Four scenarios matched](../../Reports/BalanceSimulation/20261009-175543-107-play-parity/verification.md)
across 750 ticks; no gameplay files were edited. Cold isolated-project import may
take several minutes. See the [stage-one scope](BalanceAgentStage1.md).

## Pipeline connection investigation, 2026-10-12

Read-only checks initially found Unity 6000.4.6f1, PID 10412, Pipeline
0.7.0-exp.1 and a ready server on port 7801. Later, CLI discovery returned
`STATUS_NO_INSTANCES` in the sandbox and `STATUS_PIPELINE_LOAD_PENDING`
outside it, although an outside-sandbox HTTP probe still received 401 and
Windows reported port 7801 listening under the same Unity PID. The unauthenticated
401 confirms a live HTTP server; it does not establish that CLI authentication works.

Two distinct problems were observed:

- The sandbox rejected direct loopback access with a socket-permission error.
  CLI absence messages alone therefore cannot establish that Unity is closed.
- `Library/Pipeline/.unity-pipeline-port` was absent both inside and outside
  the sandbox while the listener remained alive. Installed package source
  (`Runtime/Models/InstanceDescriptor.cs`) uses this file for discovery and
  stores the authorization token there. The reason it disappeared was not
  established; no token was printed or manually recreated.

The Editor log also contains repeated HTTP server starts and assembly reloads.
`Editor/EditorPipelineStartup.cs` recreates the server through `InitializeOnLoad`
after domain reload. Such transitions can temporarily interrupt commands, but
they do not explain every observed discovery failure. Safe Mode was not detected.
No package upgrade, server restart, scene change, build or test was performed.
For follow-up, restore the descriptor through the package's server lifecycle,
then check discovery separately from loopback access. Do not disable the sandbox
or treat an available package update as a verified fix.

### Connection recovery changes, 2026-10-12

Pipeline was upgraded from 0.7.0-exp.1 to 0.8.0-exp.1 using
`unity pipeline upgrade --project-path C:/UnityProjects/SkillTree`.
The upgrade is maintenance, not evidence that the missing-file cause is fixed.
No project C# usage of the moved `CliCommand`/`CliArg` namespace was found.
Version 0.8 uses Project Settings > Pipeline > Editor; old settings assets are
not migrated by the package. No old settings asset was changed by this task.

`Assets/Editor/PipelineDiscoveryRecovery.cs` checks every five seconds in the
Editor update loop. If the discovery file is absent and Pipeline's server is
running, it invokes the server's own heartbeat writer, preserving token ownership
and file permissions. It never starts a manually stopped server. Reflection hooks
were source-checked against 0.8 (`PipelineServerStartup.Server`, `IsRunning`,
`UpdateHeartBeat`); future upgrades require review. Recovery needs an updating
Editor and cannot run while a domain reload or an Editor stall prevents updates.

Project AGENTS.md now requires live Unity CLI calls outside the restricted shell,
with an explicit project target. This addresses the observed socket/process access
restriction without disabling the sandbox. Commands that time out after mutation
must be checked for completion rather than automatically repeated.

Live verification after import: `unity status` and `unity command editor_status`
reported ready, not compiling, and no domain reload, using PID 10412/port 7800.
A single live `eval` verified that the recovery class compiled, moved the
descriptor to a temporary backup, invoked the recovery check, and returned
`descriptorRestored: true`. A finally block removed the backup after successful
recovery or would have restored the original on failure. No token contents were
printed. A subsequent editor_status call verified discovery/authentication again.
No test suite or player build was run. The source of the original descriptor
removal remains unknown; this change repairs that failure mode rather than
claiming to identify the deleting process.

The retained legacy settings asset had automatic port selection, AutoStart and
watchdog enabled with a five-second interval, and request logging/browser access
and source telemetry storage disabled. It remains untouched; 0.8 ignores it and
uses its new settings configuration.
