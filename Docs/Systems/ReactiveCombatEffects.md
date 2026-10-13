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
and doubles that result while the owner is on low life. Unit.ReceiveDamage uses the cached PainReceived stat after actual attack HP loss; DoT grants no Pain.

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
| BlockPowerMomentum | Unblocked received attacks grant merged stacks, capped at ten; each adds fixed 0.1 Increased BlockPower. The next successful block uses the bonus then removes every stack. No timer; cleared on combat reset. See [implementation and setup](StatsAndModifiers.md#unblocked-hits-build-block-power-momentum-2026-10-12) |
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

EvadeToMitigationCharges now grants configurable Fortification stacks on OnEvade,
using the restoration producer pattern and the shared thirty-stack cap. The old
NextHitDamageMitigation effect and its dedicated mitigation asset were removed
on 2026-10-13. See [current modifier behavior](StatsAndModifiers.md#fortification-on-evade-2026-10-13).

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
- `Assets/Scripts/Battle/Effects/FortificationEffect.cs`
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

## Critical Charge (2026-10-04)

`NonCriticalHitsGrantCriticalCharge` binds/unbinds `Unit.OnNonCrit`, granting one
`CriticalCharge` per landed non-critical attack per modifier copy. Blocked and
fully absorbed hits count; evasion returns before this event. The merged buff has
three charges maximum and a shared four-second combat timer. Every application,
including at the cap, refreshes that timer. Values are fixed and do not scale with
node power. Recalculation rebuilds the subscription without clearing earned charges;
normal effect expiry and combat reset clear the buff.

After the critical roll, `AttackProcessor` calls `CriticalCharge.ApplyCriticalCharge`
once, regardless of modifier copies. A critical hit gains Added CritDamageBonus of
0.15 per charge in its own attack snapshot. All charges are removed immediately,
before damage and reactive callbacks, preventing nested attacks from spending them
again. Damage calculation refreshes CritDamageBonus using normal stat arithmetic;
Increased/More modifiers and multi-critical layers retain their existing behavior.
Misses neither grant nor consume charges. DoT does not enter this attack hook.
Existing analogues are RelentlessMomentum (merged count and non-crit runtime event
pattern) and Vengeance (attack-snapshot resource spending).

`EffectVisualType.CriticalCharge = 28` preserves previous serialized enum values.
The effect exposes charge count and timer progress. Its Descriptions tooltip asset
is registered as `criticalCharge` in TooltipTerms, as explicitly requested by the
owner. The fallback uses the same English text when no active tooltip database is
available. The status sprite and modifier icon require manual Editor assignment;
node wiring is not included.

Localization added: Modifiers / `modifier.nonCriticalHitsGrantCriticalCharge.description`
= `Non-critical hits grant a charge of {criticalCharge|Critical Charge}.`
Descriptions / `effect.criticalCharge.name` = `Critical Charge`; and
`effect.criticalCharge.description` = `Critical hits consume all charges, gaining +15% Critical Damage Bonus per charge. Lasts 4 seconds. Maximum 3 charges. Gaining a charge refreshes the duration.`
Russian/German keys are translator TODOs; existing locale tables are unchanged.

Source: `Assets/Scripts/Battle/Effects/CriticalCharge.cs`,
`Assets/Scripts/Battle/AttackProcessor.cs` and
`Assets/Scripts/SkillTree/Modifiers/ReactMods/NonCriticalHitsGrantCriticalCharge.cs`.
Modifier asset is beside its script; tooltip asset is
`Assets/Scripts/TooltipSystem/Descriptions/CriticalCharge.asset`.
Verification: installed Visual Studio Roslyn `csc.exe @Temp/CriticalChargeCompile.rsp`
compiled gameplay sources against Unity/project references with exit 0. Existing
warnings remain; none named the new classes. Localization IDs and script/tooltip
GUID references were checked. No tests, player build or gameplay playtest ran.

## Pain received stat (2026-10-12)

`PainReceived` replaces PainReceivedMultiplier at the same stable ID 94.
It is the fraction of actual attack HP loss converted to Pain, using ordinary
Added/Increased/More arithmetic. Two Added 0.05 contributions and Increased 0.2
produce 0.12: losing 100 HP grants 12 Pain, or 24 while on low life. Negative
final conversion is floored at zero. No stat yields zero gain; the owner will
remove the old innate Added 1 manually (it now means 100% conversion).

Unit.ReceiveDamage grants Pain after OnGettingHit and before OnHealthDamageTaken,
only for positive actual HP loss. Pain.CalculateGainFromHealthLost preserves the
existing low-life doubling. No reactive producer bindings remain. Pain.OnApply
and OnStack receive already computed amounts and never multiply them again;
stored Pain is unaffected by stat changes. DoT still bypasses this receipt path.
Pain consumption continues to notify Scar producers with the full stored amount.

DamageTakenAsPain.cs and its metadata were removed. All five definitions migrated
to BaseModifier / Added / PainReceived under
`Assets/Scripts/SkillTree/Modifiers/BaseModifiers/AddedPainReceived_*.asset`,
retaining their GUIDs and authored values so every node reference still resolves.
Suffixes are 0.02, 0.04, 0.05, 0.1 and 0.12; the last historically stores 0.1,
which was preserved rather than retuned. IncreasedPainReceived_0.2 is unchanged.
The existing parity fixture now constructs an ordinary Added PainReceived modifier;
it was not run. Node links, icons and positions were not changed.

Shared/English Modifiers entries have prefix `modifier.container.` and suffix
`.PainReceived`:
- added: `[[0]] {pain|Pain} gained` (percent formatting produces +X%)
- increased: `[[0]]% increased {pain|Pain} gained`
- decreased: `[[0]]% decreased {pain|Pain} gained`
- more: `[[0]]% more {pain|Pain} gained`
- less: `[[0]]% less {pain|Pain} gained`
- none: `You cannot gain {pain|Pain}`

IDs were retained while renaming the keys. English fallbacks match these texts.
ru/de translations remain TODOs; no empty entries or new effect-name keys added.
Existing pain glossary term and status presentation are reused. Old translated
DamageTakenAsPain localization entries remain unused to preserve translations.

Verification: dotnet build Assembly-CSharp.csproj --no-restore -v:minimal -m:1
-p:BuildProjectReferences=false with CustomBeforeMicrosoftCommonTargets set to
Temp/MorePainCompile.targets passed with zero errors and five existing dependency/
TutorialEventAdapter warnings. The target excludes deleted sources still present
in the generated project. Asset GUID/value/reference checks and scoped whitespace
checks were performed. No tests, player build or runtime playtest performed.
