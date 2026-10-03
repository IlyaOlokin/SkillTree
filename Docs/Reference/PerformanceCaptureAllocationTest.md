# Performance analysis: allocationTest and allocationTest2

[Home](../Home.md) · [Backlog](../Backlog.md) · [Skill tree](../Systems/SkillTree.md)

Analyzed 2026-10-03. Primary evidence: [allocationTest2.data](../../ProfilerCaptures/allocationTest2.data).
The earlier [allocationTest.data](../../ProfilerCaptures/allocationTest.data) was also
inspected but begins during ongoing allocation activity. The second recording covers
all four visible action segments and supersedes it for the comparisons below.
Scope: existing Editor CPU recordings and inspected source; optimization proposals only.
No gameplay code, assets, scenes, tests, builds or new recordings were changed/run.

## Method and coverage limits

Loaded each saved recording through `ProfilerDriver.LoadProfile` in the existing,
non-playing Editor with recording disabled. Exported main-thread samples using
`RawFrameDataView`: 2,000 frames from the first file and 921 frames (0–920) from the
second. Aggregated inclusive marker durations, counts and GC.Alloc byte metadata.
Parent/child times overlap; do not add them. Temporary scripts/exports live in Temp.

The second recording shows isolated allocation-like activity at 102, refund-like
activity at 289, a long allocation sequence at 464–549, and a long refund sequence
at 683–768. Labels are inferred from input, tree operations and allocation-effect
samples, rather than a recorded mouse-button/node-ID log. The two long sequences
contain 86 operation frames each; this is not independent proof of an exact node count.
No source revision in the capture establishes an exact match to today's source.

Managed graph planning, stat recalculation, influence and fog work are not separately
instrumented. Selected-frame inspection found no usable GC allocation call stacks.
Source candidates inside those scopes must not be presented as measured subroutine
costs. GPU performance and active-combat performance are not established here.

## Measured results from allocationTest2

All times below are milliseconds. Overall main-thread time averages 13.619 ms,
with p95 31.609 and maximum 51.559. A baseline segment (50–99) averages 10.545 ms,
including 3.726 PlayerLoop and 6.547 EditorLoop.

| Action / frames | Main thread | PlayerLoop | MainSkillTree.Update | MainSkillTree.LateUpdate | Input callback |
| --- | ---: | ---: | ---: | ---: | ---: |
| Single allocation, 102 | 32.947 | 21.475 | 10.280 | 1.119 | 5.176 |
| Single refund, 289 | 17.723 | 11.420 | 1.041 | 1.111 | 4.874 |
| Long allocation, 464–549, mean | 32.181 | 12.618 | 1.474 | 4.297 | 0.372 |
| Long refund, 683–768, mean | 13.126 | 6.477 | 1.201 | 1.139 | 0.083 |

The long allocation starts with a 51.559 ms frame: input alone costs 31.963 ms and
MainSkillTree.LateUpdate costs 7.725 ms. Excluding that click, frame time still averages
31.953 ms, PlayerLoop 12.236 ms and EditorLoop 19.374 ms. LateUpdate falls from about
7 ms near the start to 1.428 ms at the end as the allocation queue shrinks.

Long-refund input costs 7.103 ms at frame 683. The repeated Update + LateUpdate cost
averages 2.340 ms per operation frame. Idle PlayerLoop immediately before it averages
3.737 ms, versus 6.477 during the sequence. Both single and long operations therefore
have real game-side CPU cost, even though Editor overhead amplifies the visible drop.

### First-use hitch is distinct from steady allocation cost

Frame 102 contains these measured descendants / frame aggregates:

- SoundManager.LoadFMODSound: 4.976 ms inside MainSkillTree.Update.
- TransformHandle.SetHierarchyCapacity: 0.881 ms under SetParent inside that Update.
- Mono.JIT: 4.302 ms aggregated across the frame (overlaps enclosing input/update scopes).
- Frame 289 also has 1.854 ms of Mono.JIT; the long-path click at 464 has only 0.489 ms.

The allocation effect factory calls the allocation sound and reparents pooled effects
to the node. The cue library maps `ui.nodeAllocation` to NodeAllocation.wav, whose
import metadata has `preloadAudioData: 0` and `loadInBackground: 0`. Source and samples
support eager audio preparation and avoiding per-click hierarchy expansion as concrete
first-use targets. First-use JIT must not be mistaken for steady-state algorithm cost
or automatically extrapolated to a player using a different scripting backend.

### Visual scheduling improved, but queue presentation still scales poorly

- NodeVisual.LateUpdate no longer has the prior 2,812 callbacks every operation frame.
  In long allocation it averages 23.97 calls/frame, maximum 105; aggregate time averages
  0.218 ms and peaks at 3.835 ms. During refunds it averages 0.015 ms/frame.
- TMP.GenerateText averages 0.953 ms/frame during frames 465–549. Frame 465 reaches
  3.774 ms of this inclusive marker. UI canvas update work also rises substantially.
  These markers overlap with their enclosing UI/visual scopes.
- NodeConnectionRenderer.Update averages 0.024 ms during long allocation and 0.015 ms
  during long refund; it is not a leading sustained cost here.
- Allocation flash coroutine MoveNext averages 0.045 ms during long allocation.
  This excludes rendering, activation and audio; it does not prove GPU effects are free.
- SkillTreeFogOfWarController.Update averages about 0.82 ms during both operation and
  idle segments. It is a useful baseline target, not the main extra 20 ms during queues.
- BattleTickSystem.Update is almost empty (about 0.001 ms/frame). This is consistent
  with paused combat but does not prove the pause state. Active combat was not benchmarked.

After allocation finishes (550–580), PlayerLoop falls to 4.631 ms while EditorLoop
still averages 16.075 ms. Thus slow Editor FPS during the animation tail is not evidence
that the allocation service remains expensive. Editor internal attribution needs a
separate capture; no particular window/inspector is identified as the cause here.

### Allocation pressure and remaining save hitches

Main-thread GC.Alloc metadata totals 29.96 MiB. Direct-parent attribution:
MainSkillTree.Update 19.57 MiB; NodeInputHandler.OnMouseOver 4.60 MiB;
GameSaveCoordinator.Tick 4.11 MiB. Long allocation allocates 14.41 MiB, long refund
10.19 MiB. Incremental GC reaches only 0.001 ms in this recording. These are allocation
pressure figures, not evidence that GC collection causes the observed hitches.

Saving has paired spikes at 175/179 (7.014/17.067 ms), 362/365 (4.774/11.704 ms),
585/587 (4.637/12.204 ms) and 838/841 (4.660/12.609 ms). Source captures and serializes
the snapshot on the main thread, writes it on a worker, then synchronously loads/writes
the manifest via TouchProfile. Pairing is consistent with those stages but stage timing
is not separately instrumented. Moving only the snapshot write has not eliminated
main-thread save hitches.

## Source findings and proposed changes, in priority order

### 1. Remove remaining global visual/event dispatch

MainSkillTree.LateUpdate rebuilds reachability and broadcasts availability to all
NodeVisual subscribers for dirty tree/skill-point changes. Avoiding each visual's
LateUpdate did not eliminate the predicate checks performed inside this broadcaster.
Its measured cost remains about 1.14 ms during the refund sequence.

Proposal: cache each node's presentation state and update only changed frontier
nodes, affected zones and cost-threshold groups. Handle arbitrary predicates with
explicit dependency invalidation or a conservative fallback, rather than assuming
all conditions are local.

Queue-order notifications have another scaling trap: an event is raised once for
each changed node, but every NodeVisual subscribes and compares the argument with
its own node. Renumbering Q queued nodes with N visuals costs O(N*Q) subscriber calls,
plus actual text regeneration. Use direct node-to-view dispatch / per-node events.
Consider showing a highlighted path, next node and total remaining count instead of
renumbering every queued node every step. This queue-specific cost is source-confirmed;
the decline from about 7 ms to 1.4 ms in MainSkillTree.LateUpdate as the queue drains
is consistent with it, although the broadcaster has no separate timing marker.
Text rebuilding adds independently observed UI cost.

### 2. Replace per-candidate refund connectivity searches

FindAllocatedNodesDependentOn enumerates the tree and runs a new root-path search
for each eligible allocated node, repeating passes until stable. Refunding one leaf
therefore still examines unrelated allocated nodes. Each search creates collections.
This is a strong source candidate for input spikes of about 5–7 ms on refunds, but tooltip
refresh and synchronous bonus recalculation also share that measured input marker.

Proposal: solve root reachability once on the graph with the requested removal
excluded, then identify affected active nodes by set difference. Cache node indices,
forward/reverse adjacency and reusable traversal buffers. Preserve directed-edge
semantics, bridge links, inactive-but-allocated exemptions and independently allocated
exceptions; independent nodes must not silently become new connectivity roots.
Preserve reverse-distance refund order, zone consequences, infinite-node investment
and queue cancellation rules. Local component recomputation can follow the shared
linear traversal once equivalence is established.

Allocation planning should likewise validate a candidate path using one evolving
simulated state, instead of rerunning root searches for each appended candidate.
Current CanQueueNodeForAllocation and subsequent pruning repeat this work. The
31.963 ms long-path input callback makes planning a first-priority target; its exact
graph/stat/tooltip split remains unmeasured.

### 3. Separate queue changes from bonus changes; coalesce redundant recalculation

PlayerUnit subscribes RequestModRecalculation to OnSkillTreeChanged. That event is
raised for enqueue/cancel operations even when active modifiers have not changed.
A normal queued operation also raises it in MainSkillTree.ProcessNodeAllocation
and again after the operation in SkillTreeAllocationService. Gem-power or zone
cascades can raise further events.

When combat is running and registered, requests normally coalesce through the Mods
phase. When paused/unregistered/no tick system exists, RequestModRecalculation invokes
recalculation synchronously. RecalculateMods unbinds runtime modifiers, resets
stats/attributes, gathers all modifiers, runs the full phase pipeline, rebinds and
notifies all stat observers. BaseUnitModifiers.Reset recreates every StatModifier
and its More list. This is a concrete repeated-work path; the profile does not time
its individual executions.

Proposal: distinct queue/persistence, active-modifier, connectivity and presentation
changes. Use an operation-level change set so a node and its synchronous zone/power
consequences publish one consolidated bonus change. Preserve gameplay-relevant
per-node events. Queue-only changes still dirty saving, but do not recalculate stats.
Paused UI can flush once at the end of the operation; active combat must flush before
its next Mods/Resources/Effects/Actions consumption boundary. Do not simply add an
arbitrary timer to stat updates. Reuse stat buckets and scratch lists while retaining
full existing phase evaluation initially. Incremental stat math is a later, higher-risk
change because attributes, conditional/special modifiers and runtime bindings interact.

### 4. Invalidate gem influence only when its inputs change

RequiresFullRecalculation returns true for every SocketNode change, including an
empty socket. Recalculate enumerates/copies the tree, scans sockets and assigns power
across all eligible nodes. This can create intermittent more expensive operations.
The eight roughly 3.2–3.4 ms refund steps are compatible with this path but cannot be identified
as sockets from the available markers.

Proposal: retain per-source influence maps and track effective active gem/rule/power
changes. Empty sockets need no influence rebuild. Recompute only affected sources,
then apply changed totals to affected nodes; keep conservative custom-rule handling
and separate authored influence graph from runtime bridge allocation graph.

### 5. Make fog work proportional to changed cells

UpdateRevealAnimation scans the entire mask every frame, including when settled.
Uploads rewrite all pixels; RefreshNodeVisibility revisits every cached node.
HandleNodeChanged can also upload and refresh before the normal animated Update.

Proposal: active-cell list, one coalesced upload per frame, cached node-to-cell mapping
and visibility notifications only on threshold crossings. A more ambitious option is
shader-driven fading from cell start times with CPU-maintained reveal/interaction
state. Preserve the current visibility threshold, reveal delays and saved discovery;
visual deferral must not accidentally expose clickable undiscovered nodes.

### 6. Finish moving save work off the main thread

Move manifest persistence into the ordered save worker; avoid synchronous disk work
in completion callbacks. Consider worker serialization from an immutable whole-profile
DTO after main-thread capture, or a cached save model updated by committed changes.
Do not assemble a profile over multiple frames from freely changing live objects:
it can mix tree, inventory, wallet and gems from different logical moments. Preserve
snapshot/manifest order, dirty generations, error retries, reset/profile-switch fences
and the separate WebGL path. Debouncing saves alone only delays the remaining hitch.

### 7. Prepare audio and effects before interaction

Preload the allocation clip before the tree becomes interactive, rather than paying
the first load on click. Keep pooled effects under a stable presentation root and
position them from the node, or reserve hierarchy capacity during setup; preserve
scale, sorting and camera alignment. Prewarm the bounded flash pool and queue-label
UI where needed. The measured 4.976 ms audio load and 0.881 ms hierarchy expansion
make this more specific than simply deleting animations. Group/throttle sounds during
long paths and consider one travelling path pulse instead of overlapping flashes;
these are presentation choices, not reasons to delay the logical bonus update.

## Queue and animation redesign options

**Recommended first: retain progressive gameplay, separate preparation and presentation.**
Plan/validate with reusable graph state, commit deterministic node operations with
consolidated changes, and let a bounded visual scheduler animate the committed result.
A time budget applies to interruptible preparation/presentation work, not to an atomic
operation that is already expensive. Example goals, not measured guarantees: ordinary
node commit below 0.3 ms, total tree presentation below 0.5 ms/frame, no synchronous disk
I/O in the interaction path. Preserve logical operation order, point spending and
cancellation semantics. Define a combat-boundary flush before any batching changes.

**More ambitious: budgeted chunks.** Process several valid operations in a transaction
when affordable, publishing derived state once per chunk. The player sees continuous
path animation while the CPU avoids rebuilding global state for each presentation
step. This changes the timing/granularity of bonuses and cancellation; it requires an
explicit rule for which prefix is committed and which suffix can still be cancelled.
Combat may observe only complete chunks with coherent stats, never partially updated
node/power/stat state. Do not skip node-triggered rewards or reactions while batching.

**Most disruptive: plan, then atomically apply the whole path.** If enough points and
all conditions permit, commit the path as one build change and play the animation
independently. A ghost preview can be computed incrementally before the short commit;
revalidate against a state version when points, zones, gems or topology change.
Insufficient points need an explicit retained-queue/partial-prefix policy. Whole-path
bonuses arrive earlier than with the current queue and already committed operations
cannot be cancelled as pending. This is a gameplay timing change, not a transparent
optimization. It should be chosen deliberately rather than hidden in a performance fix.

Do not use slower 0.05–0.1-second queue cadence as the primary fix: it spaces expensive
operations but retains single-node hitches. Do not disable combat to mask them, nor
move live Unity objects and event callbacks onto background threads.

## Minimal follow-up evidence for implementation

Add narrowly scoped markers for path/refund planning, queue execution, full stat
recalculation, availability dispatch, gem influence, fog upload/visibility and both
save phases. Record complete isolated single/path allocation and refund actions in
already revealed and newly revealed areas, with active and paused combat distinguished.
Compare CPU frame-time percentiles and bytes per operation; do not rely only on an
FPS average. A development-player capture is needed before assigning Editor-only
variance to shipping gameplay. These are proposed checks, not checks run now.

## Inspected source

- `Assets/Scripts/SkillTree/MainSkillTree.cs`
- `Assets/Scripts/SkillTree/SkillTreeAllocationService.cs`
- `Assets/Scripts/SkillTree/SkillTreeAllocationGraphService.cs`
- `Assets/Scripts/SkillTree/Node.cs`, `NodeInputHandler.cs`, `LimitedZone.cs`
- `Assets/Scripts/SkillTree/GemPowerInfluenceService.cs`
- `Assets/Scripts/Visual/SkillTree/NodeVisual.cs`
- `Assets/Scripts/Visual/SkillTree/SkillTreeFogOfWarController.cs`
- `Assets/Scripts/Visual/SkillTree/NodeAllocationEffectFactory.cs`
- `Assets/Scripts/Battle/Unit.cs`, `PlayerUnit.cs`, `BaseUnitModifiers.cs`
- `Assets/Scripts/StatCalculator.cs`
- `Assets/Scripts/SaveSystem/GameSaveCoordinator.cs`, `SaveFileStorage.cs`, `SaveProfileManager.cs`

- `Assets/Scripts/AudioSystem/GameAudio.cs`
- `Assets/Audio/AudioCueLibrary.asset`
- `Assets/Audio/UISounds/NodeAllocation.wav.meta`
