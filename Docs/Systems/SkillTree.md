# Skill Tree and Progression

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Gems and sockets](Gems.md)

Status: **source-reviewed, 2026-09-27**. Scope: runtime allocation, node state,
power and persistence boundaries. No Unity playtest was performed for this page.
Content requirements remain in the [filling guide](../SkillTree/SkillTreeFillingGuide.md).

## Purpose and ownership

Measured visual-performance evidence is recorded in the
[2026-10-01 Editor capture analysis](../Reference/PerformanceCapture20261001.md).
It identifies expensive CPU render preparation for SkillTreeCamera and repeated
settled-connection processing; the underlying rendering cause is not yet isolated.

### Node icon circular clipping

As of 2026-10-02, the SmallNode, BigNode, SpecialNode, SocketNode and InfiniteNode
prefabs use `Assets/Shaders/SkillTreeNodeIconCircle.shader` for their Circle sprite.
Their SpriteMask components are disabled and icon Mask Interaction is None.
The shader retains URP 17.4 sprite lighting, tint and normal rendering, and clips
pixels outside a circle centered at the icon transform origin with a hard edge.
There is no feathering; sprite texture alpha still applies inside the circle.

Adjust **Clip Radius (Local Units)** on the corresponding shared material under
`Assets/Materials/NodeIcons`. Increasing it reveals more of the icon; changing a
material affects all nodes using that material. Initial radii were derived from
the old mask bounds and relative transform scales. The clip coordinates are local
positions, not atlas UVs, so replacing an icon (including a socketed gem icon)
does not resize the clipping circle. Nonuniform transform scaling stretches the
circle together with the icon. Dynamic batching is disabled for this shader to
preserve object-space vertex coordinates; performance must still be measured.

Unity shader diagnostics reported no errors and the SkillTreeCamera capture was
visually inspected. All 2,812 NodeVisual instances in the open MainScene inherited
the new materials, with no enabled child SpriteMasks. No new performance capture,
player build or runtime socket-exchange check was run. Main-menu nodes were not
changed. See BL-033 in the [backlog](../Backlog.md).

The tree turns skill-point investment into active modifiers for the player.
`MainSkillTree` coordinates nodes, allocation queues, gem influence, fog discovery
and saving. `PlayerUnit` subscribes to `OnActiveModifiersChanged` and requests modifier
recalculation; editing a visual alone does not change the player's stats.
`OnSkillTreeChanged` remains the broader persistence notification, including queue edits.

| Owner | Responsibility |
| --- | --- |
| `Node` | Cost, allocation/activity, locking, power, modifiers and stable identity |
| `MainSkillTree` | Public tree operations, change notifications and coordination |
| `SkillTreeAllocationService` | Allocation queue and dependent refunds |
| `SkillTreeAllocationGraphService` | Allocation paths and dependency analysis |
| `UnitLevel` | Skill-point accounting and progression events |
| `LimitedZone` | Additional allocation and activation conditions |
| `BonusZone` | A collected modifier based on eligible nodes in the zone |
| `SkillTreeSaveService` | Capture and restore of tree state |

## Visual transition scheduling

As of 2026-10-02, `NodeConnectionRenderer.Update` visits only an active list of
connection transitions. Completion finalizes the texture state once and removes
the entry; allocation/refund events can retarget an existing entry without adding
duplicates. Texture recreation and full state synchronization clear the active
list. Node-change handling still searches the connection list, but uploads textures
only when a connection changed. Animation timing still uses `Time.deltaTime`.

`NodePowerVisual` disables its own component when power and allocation color have
settled. State setters wake it for another transition, while presentation visibility
can still update the managed renderer directly. Both values snap to their targets
when within an absolute tolerance of 0.0001, allowing fades towards zero to finish.
The component's enabled flag is therefore owned by transition scheduling; it is
not a persistent switch for hiding the aura. Use `SetPresentationVisible` for that.

These source changes were not playtested, profiled or checked with test suites;
the owner requested to perform verification separately. BL-035 and the power-visual
portion of BL-036 await that verification; fog scheduling is unchanged.

## Node state is not a single flag

| State | Meaning |
| --- | --- |
| Discovered | Visible/available to the node input flow through fog-of-war checks |
| Unlocked | A node that starts locked has been unlocked |
| Allocated | The node has been acquired; infinite nodes also track invested points |
| Active | The allocated node currently contributes effects under activation conditions |
| Independently allocated | Allocation follows the special independent-allocation rules |
| Queued | The tree is retaining an allocation request/path for processing |

Do not treat allocated and active as synonyms. Limited-zone conditions can make
an allocated node inactive. Save data must retain its allocation even when it is
not currently contributing effects. Independent allocation is also not ordinary
root-connected allocation and must keep its existing exceptions.

## Player interaction

`NodeInputHandler` first rejects pointer input over UI and input on undiscovered
nodes. Selection affects what a click means:

1. Left-click with a selected node-use item routes to `NodeItemUseService`.
2. Left-click a socket with a selected gem routes to [gem placement](Gems.md).
3. Otherwise left-click requests allocation or an allocation path through the tree.
4. Right-click on a filled socket attempts gem extraction before ordinary refunds.
5. Other right-click handling cancels a queued allocation, clears a selected item,
   or requests a dependent refund, depending on current state.

Pending bridge placement has its own cancellation handling. Test node input with
both an empty selection and a selected item; these are different interaction paths.

## Paths, queues and refunds

`ConnectedNodes` is the authored graph. `AllocationNeighbors` additionally exposes
a valid reciprocal bridge partner. Allocation and dependent-refund logic must use
the appropriate graph; [gem influence](Gems.md) deliberately uses authored links.

`TryAllocateOrQueue` asks the graph service for an allocation path. The allocation
service retains queued work and prunes requests invalidated by changes. Operations
are processed incrementally by `MainSkillTree.Update`, rather than assuming every
path change finishes in the click callback.

Refunding a parent can schedule refunds for dependent nodes. Root nodes and
independently allocated nodes are excluded by the ordinary dependent-refund entry
point. Bridge exchanges wait while dependent refunds are pending.

### Allocation queue performance investigation (2026-10-02)

### Allocation optimization implementation (2026-10-03)

Queue presentation now subscribes by node. MainSkillTree dispatches only to listeners
of entries whose displayed state changed, rather than invoking every NodeVisual for
each entry. Display numbers stay stable as earlier nodes complete or are cancelled;
gaps are expected. Appending to a nonempty queue continues its display sequence. A new
empty-to-nonempty queue starts at 1; save restoration reconstructs labels from saved
queue order. These labels are not current one-based positions. GetQueuedAllocationOrder
still returns the true position; GetQueuedDisplayOrder serves presentation only.

Graph planning builds a reusable reverse-adjacency snapshot once per simulation pass,
including directed allocation links and bridge endpoints. Accepted queued nodes extend
reachability incrementally. Dependent refunds use one reachability pass with the source
excluded instead of a root search for each allocated candidate and repeated scans.
Only active, allocated, non-root, non-independent unreachable nodes become additional
refunds; inactive allocations retain their existing exemption. Independent nodes are
not new roots, but their active links may still participate in genuine root paths.
The existing reverse-distance refund ordering and per-operation validation remain.
Topology is rebuilt per planning pass, so no persistent graph cache requires a new
invalidation contract. Public operations still process at most one node per Tick with
the existing 0.01-second interval; there is no whole-path commit or combat delay.

MainSkillTree groups normal queue operations, node-change cascades and restoration
notifications until the current synchronous operation completes. OnActiveModifiersChanged
notifies PlayerUnit and the weapon provider once at that boundary. Queue-only edits
still notify saving through OnSkillTreeChanged but do not request modifier recalculation.
Individual node/zone events and synchronous gem-power application remain in place.
Direct node events outside a MainSkillTree operation retain their own notification
boundary; this is not a global suppression of all modifier requests from other systems.

Profiler scopes now expose SkillTree.PlanAllocationPath, SkillTree.EnqueuePath,
SkillTree.PlanDependentRefund, SkillTree.PruneQueue, SkillTree.ExecuteQueuedOperation,
SkillTree.RefreshAvailability, SkillTree.PublishQueuePresentation and
Battle.RecalculateUnitModifiers. Availability still has its conservative global refresh;
fog, gem-influence invalidation, save threading and audio/effect preparation are separate
remaining targets from the capture report.

Verification: the gameplay C# assembly compiled against the installed Unity references
with zero errors (27 warnings). The ordinary generated project initially failed because
its referenced-project output DLLs were absent; a temporary compilation project pointed
those references at Library/ScriptAssemblies. The active Play Mode was not restarted,
and no gameplay tests, player build or new performance recording were run. This confirms
compilation, not behavioral or performance verification; BL-038 remains open.

### Earlier investigation and implementation history

Follow-up evidence, 2026-10-03: [allocationTest2 analysis](../Reference/PerformanceCaptureAllocationTest.md)
shows that global NodeVisual LateUpdate activity was reduced, but long allocation
still spends 4.297 ms/frame on average in MainSkillTree.LateUpdate (7.725 ms maximum).
Queue-order notifications are filtered by each subscriber, not dispatched directly:
each changed queued node still invokes every subscribed NodeVisual. Availability
refresh also remains a global broadcast. Queue changes raise OnSkillTreeChanged even
without active-modifier changes; a normal node operation can raise it both inside
ProcessNodeAllocation and again after queue processing. Paused PlayerUnit recalculation
is synchronous, so these requests are not automatically coalesced in that state.
These are inspected source findings and proposed optimization targets, not new behavior.

Third implementation update: allocation queue pruning reuses the last successful
validation during ordinary active allocation progress. Each pass still scans live
queued-node state and allocation predicates; any invalid candidate triggers the
original full graph-based pruning. Enqueue/reinsert/restore, cancellation,
deactivation/refund and explicit topology notifications invalidate validation.
The queue head still runs CanBeAllocated/HasEnoughSkillPoints, and Allocate retains
its own checks and skill-point spending. Operation cadence and ordering are unchanged.

Gem influence now retains its calculated power map. Built-in distance rules do
not require a full-tree recalculation when an ordinary node changes; the changed
node still synchronously receives its cached runtime power before tree observers
run. Socket changes, topology changes and save restoration retain full recalculation.
Active custom influence rules conservatively retain full recalculation on every
node change. No tests, Editor checks or measurements were run for this update.

Second implementation update: MainSkillTree coalesces queue-order notifications
and sends them only for nodes whose published order changed. Allocation
availability uses one shared reverse-graph reachability pass per dirty frame,
including runtime bridges and independent allocation. NodeVisual compares
availability before enabling its LateUpdate; skill-point changes no longer
schedule every inactive visual. Limited-zone count changes refresh that zone's
visuals. Topology changes rebuild reverse adjacency. NodeVisual runs after
MainSkillTree in LateUpdate. Cached connectivity is used only for presentation.
No tests, Editor checks or measurements were run for this update, as requested.

The subsequent [NoAllocationEffect capture](../Reference/PerformanceCaptureNoAllocationEffect.md)
shows a 69-frame expensive segment with 2,812 NodeVisual.LateUpdate callbacks per
frame, costing 10.400 ms in aggregate, plus 10.429 ms in MainSkillTree.Update.
Coalescing alone did not remove full-tree refreshes during queue processing. The
allocation flash was disabled and the reported slowdown remained. EditorLoop also
contributes materially; these are Editor CPU measurements.

The owner reported lower FPS while a long allocation queue is processed. Source
inspection found that the service processes at most one node operation per Tick,
with a 0.01-second interval; it does not allocate the entire path in one frame.
Each ordinary allocation spends a skill point and raises the global node-allocation
event. Every NodeVisual subscribes to both notifications and refreshes its own
visual, including `CanBeAllocated` (which may traverse root connectivity) and a
linear queue-order lookup. Queue-change notifications also refresh queue labels
across all NodeVisual instances. These are repeated global work, not measured
attribution of the reported slowdown.

Other source candidates are full queue pruning before each allocation, complete
gem-power recalculation on node changes, fog-mask upload and all-node visibility
refresh when a node is first discovered, and a full connection-list scan on node
events. The active-transition optimization only removed the per-frame connection
scan; it did not remove the event-time scan. Proposed work is to coalesce redundant
visual refreshes, update relevant nodes/queue labels, index queue positions and
incident connections, and invalidate expensive graph/influence work only when its
inputs change. Custom allocation predicates and socket/zone dependencies require
care before replacing global invalidation with local invalidation. See BL-038 in
the [backlog](../Backlog.md). No runtime checks or code changes were made in this
investigation.


Implementation update, 2026-10-02: NodeVisual coalesces visual, power and queue
requests in LateUpdate and disables idle callbacks. Queue positions use a lazily
rebuilt dictionary; unchanged labels and colors are not rewritten. Active nodes
skip availability traversal and unrelated skill-point/allocation refreshes.
Global invalidation remains for inactive nodes to preserve custom predicates.
Connection event handling uses an index of incident connections built when state
textures are recreated. Queue pruning, gem influence and fog remain unchanged.
No tests, Unity checks or profiling were run, at the owner's request.

## Power and collected effects

For an ordinary node, `Power = PermanentPower + RuntimePower` and the modifier
power multiplier is `max(0, 1 + Power)`. For example, a scalar value of `0.10`
scaled by power `0.50` becomes `0.15`; this is an arithmetic example, not a content
balance recommendation. Type-mask stats are not scaled as ordinary numbers.

Permanent power belongs to saved node state. Runtime influence is recalculated
from gems. Nodes can forbid power changes; sockets and infinite nodes do so.
Never change power merely to evade the content budget in the filling guide.

The tree collects modifiers from active nodes, active local-modifier gems and
bonus zones. Modifier implementations and `StatCalculator` determine actual
stat behavior; a tooltip or asset name is not a substitute for checking them.
Follow [stats and modifiers](StatsAndModifiers.md) for calculation phases and
[combat and effects](CombatAndEffects.md) for the attack snapshot and event order.

## Infinite nodes

An infinite node invests one point per allocation and refunds one point at a time.
Removing its final point follows dependency rules. Its effects scale with invested
points instead of power. Only ordinary `BaseModifier` scalar `Added`/`Increased`
containers are supported; masks, `More` and special modifiers are rejected.

See the [infinite node reference](../SkillTree/InfiniteNode.md) for setup,
backward-compatible save behavior and the dedicated validation command.

## Persistence boundary

`SkillTreeSaveData` records allocated and independent node IDs, unlocks, the
allocation queue, discovered nodes, socketed gems, bridge pairs, permanent-power
changes and infinite-node investments. Runtime power is derived again.

`Node` has explicit IDs and legacy aliases. Preserve them during content edits.
Restore migrates referenced aliases; ambiguous legacy ownership is an error,
not permission to arbitrarily choose a node. Loading notifies that the tree is
unavailable, cancelling transient gem placement before restored state is applied.

The tree data is part of the profile snapshot managed by `GameSaveCoordinator`;
do not introduce separate inventory/tree writes for an exchange without reviewing
the complete save transaction. See [saves and profiles](SavesAndProfiles.md) for
snapshot recovery and [inventory and items](InventoryAndItems.md) for exchange ownership.

## Checks when changing this system

- Allocate through a path, exhaust points, earn another point and inspect the queue.
- Refund a parent and check dependent refunds, returned points and alternate paths.
- Exercise allocated-but-inactive nodes in a limited zone.
- Verify stat changes and tooltips after allocation, power changes and gem removal.
- Save/reload a tree with queued nodes, gems and infinite investment.
- If bridges change, run the cases in the [bridge reference](../Reference/BridgeGem.md).

These are recommended verification scenarios, not results from this documentation
pass. For content edits, also follow the filling guide's visual and balance checks.

## Source entry points

Repository-relative paths (outside the Obsidian vault):

- `Assets/Scripts/SkillTree/MainSkillTree.cs`
- `Assets/Scripts/SkillTree/Node.cs`
- `Assets/Scripts/SkillTree/NodeInputHandler.cs`
- `Assets/Scripts/SkillTree/SkillTreeAllocationService.cs`
- `Assets/Scripts/SkillTree/SkillTreeAllocationGraphService.cs`
- `Assets/Scripts/SkillTree/LimitedZone.cs`
- `Assets/Scripts/SkillTree/BonusZone.cs`
- `Assets/Scripts/SkillTree/Modifiers/Modifier.cs`
- `Assets/Scripts/SkillTree/SkillTreeSaveService.cs`
- `Assets/Scripts/SaveSystem/SaveDataModels.cs`
- `Assets/Scripts/Battle/PlayerUnit.cs`

## Weapon selection and root icon

`SkillTreeWeaponTypeProvider` selects from Hammer, Sword, FireStaff, ColdStaff and
LightningStaff using separate lists of active nodes. Conflicts log a warning and
use that priority order; no active weapon node selects Unarmed. The former
`staffNodes` list migrates to `fireStaffNodes`. Weapon enum values retain existing
serialized meanings: 0 Unarmed, 1 Sword, 2 FireStaff (formerly Staff), 3 Hammer;
ColdStaff and LightningStaff use 4 and 5.

`RootNodeWeaponIcon` exposes five weapon/icon entries. Editor validation preserves
sprites by weapon type while adding missing entries. Assign existing sprites to
the three staff entries and configure the corresponding node lists in the
Inspector. Unarmed or an unassigned sprite uses the root's original icon. All
three staff types share the existing staff attack sprite, sound and hit effect.
This change extends selection/presentation; it does not alter damage stats.
