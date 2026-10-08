# Gems and Sockets

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Skill tree and progression](SkillTree.md)

Status: **source-reviewed, 2026-09-27**. Scope: gem kinds, placement, influence and
tree/save integration. No Unity playtest was performed for this page.

## Definitions, instances and sockets

`GemDefinition` is an item definition asset containing kind, modifier templates,
influence rules and an optional bridge line prefab. `GemInstance` references a
definition and has an instance ID. A `SocketNode` stores a gem and owns runtime
copies of local modifier templates, cleaning those copies up when replaced.

Inserting a gem and allocating its socket are separate actions. A stored gem's
local effects or influence become active only when its socket is active and it
is not a pending bridge endpoint. See [node states](SkillTree.md) and
[stat calculation](StatsAndModifiers.md) for how collected effects reach combat.

| Gem kind | Gameplay role | Implementation |
| --- | --- | --- |
| `LocalModifiers` | Supplies modifiers from an active socket | `SocketNode.GetActiveModifiers` |
| `NodeInfluence` | Adds runtime power to eligible nodes at graph distances | `GemPowerInfluenceService` and influence rules |
| `Bridge` | Connects two sockets for allocation, paths and refunds | `BridgePlacement`, `BridgeConnectivity`, `AllocationNeighbors` |

Adding a definition does not automatically add it to loot or a shop. Distribution
is a separate content decision and must be wired into the relevant system.

## Placement and inventory ownership

`InventorySelectionState` owns the selected slot/item. `GemPlacementService`
routes placement and cancellation; `InventorySocketService` coordinates socket
exchanges with `PlayerInventory`. Use these services rather than directly changing
socket fields from UI code. See [inventory and items](InventoryAndItems.md) for
stacking, reference-based selection and compaction rules.

Ordinary insertion creates an instance from the selected definition and uses the
inventory exchange path. Replacement returns displaced contents if the exchange
can succeed. Bridge replacement also checks whether releasing the old connection
would break dependent paths.

Socket/inventory changes are staged before publishing their change notifications.
Preserve that ordering so observers do not save or recalculate a half-completed
exchange. Check capacity failures as well as successful transfers.

## Influence distance and power

Runtime invalidation update, 2026-10-02: MainSkillTree fully recalculates influence
on socket changes, explicit topology notifications and save restoration. For the
sealed built-in distance rules, ordinary node changes restore only that node's
cached runtime power synchronously; allocation does not change authored distances
or these rules' target eligibility. If an active source uses an unknown influence
rule implementation, every node change retains the original full recalculation.
This preserves custom rules that may inspect allocation or other node state.
No runtime checks were performed for this optimization at the owner's request.

An active `NodeInfluence` socket triggers a breadth-first traversal over
`ConnectedNodes`. Distance is the shortest number of authored links from the
socket, not world-space distance, and bridges do not shorten it.

- `WithinDistanceGemPowerInfluenceRule` matches distances up to its maximum.
- `ExactDistanceGemPowerInfluenceRule` matches only the specified distance.
- The source socket is excluded. Targets must allow power changes.
- Traversal does not require each intermediate node to be allocated or active.
- Matching rules add their bonuses together into runtime power.
- Recalculation assigns zero to eligible nodes that no longer receive influence.

For example, a node two authored links away receives a distance-two bonus even
if a bridge creates a shorter allocation path. Two applicable `0.10` bonuses
produce `0.20` runtime power. These are behavior examples, not prescribed balance.

Sockets and infinite nodes cannot change power and are excluded as influence
targets. An inactive target may have calculated power, but its node effects still
depend on [activation](SkillTree.md).

## Bridge lifecycle

1. Select a bridge gem and choose the first socket. The inventory unit is reserved;
   the first endpoint is pending and creates no allocation connection yet.
2. Choose a different socket. A successful exchange consumes one unit and installs
   a reciprocal pair sharing one gem identity.
3. Right-click or Escape cancels a pending placement. Selection changes, tree
   disable and save loading also terminate pending placement.
4. Removing a completed bridge returns one item for the pair, subject to capacity
   and connectivity rules. Dependent allocated paths, including temporarily
   inactive dependencies, must remain valid under the removal rules.

Cancellation does not put displaced first-socket contents back into that socket;
they remain in inventory. The [bridge reference](../Reference/BridgeGem.md) contains the
detailed player procedure, replacement edge cases and validation scenarios.

Do not implement bridges by editing `ConnectedNodes`: that would also change
authored geometry and influence-distance semantics.

## Saving and loading

Ordinary socketed gems and completed bridge pairs have distinct save records.
A bridge stores one identity and two endpoint IDs. Pending placement is transient;
its reserved inventory unit remains present until completion.

`SkillTreeSaveService` checks endpoint identity and occupancy during restore.
Node-ID migration covers bridge endpoints as well as ordinary node records.
`GemDefinitionCatalog` resolves saved gem definitions. When introducing a new
definition, verify it is resolvable in a player build, not merely present in an
Editor session.

Inventory and tree records participate in the same profile snapshot. See
[saves and profiles](SavesAndProfiles.md) for recovery and definition resolution,
and [tree persistence](SkillTree.md) before changing gem serialization.

## Checks when changing this system

- Insert, replace and extract ordinary gems; verify inventory quantities and stats.
- Deactivate/reactivate a socket and inspect its local effects or influence.
- Check exact/within distance at the boundary and after overlapping influence removal.
- Confirm a bridge changes allocation reachability without changing influence distance.
- Cancel after displacing a gem; check reservation cleanup and inventory ownership.
- Test full inventory, stacked bridges, dependent paths and pending refunds.
- Save/reload a completed pair and reload during a pending placement.

The [bridge reference](../Reference/BridgeGem.md) documents the existing Editor validation
command. Its historical pass report is not a new test result from this pass.

## Source entry points

Repository-relative paths (outside the Obsidian vault):

- `Assets/Scripts/Items/Gems/GemDefinition.cs`
- `Assets/Scripts/Items/Gems/GemInstance.cs`
- `Assets/Scripts/Items/Gems/GemPowerInfluenceRule.cs`
- `Assets/Scripts/Items/Gems/WithinDistanceGemPowerInfluenceRule.cs`
- `Assets/Scripts/Items/Gems/ExactDistanceGemPowerInfluenceRule.cs`
- `Assets/Scripts/SkillTree/SocketNode.cs`
- `Assets/Scripts/SkillTree/GemPowerInfluenceService.cs`
- `Assets/Scripts/Inventory/GemPlacementService.cs`
- `Assets/Scripts/Inventory/InventorySocketService.cs`
- `Assets/Scripts/Inventory/BridgePlacement.cs`
- `Assets/Scripts/SkillTree/BridgeConnectivity.cs`
- `Assets/Scripts/SaveSystem/GemDefinitionCatalog.cs`

## Ordinary flat-stat gem content (2026-10-08)

29 additional LocalModifiers definitions live in
`Assets/Resources/Items/Gems/ModifierGems`, named `Added<Stat>Gem_<value>`.
Each has one dedicated `GemAdded<Stat>_<value>` BaseModifier asset in the same
folder, using Added (modifierType 0), a unique saveDefinitionId and maxStack 99.
Initial values were five times the corresponding Added small-node standard in the
[authoring guide](../SkillTree/SkillTreeFillingGuide.md). Percentage stats are
additive fractions, not Increased bonuses. Damage covers all six current terminal
DamageType values; retired PoisonDamage is excluded.

| Stat | Raw Added value |
| --- | --- |
| PhysicalDamage | 10 |
| FireDamage | 10 |
| ColdDamage | 10 |
| LightningDamage | 10 |
| LightDamage | 10 |
| DarknessDamage | 10 |
| Armor | 30 |
| Evasion | 30 |
| MaximumHealth | 100 |
| BarrierCapacity | 60 |
| BlockChance | 0.15 |
| BlockPower | 20 |
| ParryChance | 0.5 |
| ParryPower | 0.75 |
| MysticNegation | 0.25 |
| ElementalResistance | 0.2 |
| FireResistance | 0.25 |
| ColdResistance | 0.25 |
| LightningResistance | 0.25 |
| HealthRegenerationPerSecond | 5 |
| MysticCleansePerSecond | 0.05 |
| AilmentGuard | 0.25 |
| IgniteMitigation | 0.5 |
| ChillDurationReduction | 0.5 |
| OverchargeAvoidanceChance | 0.5 |
| BleedMitigation | 0.5 |
| SunderMitigation | 0.5 |
| DistractMitigation | 0.5 |
| ExposeMitigation | 0.5 |
These assets reuse the existing Steel Gem sprite as a placeholder. English item
names are literal fallback strings; stat descriptions use existing BaseModifier
localization. No new localization keys or TooltipDescriptionData were introduced.
Existing gems, loot, shops and scene configuration were preserved. Resources/Items
is loaded by SaveDefinitionCatalog, so these definitions are discoverable in builds.

BlockPower uses the owner's 2026-10-08 override: 4 per small node, 20 per gem.
BarrierCount has no valid ordinary small-node standard (0.66 is explicitly barred
from small nodes); masks, resistance caps and Increased-only regeneration are not
part of this flat-stat set. No new mitigation standards were invented.

Verification: serialized script references, one-modifier cardinality, stat IDs,
values and paired GUIDs were checked during content creation. No C# changed; no
compilation, tests or Unity playtest were run. This is budget matching, not a claim
of combat balance.

Modifier organization update (2026-10-08): moved all 29 numeric modifier assets
from the gem folder into Assets/Scripts/SkillTree/Modifiers and removed the Gem
prefix. Names reflect current serialized values. Only m_Name changed inside the
assets; all values and metadata GUIDs were preserved, keeping gem references valid.
Owner changes to Evasion (30) and BlockChance (0.15) are retained. Content is no
longer uniformly equal to five historical small-node standards. Verified file
contents and metadata preservation; no compilation or tests were needed.

Shop distribution update (2026-10-08): these 29 gems are now sold across Shop1–Shop4
at 100 / 150 / 200 / 250 gold, with three purchases per gem and one gem per
purchase. See [tiered gem shops](EconomyAndLoot.md#tiered-flat-stat-gem-shops-2026-10-08)
for the stat groups and unlock prerequisites. Older alternate-value gems in the
same folder are outside this 29-gem distribution and were not changed.
