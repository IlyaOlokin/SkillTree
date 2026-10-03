# Bridge gem

[Home](../Home.md) · [Gems and sockets](../Systems/Gems.md) · [Skill tree](../Systems/SkillTree.md)

Definition: `Assets/Resources/Items/Gems/BridgeGem.asset` (`GemKind.Bridge`). Uses an existing gem icon temporarily. During placement a line follows the cursor from the first socket to the second gem. No distance limit or automatic loot/shop entry.

## Try it

1. Enter Play Mode and use **Tools > Skill Tree > Add Bridge Gem (Play Mode)**.
2. Select it in inventory and click a discovered socket, learned or unlearned. Existing contents return to inventory if there is room and an existing bridge can safely be removed.
3. The first socket is temporarily occupied; the cursor shows the second gem without text. The inventory item is reserved, marked `…`, and is not consumed yet.
4. Click a different socket to complete the pair. One inventory unit is consumed and the cursor clears, including when more units remain in the stack.
5. During step 3, RMB anywhere or Escape cancels. Selecting another item, disabling the tree or loading a save also cancels. The first socket becomes empty; displaced items stay in inventory.
6. With no selected item, LMB still learns filled sockets using normal skill points, root connectivity and zone/lock rules.
7. RMB on either installed end attempts to remove the whole pair and selects the one returned inventory item. Removal/replacement is refused if it would break an existing active root path or the allocated path of temporarily inactive dependent nodes. Independent allocations keep their existing exceptions. Already-disconnected regions do not block unrelated removals.

If inventory space is insufficient, the operation fails without changing sockets or items. In particular, the first replacement may require a free slot because the new item remains reserved until completion. Bridge operations wait while a queued refund is executing.

## Implementation

- BridgePlacement owns pending placement and reservation cleanup; GemPlacementService coordinates selection and input.
- BridgePlacementLine owns the temporary UI line; the cursor only supplies its endpoints and lifecycle.

- `Node.ConnectedNodes` remains the authored graph. `AllocationNeighbors` adds a reciprocal bridge partner for allocation, path queues and refunds only. Gem influence distances and ordinary line rendering still use the authored graph.
- A completed pair stores one gem identity and two endpoint references. Instance IDs keep the pair valid across Unity domain reloads. A pending end is transient and has no graph connection.
- Inventory exchange stages capacity/count changes, then updates all affected sockets before publishing change events.
- `SkillTreeSaveData.bridgeGems` stores one record per pair. Pending placement is absent from tree saves, while the reserved inventory unit remains present. Endpoint IDs participate in existing ID migration. Restore validates endpoints and occupancy before modifying the tree, restores pairs before zone/queue evaluation, and suppresses intermediate tree callbacks.
- New built-in definitions under `Resources/Items` are loaded by save catalogs, so the bridge is resolvable in player builds before it has been equipped.

## Validation

**Tools > Skill Tree > Validate Bridge Gem** runs 14 checks outside Play Mode in isolated preview scenes. Results are written to `Logs/BridgeGemValidation.txt`. Checks cover cancellation with displaced items, stacked inventory consumption, pair saving/loading and queued allocation, dependent removal, inactive dependencies, alternate paths, capacity failures, replacement, cascading refunds, rootless cycles, selection changes, invalid saves, multiple bridges, serialized copies, and cursor visibility/second-end state.

Historical validation recorded these checks as passing in a separate Unity 6000.4.6f1 batch project using the compiled gameplay assembly. They were not rerun in the documentation pass. Cursor appearance and physical mouse clicks in the main scene still require a Play Mode check.
