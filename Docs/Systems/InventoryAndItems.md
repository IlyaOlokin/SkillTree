# Inventory and Items

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Gems](Gems.md) · [Saves and profiles](SavesAndProfiles.md)

Status: **source-reviewed, 2026-09-27**. Scope: item identity, slots, stacking,
selection, use and socket exchanges. Shop pricing, loot rolls and UI layout are
outside this pass. No Unity playtest was performed.

## Ownership

| Type | Responsibility |
| --- | --- |
| `ItemDefinition` | Shared asset: stable definition ID, name, description, icon, stack limit and use behavior |
| `InventoryItem` | Runtime entry: item type, definition, optional gem instance and quantity |
| `InventorySlot` | Holds an inventory entry |
| `PlayerInventory` | Slot operations, compaction, inventory events and capture/restore |
| `InventorySelectionState` | Tracks the selected gem or node-targeted item |
| `InventoryItemUseService` | Direct item use |
| `NodeItemUseService` | Applies a selected item to a node |
| `InventorySocketService` | Coordinates inventory/socket exchanges |

Generic items and gems share the inventory model. `FromItemDefinition` creates a
gem entry when given a `GemDefinition`; other definitions create generic entries.
Creation clamps the requested quantity to the definition's stack limit. It does
not create multiple stacks for an oversized amount.

`InventoryItem.CreateCopy` copies the entry and quantity. For a gem it retains the
same `GemInstance` reference; it is not a deep clone with a new identity. Ordinary
socket insertion explicitly creates an instance from the definition in the socket
service. Do not infer identity semantics from a method name alone.

## Slots and stacking

`PlayerInventory` has a serialized slot count, with a code default of 24. Scene
configuration and restored save data can differ. It captures default inventory
state during `Awake` for fresh-profile/reset restoration.

- Stack compatibility requires the same item type and the same definition asset,
  a stackable definition, nonempty entries and spare capacity in the destination.
- `TryAddItem` first looks for one compatible stack that can hold the **entire**
  incoming quantity. Otherwise it uses one empty slot. It does not distribute a
  quantity over multiple partially filled stacks.
- `TryMoveItem` moves or swaps entries; it is not a stack-merge operation.
- Removal, movement, replacement and loading compact nonempty entries toward the
  start. A slot index is a current position, not a stable item identity.
- Shrinking the configured slot count removes excess slots in `EnsureSlotCount`.
  Capacity changes therefore need explicit handling of existing contents.

Example: two stacks with two free spaces each do not accept an incoming stack of
four through `TryAddItem` when there is no empty slot. This is current behavior,
not a proposed redesign.

## Selection and node interaction

`InventorySelectionState` accepts gems and items with `CanBeUsedOnNode`; it is not
a universal selection model for every consumable. On inventory changes it searches
for the selected entry by reference, updates its slot index, or clears selection
when that entry no longer exists. Replacement with a copy can therefore require
explicit reselection by the owning service.

`NodeInputHandler` routes a selected node item before ordinary node allocation.
Gem selection on sockets routes to [gem placement](Gems.md). New item UI should
use the existing services rather than bypassing this interaction ordering.

## Using items

Direct use creates `ItemUseContext` and calls the item's use method. On success,
`InventoryItemUseService` consumes one unit when `ConsumeOnUse` is enabled.
Node-targeted use similarly calls `TryUseOnNode` before requesting consumption.

The effect is applied before consumption, and these paths do not provide a general
rollback of the effect. In particular, `NodeItemUseService` does not propagate a
failed `TryConsumeItem` result. Custom use implementations must account for
callbacks that mutate inventory or selection; do not describe all item use as an
atomic transaction merely because gem exchange stages its changes.

Existing node-targeted definitions include unlocking a node, increasing permanent
power and independent allocation. The power item checks `CanChangePower` and the
node's permanent-power cap; independent allocation delegates eligibility to the
node. See [skill-tree state and power](SkillTree.md) before extending these items.

## Gem exchange boundary

`PlayerInventory.TryExchangeGem` builds a candidate inventory first: consume one
source unit if required, fit the returned gem into a stack or free slot, then
compact the candidate list. If it cannot fit, it returns before committing this
exchange. On success it writes inventory slots, invokes the socket commit callback,
and then raises `OnInventoryChanged`.

The callback is expected to complete normally; this helper does not implement
rollback after an exception in that callback. Socket notifications follow in the
socket service. Keep observers from seeing a half-applied exchange.

A pending bridge reserves an inventory entry by reference. Ordinary remove, move,
set and consume operations reject reserved slots. Bridge completion/cancellation
uses its dedicated path. Read [gems and sockets](Gems.md) for connection checks,
displaced contents and pending-placement semantics.

## Persistence and definition identity

Capture stores slot count and nonempty entries with slot indices. Restore clears
the bridge reservation, resolves entries through item/gem catalogs, skips invalid
slots or unresolved/empty entries, then compacts and publishes an inventory change.

`ItemDefinition.SaveDefinitionId` uses its explicit ID, with `name:<asset name>` as
a fallback. Renaming a definition that depends on that fallback can break old
references. Preserve explicit IDs; distinguish definition identity from gem instance
identity. Catalog loading and failure behavior are described in
[saves and profiles](SavesAndProfiles.md).

## Checks when changing this system

- Add into a compatible stack, an empty slot and a full inventory.
- Test quantities that fit only across several partial stacks.
- Remove/move entries while another item is selected; verify selection follows or clears.
- Apply valid and invalid node-use items; check quantities and node state together.
- Exchange socket contents with full inventory and a reserved bridge entry.
- Save/reload quantity, capacity and equipped gems; verify the whole profile agrees.

These are verification scenarios, not completed tests from this pass. The save
regression harness uses gameplay stubs and does not replace tests of the real
inventory component and UI in Unity.

## Source entry points

Repository-relative paths, outside this vault:

- `Assets/Scripts/Inventory/PlayerInventory.cs`
- `Assets/Scripts/Inventory/InventoryItem.cs`
- `Assets/Scripts/Inventory/InventorySlot.cs`
- `Assets/Scripts/Inventory/InventorySelectionState.cs`
- `Assets/Scripts/Inventory/InventoryItemUseService.cs`
- `Assets/Scripts/Inventory/NodeItemUseService.cs`
- `Assets/Scripts/Inventory/InventorySocketService.cs`
- `Assets/Scripts/Items/ItemDefinition.cs`
- `Assets/Scripts/Items/IncreaseNodePowerItemDefinition.cs`
- `Assets/Scripts/Items/IndependentNodeAllocationItemDefinition.cs`
- `Assets/Scripts/SaveSystem/SaveDataModels.cs`
