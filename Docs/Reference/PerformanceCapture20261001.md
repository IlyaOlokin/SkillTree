# Performance capture: 2026-10-01

[Home](../Home.md) · [Editor and build tools](EditorAndBuildTools.md) · [Backlog](../Backlog.md)

Analyzed on **2026-10-01**. Source: [Unity Profiler capture](../../ProfilerCaptures/SkillTree_2026-10-01_21-14-37.data) and its companion PNG/highlights. This is a CPU Profiler recording, not a Memory Profiler object snapshot. The recording contains EditorLoop and RenderPlayModeViewCameras: results describe an Editor session, not standalone-player performance.

## Method and limits

Unity **6000.4.6f1** loaded the capture with `ProfilerDriver.LoadProfile` in an isolated temporary project under `Temp/ProfilerAnalysisProject`. `RawFrameDataView` exported all 542 frames' thread metadata, main/render-thread samples and available memory/rendering counters. Samples were aggregated by marker name; inclusive parent and child times must not be added together. Frame indices below are zero-based exported indices, 0–541. The extraction did not change gameplay code, scenes, assets or saves and did not run project tests or builds.

Temporary extraction sources and CSV-style TSV exports are retained under `Temp/ProfilerAnalysisProject`; `summary.json` and `details.json` contain aggregates. These temporary files are not durable documentation dependencies. Native methods and their nested managed calls are not fully instrumented, so a large marker identifies an investigation boundary rather than its precise internal operation. API reference: [Unity ProfilerDriver source](https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/ProfilerEditor/Public/ProfilerAPI.bindings.cs).

GPU frame times are zero throughout the export; GPU performance cannot be assessed. Render-thread waiting on main-thread commands is not GPU work. Input, zoom level and camera movement were not recorded as explanatory events; changes between capture segments cannot be attributed to those actions from this evidence alone.

## Frame distribution

| Metric | Main-thread frame time |
| --- | --- |
| Frames / summed duration | 542 / 18.499 seconds |
| Mean | 34.131 ms (29.3 frames/s from frame count / duration) |
| Median | 10.461 ms |
| 95th / 99th percentile | 76.662 / 110.972 ms |
| Minimum / maximum | 7.327 / 365.234 ms |
| Above 16.667 / 33.333 ms | 232 / 208 frames |
| Above 50 / 100 ms | 198 / 10 frames |

The mean hides two very different operating regimes. Illustrative contiguous segments, not detected gameplay states:

| Frames | Average PlayerLoop | Average QueuePrepareIntegrateMainThreadObjects |
| --- | --- | --- |
| 0–139 | 68.151 ms | 56.889 ms |
| 140–479 | 8.748 ms | 2.189 ms |
| 480–541 | 54.758 ms | 44.292 ms |

## Findings and priority

### 1. Skill-tree CPU rendering preparation dominates heavy segments

`QueuePrepareIntegrateMainThreadObjects` totals **11.455 seconds**, about **61.9%** of summed main-thread frame time, averaging **21.134 ms/frame** with a median of **1.037 ms** and a per-frame maximum of **63.545 ms**. It sits under `CullScriptable → CullResults.CreateSharedRendererScene → EndRenderQueueExtraction`. The inclusive `Inl_UniversalRenderPipeline.RenderSingleCameraInternal: SkillTreeCamera` marker totals **11.917 seconds** (21.988 ms/frame), versus **0.345 seconds** for BattleCamera (0.637 ms/frame).

This is the first performance investigation target. Check the skill-tree camera's renderer population and sprite-mask participation in a comparable player capture, then isolate those factors individually. Main-thread `SpriteMask.Render` appears **516,190 times** (about 952 samples/frame), but its measured own duration totals only **49.110 ms**. The count motivates investigation; it does **not** prove sprite masks cause the expensive queue marker. The capture does not establish a shader/GPU bottleneck or justify removing visual effects blindly.

### 2. Synchronous autosave causes distinct gameplay hitches

`SaveSystem.GameSaveCoordinator.Tick()` exceeds 20 ms on **nine frames**: 0, 24, 37, 65, 91, 128, 304, 496 and 526. These calls take **39.794–65.157 ms**; median Tick cost over all frames is only **0.0005 ms**. Mean cost therefore understates the visible pauses.

GC.Alloc samples directly parented by Tick total **20,833,212 bytes (19.87 MiB)**, approximately 63% of the recorded GC-allocation counter total. Inspected `GameSaveCoordinator.Tick` calls `SaveDirtyDocuments` synchronously; the latter captures all subsystems, calls storage and then updates profile metadata. Current [save documentation](../Systems/SavesAndProfiles.md) correctly describes whole-profile snapshots and debounce timing. Detailed serialization/compression/storage attribution remains unmeasured inside Tick.

Recommended optimization: keep Unity-owned state capture on the main thread, measure its cost separately, and consider moving encoding and disk work off the main thread with ordered writes and preserved snapshot/recovery guarantees. Do not split inventory/tree generations or weaken atomic profile consistency just to reduce pauses.

### 3. Stable per-frame skill-tree visual overhead

| Marker | Mean/frame | Maximum sample |
| --- | --- | --- |
| NodeConnectionRenderer.Update | 1.275 ms | 2.386 ms |
| SkillTreeFogOfWarController.Update | 0.768 ms | 1.522 ms |
| NodePowerVisual.Update, all instances | 0.364 ms | 0.134 ms |

Inspected `NodeConnectionRenderer.Update` walks the entire connection-state array each frame. Settled entries still call `FinalizeConnectionVisualState`, which writes texture state/progress via setters. An active-animation list and one-time finalization are concrete candidates for avoiding this repeated work. Texture uploads are already conditional on animation changes; do not describe this as an unconditional per-frame texture upload.

`NodePowerVisual.Update` appears **1,498,088 times**, or **2,764 invocations/frame**. It already returns when power/color are settled; the remaining callback volume is a candidate for enabling updates only during transitions. The count is not a count of visible renderers. Fog Update calls reveal-animation processing every frame; texture upload and node-visibility refresh are conditional. Its measured cost is lower priority than rendering preparation and autosave.

Sources inspected: `Assets/Scripts/Visual/SkillTree/NodeConnectionRenderer.cs`, `SkillTreeFogOfWarController.cs`, `NodePowerVisual.cs`, and `Assets/Scripts/SaveSystem/GameSaveCoordinator.cs`.

### 4. Allocation pressure exists; GC pauses do not explain these spikes

GC Allocated In Frame totals **33,127,534 bytes (31.59 MiB)** over the capture: mean **59.69 KiB/frame**, median **12.04 KiB**, maximum **2.28 MiB**. Direct GC.Alloc parents include autosave (**19.87 MiB**), BattleTickSystem.Update (**9.28 MiB**) and AttackCooldownVisual.Update (**0.84 MiB**). Parent attribution identifies the enclosing sample, not a precise allocating function or object type.

BattleTickSystem.Update itself averages **0.183 ms/frame**, maximum **2.355 ms**: it is an allocation follow-up, not the principal measured CPU bottleneck. GC.Collect has no measured positive duration; GarbageCollector.CollectIncremental reaches only **0.0149 ms**. This recording does not support attributing the large hitches to collection.

GC used memory grows from **1.384 to 1.422 GiB**; total used memory grows from **2.739 to 3.525 GiB**. Editor state and recording overhead are included. This short recording cannot establish a leak or identify retained objects; a player Memory Profiler comparison is needed for that claim.

### 5. The two worst frames are primarily Editor overhead

Frame **522** lasts **365.234 ms**, including **305.345 ms** in EditorLoop and **59.394 ms** in PlayerLoop. Frame **110** lasts **306.606 ms**, including **237.358 ms** in EditorLoop and **68.624 ms** in PlayerLoop. The render thread mostly waits for main-thread commands (15.263 seconds across the recording). Do not treat the worst Editor frames as standalone game-logic costs.

Rendering counters: SetPass mean **127**, maximum **182**; triangles mean **472,384**, maximum **745,519**; vertices mean **753,201**, maximum **1,045,262**. Draw-call/batch counter names were unavailable in this export, so these metrics are not reported as zero.

## Next focused checks

1. Reproduce the expensive skill-tree view in a development player capture and isolate renderer/mask contributions to CPU render preparation.
2. Add separate profiling boundaries for snapshot capture, encoding, storage and metadata before choosing an autosave implementation change.
3. Remove repeated settled-connection writes and idle visual callbacks if optimizing this subsystem is authorized, then compare the same scenario.
4. Trace combat allocation call stacks only after the dominant frame-time costs are addressed.

These are proposed follow-ups, not tests already run or fixes already applied.
