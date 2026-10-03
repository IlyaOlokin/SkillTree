# Reactive combat effects

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Combat lifecycle](CombatAndEffects.md)

Status: **source-reviewed, 2026-09-28**. Complements [ailments](AilmentsAndDebuffs.md)
with the remaining named combat effect families and their consumption boundaries.
This is not an audit of every modifier asset or an approval of its balance.

## Stored attack resources

Pain accumulates a nonnegative amount. On an attack that passes evasion, it adds
Increased Damage equal to amount / attacker's maximum HP to the attack snapshot,
then queues consumption. Consumption raises PainConsumed and marks it used.
Its gain helper multiplies positive health lost by a nonnegative conversion factor
and doubles that result while the owner is on low life. The applying modifier
still determines when and with what factor that helper is used.

Vengeance accumulates nonnegative stacks. Its attack bonus is one More Damage
contribution of `0.05 * stacks` plus Added AilmentChance of `0.1 * stacks`, followed
by queued consumption. This is not one separate multiplicative factor per stack.
Like Pain, it is read from the attacker after evasion, unlike defender-owned
[Overcharge](AilmentsAndDebuffs.md#overcharge).

Scar is a separate timed instance per application. It snapshots amount / maximum
HP on application and grants that value as Increased Armor and Added AilmentGuard,
while applying the negative value as Added HealingReceived. Removal detaches all
three modifiers. Changing maximum HP does not recalculate the stored fraction here.

## Temporary modifier families

| Effect | State and modifier contract |
| --- | --- |
| CriticalMomentum | Merges added stacks; applies More CritChance loss and More CritDamageBonus gain using the existing instance's per-stack coefficients |
| CriticalRegeneration | Independent timed instances adding PercentHealthRegeneration modifiers |
| BarrierSurge | Independent timed instances adding Increased BarrierRegenerationSpeed and Increased ElementalDamage; shared icon can show count and closest expiry |
| EvasiveMomentum | Independent timed instances with More Evasion loss and More CritChance gain; MaxStacks constant is three, but this effect class alone does not enforce insertion count |
| RelentlessMomentum | Merged count capped at ten; each stack adds 0.05 Increased AttackSpeed and 0.05 Increased Damage |
| BleedPhysicalDamageBuff | Independent timed instances adding nonnegative flat PhysicalDamage |
| Mini-game stat buffs | Replace same-type strength and refresh duration; see [mini-games](BattleMiniGames.md) |

The effect's mechanics and the trigger producing it are separate. Trace the
corresponding reactive modifier before changing gain frequency, stack caps or
removal conditions. Do not infer a complete trigger rule from a class or icon name.
All outer modifiers must be detached through the effect lifecycle; owned runtime
objects are released by the controller. See [modifier bindings](StatsAndModifiers.md).

## Next-attack and next-hit boundaries

NextAttackModifierEffect adds an outer modifier and listens to the owner's OnHit.
That marks it used; normal effect cleanup removes it. Evasion does not emit OnHit.
It can own its supplied modifier when explicitly requested by the caller.

TimedNextAttackModifierEffect keeps a source-linked rechargeable state and can
copy its charged container to an attack snapshot. It discharges on OnAttackCompleted,
not OnHit; that event also occurs for evaded attacks. Do not reuse the ordinary
next-hit consumption assumption for this class.

NextHitDamageMitigation installs an outer modifier and schedules pending stat
recalculation after the current attack. OnGettingHit marks it used and removes
its own active entry immediately; removal also schedules recalculation. This
receipt callback is distinct from positive HP loss and may occur with full absorption.

CyclicAilmentChanceIndicatorEffect tracks an Ignite/Chill/Overcharge chance cycle
and subscribes to OnAttackCompleted. Its source presence is checked on effect ticks.
Like other source-linked effects, runtime source removal and combat reset must
clean up subscriptions; use [effect events](../Reference/EffectApplicationEvents.md) when
building repeat reactions rather than recursively replaying generic notifications.

## Verification and sources

Suggested checks: hit versus miss consumption, several attacks between effect
ticks, source removal, same-type applications with different strengths, stack cap
enforcement in the producer, temporary modifier cleanup and maximum-HP changes.
No combat tests were run for this documentation-only pass.

Source files are under `Assets/Scripts/Battle/Effects`:

- `Assets/Scripts/Battle/Effects/Pain.cs`
- `Assets/Scripts/Battle/Effects/Vengeance.cs`
- `Assets/Scripts/Battle/Effects/Scar.cs`
- `Assets/Scripts/Battle/Effects/CriticalMomentum.cs`
- `Assets/Scripts/Battle/Effects/CriticalRegeneration.cs`
- `Assets/Scripts/Battle/Effects/BarrierSurge.cs`
- `Assets/Scripts/Battle/Effects/EvasiveMomentum.cs`
- `Assets/Scripts/Battle/Effects/RelentlessMomentum.cs`
- `Assets/Scripts/Battle/Effects/BleedPhysicalDamageBuff.cs`
- `Assets/Scripts/Battle/Effects/NextAttackModifierEffect.cs`
- `Assets/Scripts/Battle/Effects/TimedNextAttackModifierEffect.cs`
- `Assets/Scripts/Battle/Effects/NextHitDamageMitigation.cs`
- `Assets/Scripts/Battle/Effects/CyclicAilmentChanceIndicatorEffect.cs`

## Barrier Surge on barrier loss (2026-10-03)

BarrierLossGrantsBarrierSurge subscribes through a per-owner runtime binding to
Barrier.OnBarriersLost(amount). Damage absorption and successful TryConsume emit
the actual spent count; losing N charges grants N independent stacks. Restoration,
combat reset and maximum-count clamping do not grant stacks. Default asset values
are 6 seconds, 30% Increased BarrierRegenerationSpeed and 20% Increased
ElementalDamage per stack. Both bonuses scale with modifier power; duration does
not. There is no stack cap. Each instance removes its own modifiers on expiry.
Visual type BarrierSurge retains serialized ID 11 and the existing shared icon.
The Descriptions tooltip receives actual per-instance bonus and duration arguments.
Source and asset: Assets/Scripts/SkillTree/Modifiers/ReactMods/BarrierLossGrantsBarrierSurge.
Compilation verification is separate from a Unity playtest.

Verification for this change: MSBuild.exe Assembly-CSharp.csproj /t:Build /p:Configuration=Debug /v:minimal /nologo succeeded. The generated project source path was temporarily updated for the rename and restored afterward. Existing dependency conflicts, deprecated Unity APIs and unrelated analyzer warnings remain. No tests or Unity playtest were run.

Localization: Modifiers / modifier.barrierLossGrantsBarrierSurge.description; Descriptions / description.barrierSurge1, description.barrierSurge2 and effect.BarrierSurge.name. English entries are updated; existing ru/de translations are preserved and require translator updates for these keys. No empty locale entries were added.
