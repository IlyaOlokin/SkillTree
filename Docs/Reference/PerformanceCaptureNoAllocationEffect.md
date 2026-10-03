# Performance capture: NoAllocationEffect

[Home](../Home.md) · [Backlog](../Backlog.md) · [Skill tree](../Systems/SkillTree.md)

Analyzed 2026-10-02 from
[NoAllocationEffect.data](../../ProfilerCaptures/NoAllocationEffect.data).
The owner recorded allocation-queue processing with the allocation flash disabled
and reported approximately 22 FPS. This is an Editor CPU recording, not a player
benchmark. No gameplay changes, tests or new recordings were made during analysis.

## Method and scope

The existing Unity Editor loaded the saved recording with ProfilerDriver.LoadProfile.
RawFrameDataView exported all 682 main-thread frames (indices 0–681), aggregating
inclusive marker durations and call counts within each frame. Inclusive marker
times overlap; do not add parents to their children. The temporary extraction
script and exports live under Temp and are not durable documentation dependencies.

Overall frame time: mean 16.963 ms, median 11.206 ms, 95th percentile 51.141 ms.
These overall values hide the expensive allocation segment. Selecting frames where
MainSkillTree.Update exceeds 5 ms yields the contiguous 69 frames 333–401. This is
a marker-based selection matching the reported symptom, not recorded input events.

## Expensive segment, frames 333–401

| Marker | Average per-frame sum | Maximum per-frame sum |
| --- | --- | --- |
| Main Thread | 51.927 ms | 68.934 ms |
| PlayerLoop | 30.011 ms | 54.380 ms |
| EditorLoop | 21.475 ms | 28.687 ms |
| MainSkillTree.Update | 10.429 ms | 14.372 ms |
| NodeVisual.LateUpdate, all instances | 10.400 ms | 14.857 ms |

NodeVisual.LateUpdate has exactly 2,812 calls per selected frame. The previous
coalescing change therefore reduced duplicates but still updates the entire tree
each allocation step, including queue-label requests. Source inspection identifies
global skill-point/allocation invalidation and global queue-order notifications.
Queue label caching avoids unchanged writes but does not avoid these callbacks.
Inactive visual refreshes can still run root-connectivity searches.

MainSkillTree.Update contains queue pruning, node allocation and synchronous node
listeners, including full gem-influence recalculation. The recording does not split
their internal costs; do not attribute all 10.429 ms to one of these functions.

Across the complete recording, NodeConnectionRenderer.Update averages 0.00825 ms
and has a maximum of 0.0439 ms; SaveCoordinator.Tick averages 0.0262 ms and reaches
12.9253 ms. SkillTreeCamera's non-Inl RenderSingleCameraInternal marker averages
0.9636 ms and reaches 1.4881 ms. These are scoped CPU costs; no GPU conclusion is
drawn. The earlier mask-related CPU preparation cost is not the dominant marker
in this recording. Editor overhead materially contributes to the observed FPS.

## Next implementation targets

1. Remove global queue-label callbacks for unqueued nodes; notify only visuals whose
   queue position changed, without scheduling a full visual refresh.
2. Refresh availability only for affected nodes, preserving custom predicates and
   limited-zone dependencies through explicit broader invalidation where needed.
3. Reduce repeated graph/influence work inside allocation after tracing dependencies;
   the current capture does not separate pruning from influence calculations.

The allocation flash was disabled in this recording and the frame-rate drop remained.
Limiting flash concurrency is not the first fix for this captured slowdown. BL-038
remains open. A comparable owner recording should establish improvement after the
next change; this analysis is not verification of a fix.
