# Defences and combat resources

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Combat pipeline](CombatAndEffects.md) · [Ailments](AilmentsAndDebuffs.md)

Status: **source-reviewed, 2026-09-28**. Covers hit avoidance, mitigation, block,
barrier charges, health and mystic absorption. Values below are inspected code
behavior, not approved content balance. No Unity playtest was run in this pass.

## Where each defence acts

The [attack pipeline](CombatAndEffects.md#attack-pipeline) owns the ordering:

| Stage | Behaviour |
| --- | --- |
| Evasion | Stops the attack before hit modifiers, damage calculation and ailments |
| Armor | Reduces the physical component |
| Resistance | Reduces fire/cold/lightning through general and specific resistance |
| Mystic negation | Subtracts a flat amount from net mystic damage |
| Per-type mitigation | Multiplies each present damage component by its remaining fraction |
| Ailment application | Reads damage at this point, before block and barrier |
| Block / parry | Block reduces all components; parry can advance a counterattack |
| Barrier | Spends charges against damage types in its mask |
| Mystic health / health | Converts light/darkness to absorption; other types subtract HP |

DoT calls `ReceiveDoT` directly and skips these attack defence stages. Bleed and
Ignite calculate their own mitigation when created; they do not reroll evasion or
block, consume barrier charges or reapply armor/resistance on each tick.

## Evasion and armor

Evasion clamps accuracy and evasion ratings to nonnegative values. With positive
evasion, hit chance is `clamp(0.8 * (accuracy + 5) / evasion, 0.01, 1)`; zero
evasion means a guaranteed hit. Dodge chance is its complement. The stability
term `+5` means equal ratings do not yield exactly 80% hit chance.

For incoming physical damage `D` and armor `A`, the attack path leaves
`D * D / (A + D)` physical damage; it returns unchanged when either value is zero.
Thus the reduction for nonnegative values is `A / (A + D)`, which depends on hit
size. **Boundary mismatch:** the display/helper calculation clamps negative inputs,
but `ApplyArmorMitigation` does not. Do not assume the helper proves the attack
path safe for negative armor or an invalid denominator; this pass makes no code fix.

## Resistance, penetration and mitigation

Each resistance first uses `min(resistance, maximumResistance)`. For a positive
capped value, nonnegative matching penetration subtracts down to zero. Negative
resistance is retained; penetration does not push a positive value below zero.
A single attack-payload bypass roll removes positive general and specific
elemental resistance, while preserving negative resistance.

Fire damage uses `(1 - effectiveElementalResistance) * (1 - effectiveFireResistance)`;
cold and lightning use their matching specific resistance. These layers multiply,
not add: 20% general and 35% fire resistance leave `0.8 * 0.65 = 52%` fire damage
before subsequent mitigation. This is an arithmetic example, not a balance target.

Per-type damage mitigation then applies `1 - clamp01(mitigation)` to each component.
It is separate from resistance, penetration and ailment-specific protection.

Mystic negation uses
`(max(0, MaximumHealth) + max(0, BarrierCapacity)) * max(0, MysticNegation) + max(0, additionalNegation)`.
When this is positive and light-minus-darkness is nonzero, the code cancels the two
components and subtracts the amount from their absolute difference, leaving only
the surviving type. The capacity term is one capacity stat, not capacity times
current barrier charges. Zero negation or equal light/darkness returns unchanged
at this stage; later type mitigation and barrier masking still operate separately.

## Block and parry

Block chance is clamped to 0..0.9. BlockPower is a nonnegative flat damage amount,
displayed as a number rather than a percentage (changed 2026-10-07). For total
damage `D` immediately before block, a successful roll multiplies every component
by `max(0, D - BlockPower) / D`; zero total damage remains zero without division.
Power at or above `D` fully blocks damage. For example, 30 physical + 20 fire with
10 BlockPower becomes 24 physical + 16 fire. Existing authored BlockPower values
are now interpreted as flat amounts; no content rebalance or save migration was
performed. A zero-power block still counts as a
successful block and can emit events or enable parry. Block cannot cancel ailments
already attempted earlier in the attack.

Parry is a separate roll conditional on a successful block, using clamped
`ParryChance`. It adds `0.3 * max(0, 1 + ParryPower)` attack progress to the defender
after the outer attack finishes, avoiding a nested counterattack overwriting a
snapshot still in use. Parry itself adds no further damage reduction.

## Barrier charges

`BarrierCount` is truncated to an integer and bounded at zero; recalculation clamps
current charges to the new maximum. Per-charge `BarrierCapacity` is at least one.
`BarrierDamageTypeMask` selects the components protected by the barrier.

The barrier sums eligible damage and spends whole charges until capacity covers
the requested barrier damage, charges run out, or the attack's loss limit is
reached. The loss limit is at least one. Absorption is distributed proportionally
among the protected components; unprotected types pass through unchanged.

The barrier-damage multiplier affects **how many charges are consumed**. Absorbed
HP-bound damage is still capped by `consumedCharges * capacity` and the original
eligible damage. For example, double barrier damage can spend extra charges;
it does not directly double the damage subsequently applied to HP.

Regeneration accumulates `deltaTime / 4 * BarrierRegenerationSpeed` while not full.
It restores at most one charge per tick and retains excess progress. Damage does
not reset that progress. `RestoreFull` resets progress and emits a count change;
ordinary `Restore` emits one restoration event per gained charge.
`RestoreAdditional` prevents nested bonus-restoration chains across bindings.

## Health and mystic absorption

Health subtracts physical/fire/cold/lightning damage and skips light/darkness.
Normal receipt measures `max(0, HPBefore - HPAfter)` for `HealthDamageTaken`; HP can
fall below zero, so this value can include overkill. It is not capped to remaining
living HP. Death notification is latched until `RestoreToFull` resets it.

Healing, including regeneration, multiplies its amount by `max(0, 1 + HealingReceived)`
and caps the result at maximum HP. Recalculating maximum health preserves the old
HP fraction. `ProfanedHealthPercent` and `HallowedHealthPercent` define threshold
regions of the same HP resource, not independent pools; by default profaned is
the high region and hallowed the low region. A runtime swap reverses their sides.
The high region requires HP strictly above its lower boundary, and the low region
requires HP strictly below its percentage threshold.

Mystic health keeps one signed absorption amount: positive for light, negative for
darkness. Incoming light adds and darkness subtracts, so opposite types cancel.
The amount is capped to plus/minus maximum HP. Death occurs when current HP is
less than or equal to its absolute value, even if HP remains above zero.
Cleanse moves it toward zero by `MysticCleansePerSecond * MaximumHealth * deltaTime`.

Each full 5% of maximum HP in absorption contributes a debuff stack. Both types
apply `Added AilmentGuard = -0.05 * stacks`. Light adds a `More Accuracy` modifier
of `-0.02 * stacks`; darkness instead applies that modifier to Damage. Stacks are
replaced to reflect the current absorption state, not accumulated on each update.
See [stat groups](StatsAndModifiers.md#aggregate-stats) for AilmentGuard expansion.

## Verification and source entry points

Recommended checks: zero/equal ratings; small and large physical hits; negative
resistance and penetration; mixed damage and masks; zero-power block/parry;
barrier loss limits and regeneration overshoot; HP overkill; absorption cancellation,
type flips, cleanse and death threshold; stat recalculation and combat reset.

`Tests/CombatRegression/Program.cs` contains barrier-count clamping and mystic
absorption checks, but does not validate the complete Unity damage pipeline.
The existing runner is `dotnet run --project Tests/CombatRegression`; it was not
run for this documentation-only pass. Link and source-path checks are separate.

- `Assets/Scripts/Battle/Evasion.cs`
- `Assets/Scripts/Battle/Armor.cs`
- `Assets/Scripts/Battle/Resistance.cs`
- `Assets/Scripts/Battle/MysticNegation.cs`
- `Assets/Scripts/Battle/Block.cs`
- `Assets/Scripts/Battle/Parry.cs`
- `Assets/Scripts/Battle/Barrier.cs`
- `Assets/Scripts/Battle/Health.cs`
- `Assets/Scripts/Battle/MysticHealth.cs`
- `Assets/Scripts/Battle/Unit.cs`
- `Assets/Scripts/Battle/AttackProcessor.cs`
- `Assets/Scripts/Battle/Effects/AbsorptionEffect.cs`
- `Assets/Scripts/Battle/Effects/LightAbsorption.cs`
- `Assets/Scripts/Battle/Effects/DarknessAbsorption.cs`

Barrier.TryConsume(amount) spends an exact positive cost only when enough active charges exist, preserves regeneration progress, and emits OnBarrierCountChanged once, just like damage loss. See [barrier sacrifice attacks](StatsAndModifiers.md#barrier-sacrifice-attacks).

Barrier.OnBarriersLost(amount) reports actual charge loss from damage absorption
and successful TryConsume, after the count notification (and after damage
absorption finishes for TakeDamage). Restoration, reset and stat-driven maximum
clamping do not emit this event. See [Barrier Surge](ReactiveCombatEffects.md#barrier-surge-on-barrier-loss-2026-10-03).

The optional deferred-attack modifier introduces a pre-HP stage after those
resources have processed an attack. It postpones only HP-bound damage, preserving
mystic absorption and the direct DoT path. Debt payments bypass all defences but
still validate health and absorption death thresholds. See the
[debt contract](CombatAndEffects.md#deferred-attack-hp-damage-2026-10-03).
