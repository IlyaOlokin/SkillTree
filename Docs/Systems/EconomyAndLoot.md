# Economy and loot

[Home](../Home.md) · [Project map](../ProjectMap.md)

Status: **source-reviewed, 2026-09-28**. Covers wallet, shop purchases, gold drops
and location reward delivery. No Unity playtest or balance validation was run.

## Wallet and persistence

`PlayerWallet` owns authoritative gold separately from displayed gold. Adding or
spending updates the real balance and its event immediately; display interpolation
uses unscaled time. Purchases must use real gold, not the animated display value.
Nonpositive additions are ignored; spending a nonpositive or unaffordable amount
fails. Loading clamps gold to zero or higher and sets the display immediately.

Wallet gold and shop purchase counts participate in the profile save through the
[save coordinator](SavesAndProfiles.md). Shop counts are keyed by shop and entry ID.
`ShopDefinition` falls back to its asset name for a placeholder shop ID and repairs
missing/duplicate entry IDs when validating its entries. Preserve authored IDs;
regenerating them can reset the apparent stock history.

**Implementation issue:** wallet `Tick` currently grants 1,000 gold on `KeyCode.Y`
without an editor-only guard. This is a debug input in the inspected source, not
an approved economy rule. This documentation pass does not change it.

## Shop purchase and sale contracts

`ShopService.TryBuy` validates the entry, remaining stock, funds, item construction
and inventory capacity before spending. It then adds the item. If insertion fails,
it refunds the price and returns failure; on success it records one purchase and
notifies listeners. This explicit failure rollback is not an exception-safe atomic
transaction: wallet/inventory listeners can observe intermediate changes.

| Entry field | Meaning |
| --- | --- |
| Amount | Units delivered per purchase, at least one |
| Price | Gold per purchase, zero or higher; free purchases skip spending |
| Stock limit | Number of purchases, not total item units; zero means unlimited |
| Remaining stock | `-1` means unlimited |

`TrySellInventorySlot` removes the whole inventory entry and credits the positive
gold amount supplied by its caller. It does not derive a unit price from the item
or multiply a price by stack size. See [inventory](InventoryAndItems.md) for stack
and slot behavior before adding a sale interface.

## Enemy gold

`EnemySpawner` resolves gold on enemy death and emits `OnEnemyGoldResolved` for a
nonempty result. `GoldDropContext` carries source stage, rarity and enemy power,
with nonnegative chance and amount multipliers. `GoldDropConfig` calculates a roll
and amount; the resolver supplies fallback tuning when no config is assigned.

The fallback chance is clamped to 0..1:

`(0.7 + 0.2 * clamp01(power / 140)) * rarityChance * contextChanceMultiplier`

The fallback amount is rounded and at least one on a successful roll:

`(2 + power * 0.35) * (1 + max(0, stage - 1) * 0.035) * rarityAmount * contextAmountMultiplier * random(0.85, 1.15)`

Fallback rarity factors, in Normal/Magic/Rare/Elite/Boss order, are
`1 / 1.1 / 1.25 / 1.45 / 1` for chance and `1 / 1.35 / 1.75 / 2.4 / 5` for amount.
These are code fallbacks, not a claim about the currently assigned asset balance.

The shared `Assets/EnemyConfig/GoldDropConfig.asset` was tuned on 2026-10-08
to target approximately 30 gold from one clear of battle locations Level1–Level3:
`baseGold = 0`, `goldPerPower = 0.024`. The base drop chance remains 0.2;
existing power and rarity chance bonuses, amount multipliers and random range
are unchanged. Successful drops still award at least one gold. A numerical
expectation calculation using the inspected wave generation, rarity rules, boss
overrides and integer payout gives approximately 31 gold (3.5 / 10.2 / 17.5
per location), before spending. This is an estimate, not a Unity playtest.
The shared asset also lowers enemy gold amounts in later locations; code fallback
values remain unchanged.

`EnemyItemDropSpawner` listens to the gold event and animates automatic pickup.
It credits the wallet in the flight completion callback, or immediately when no
flyout root can be resolved. Gold resolution and wallet credit are different
moments; saving or leaving during a flight needs explicit verification.

## Items and location rewards

### Gold completion rewards (2026-10-08)

`LocationLevelRewardEntry` supports `rewardKind = Item` (0, the default for
existing assets) and `Gold` (1). For Gold, `amount` is the wallet sum and no
`itemDefinition` is required. Add an entry to the location's `levelRewards`, select
Gold, set the absolute `levelNumber` (usually the final boss stage), and set Amount.
No location reward amounts were assigned as part of this feature.

Both the location information window and the completion window show Gold using
the drop spawner's coin sprite (including its generated fallback). The quantity
label shows the full gold amount, even when it is one. If no drop spawner exists,
the icon view uses a gold-colored image fallback. The tooltip title uses GameUI
key `ui.locationReward.gold.title`, with English text/fallback `Gold`; Russian and
German translations remain TODOs. No tooltip description asset or term is needed.

Collecting Gold records its claim and credits `PlayerWallet` immediately, then
optionally plays a cosmetic flight to the wallet. The flight cannot credit it
again. Gold does not occupy inventory slots. A repeated claim is rejected by the
existing saved claim-ID set; claims from another selected location are rejected.
The ID is `<locationId>:gold_reward:<absoluteStage>:<amount>`; changing its amount
changes its identity. Existing item IDs and the save schema remain unchanged.
Wallet and claim state participate in the existing whole-profile snapshot.

Reward-flight counts are established before starting animations so synchronous
fallback delivery cannot finish collection prematurely for a mixed reward list.
The pre-existing item insertion failure gap below remains open (BL-001).

Verification: Unity CLI `eval` requested `CompilationPipeline.RequestScriptCompilation`;
`recompile_status` completed with `failed: false`, and current Console counts were
zero errors and one warning (warning content was not inspected). Scoped
`git diff --check` passed. No test suite, player build or visual playtest was run.

The generic `ItemDropResolver` rolls **each table entry independently**, so a table
can produce several items. Entry chance combines base chance, a power bonus and a
rarity multiplier, clamped to 0..1. A missing definition or nonpositive base chance
disables an entry even when it has a power bonus.

However, the inspected `Assets/Scripts` contains no caller of `ItemDropResolver`.
Do not infer that ordinary enemy deaths roll item tables: the inspected spawner
subscribes to gold, while completion rewards use explicit location entries.
`ItemDropPickup` only configures an icon and camera; its name does not establish
an inventory transfer path.

[Location completion](LocationsAndEnemies.md) builds a list of unclaimed rewards
for the completed stage. Collection either adds them directly or sends them through
`FlyItemFromRectToInventory`. Animated item transfer also adds to inventory on
flight completion, then calls the completion-window callback to record the claim.
The exit action waits until window rewards have finished claiming.

**Implementation issue:** both direct and animated reward paths ignore the return
value of `TryAddItem`, then mark the reward claimed. Failed insertion can therefore
consume the claim without delivering the item. This is a failure-handling gap,
not a design rule. No gameplay fix was made during this documentation pass.

## Verification when changing this area

- Buy with exact funds, no funds, zero price and insufficient inventory capacity.
- Buy multi-unit stacks with limited stock; reload and confirm purchase counts.
- Check sale of an entire stack against the caller's intended price.
- Compare resolved gold, delayed credit and displayed balance, including paused time.
- Collect a reward with failed insertion; save/leave during item and gold flights.
- Confirm repeated completion does not duplicate a delivered reward.

These are recommended checks, not checks run in Unity during this pass.

## Source entry points

- `Assets/Scripts/Currency/PlayerWallet.cs`
- `Assets/Scripts/ShopSystem/ShopDefinition.cs`
- `Assets/Scripts/ShopSystem/ShopService.cs`
- `Assets/Scripts/DropSystem/GoldDropContext.cs`
- `Assets/Scripts/DropSystem/GoldDropConfig.cs`
- `Assets/Scripts/DropSystem/GoldDropResolver.cs`
- `Assets/Scripts/DropSystem/ItemDropEntry.cs`
- `Assets/Scripts/DropSystem/ItemDropResolver.cs`
- `Assets/Scripts/DropSystem/EnemyItemDropSpawner.cs`
- `Assets/Scripts/DropSystem/ItemDropPickup.cs`
- `Assets/Scripts/UI/LocationCompleteWindowController.cs`

Related: [locations](LocationsAndEnemies.md), [inventory](InventoryAndItems.md),
[saves and profiles](SavesAndProfiles.md).

## Tiered flat-stat gem shops (2026-10-08)

The 29 [ordinary flat-stat gems](Gems.md#ordinary-flat-stat-gem-content-2026-10-08)
are distributed exactly once across four ShopDefinition assets under
`Assets/EnemyConfig/Locations/Shops`. Each entry delivers one gem and permits three
purchases per profile (`amount = 1`, `stockLimit = 3`). Gem values are unchanged.

| Shop | Gem stats | Entries | Price per gem | Unlock prerequisite |
| --- | --- | --- | --- | --- |
| Shop1 | PhysicalDamage, FireDamage, ColdDamage, LightningDamage, BarrierCapacity, Armor, Evasion | 7 | 100 | Level3 (preserved) |
| Shop2 | MaximumHealth, HealthRegenerationPerSecond, FireResistance, ColdResistance, LightningResistance, ElementalResistance, BlockChance, BlockPower, ParryChance, ParryPower | 10 | 150 | Level6 |
| Shop3 | MysticNegation, MysticCleansePerSecond, LightDamage, DarknessDamage | 4 | 200 | Level9 |
| Shop4 | AilmentGuard, IgniteMitigation, ChillDurationReduction, OverchargeAvoidanceChance, BleedMitigation, SunderMitigation, DistractMitigation, ExposeMitigation | 8 | 250 | Level11 (preserved) |

Shop1 and Shop4 retain their asset GUIDs and shop IDs 1 and 4. New Shop2 and Shop3
definitions and location definitions have IDs 2 / 3 and shop-2 / shop-3, and are
registered in LocationCatalog. New locations reuse Shop1's map icon and shop
configuration. Pre-existing entry IDs were retained where the gem was already
listed; newly listed gems received unique IDs. Purchase history is keyed by both
shop ID and entry ID, so moving a gem to another shop gives it fresh stock there
on an existing profile; no saved purchase records were migrated.

Verification: Unity Editor asset operations checked all 29 gems for unique coverage,
entry counts, prices, amounts and stock limits. Serialized assets and catalog
references were checked after saving. The intended location checker
`Tools/Locations/check_locations.py` is still absent; that check was not run.
No runtime code changed; no compilation, test suite, build or Unity playtest was run.

