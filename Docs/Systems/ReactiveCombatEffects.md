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

## Periodic elemental charge generation (2026-10-18)

`PeriodicElementalCharges` in
`Assets/Scripts/SkillTree/Modifiers/ReactMods/PeriodicElementalCharges.cs` grants
an internal `PeriodicElementalChargesEffect`. It follows the timer/reset binding
pattern of AilmentAbsorptionCooldown and the direct self-buff insertion of
CombatStartGrantsElementalCharges. No battle-flow changes or new events are needed.

Inspector fields are Charge Type (Embers=0, Rime=1, Sparks=2, Random=3), Interval
(default 2 seconds, minimum 0.01), and Stacks (default 1, nonnegative integer).
The first grant occurs after one complete interval. Random independently chooses
one type with equal probability for each grant; the entire batch has that type.
Each contribution grants `max(0, floor(Stacks * powerContext.Multiplier))` charges;
power changes count only, not interval or the damage per charge. Zero count
creates no generator. Long combat ticks process every elapsed interval.

Each collected contribution has an independent timer, including repeated copies
of the same asset. The untimed, non-stackable generator uses controller effect
ticks, so it respects the normal combat pause. Rebinding after stat recalculation
reclaims an unbound effect with the same source modifier and source node and
updates its settings without restarting the remaining interval. Removal unbinds
the generator immediately; its next effect tick removes it without granting.
Already granted charges retain their ordinary consumption/reset lifecycle.
OnCombatStateReset clears the old generator and charges, then creates a fresh
timer through the binding's reset subscription. This is the existing reset
boundary, including map/shop transitions, rather than a new battle-start event.

The generator is hidden through `BaseEffect.ShowStatusIcon = false`; the existing
Embers/Rime/Sparks effects provide the visible icons, counts, names and glossary
links. `UnitVisualEffectsController` checks this flag, which defaults to true for
all other effects. VisualType.None alone is insufficient to hide an effect because
the icon resolver supplies a default sprite. No new EffectVisualType, effect-name
key, tooltip asset, tooltip database entry or serialized icon mapping is needed.
Direct insertions have no repeat factory/source attribution, so received-effect
repetition does not multiply the generator or charge grants.

Created modifier definition:
`Assets/Scripts/SkillTree/Modifiers/ReactMods/PeriodicElementalCharges_Random_2s_1.asset`.
Its script GUID is `32531ace296714a4782abb473c879da3`; settings are Random, 2, 1.
The [LocalModifiers gem](Gems.md#periodic-elemental-charge-gem-2026-10-18) references
it and is sold in [Shop4](EconomyAndLoot.md#elemental-charge-gem-in-shop4-2026-10-18).

Added shared/English entries in **Modifiers**:

| Key | English text |
| --- | --- |
| modifier.periodicElementalCharges.embers.description | Every [[0]] seconds, gain [[1]] {embers\|Embers} charges. |
| modifier.periodicElementalCharges.rime.description | Every [[0]] seconds, gain [[1]] {rime\|Rime} charges. |
| modifier.periodicElementalCharges.sparks.description | Every [[0]] seconds, gain [[1]] {sparks\|Sparks} charges. |
| modifier.periodicElementalCharges.random.description | Every [[0]] seconds, gain [[1]] charges of a random type: {embers\|Embers}, {rime\|Rime} or {sparks\|Sparks}. |

Added **Descriptions**, literal key `Elemental Charge Gem`, English value
`Elemental Charge Gem`. The literal key is also the gem's readable English
fallback when a locale has no translation. Modifier descriptions have matching
English code fallbacks. Existing charge display-name keys are reused. These five
new entries need ru/de translations; no empty entries or translations were added.

**Manual Unity Editor Steps:** none required for the supplied gem and Shop4 entry.
For another variant, create a Periodic Elemental Charges modifier and choose Charge
Type, Interval and Stacks; assign it to the desired node or gem manually. The
supplied gem uses the existing Steel Gem placeholder sprite
`Assets/Sprites/IconsDepricated/skill_316.png`; an optional dedicated icon can be
imported and assigned to its Icon field. No image was generated.

**Icon Generation Prompt** — proposed style; no image reference inspected:
Create a square painterly dark-fantasy RPG skill icon of a crystalline hourglass
releasing a glowing orange ember, a sharp icy-blue frost crystal and a golden
electric spark from its lower chamber. Use one bold centered hourglass silhouette
with three compact elemental accents to communicate a recurring supply of energy.
Deep violet crystal, orange, icy blue and gold palette, dramatic rim lighting,
dark charcoal background, simple high-contrast shapes readable at small skill-tree
icon sizes. No text, letters, numbers, logos or watermarks.

Verification: live Unity CLI run_script created the modifier/gem and appended one
Shop4 entry with amount 1, price 250 and stock limit 3. It verified the script
reference, modifier defaults, gem settings/save ID, existing shop entry IDs,
English/shared localization IDs, registered charge terms and GemDefinitionCatalog
resolution. Final Unity CLI recompile/recompile_status targeting
`C:/UnityProjects/SkillTree` completed after the status-icon filter was added:
status completed, failed=false, compilationFailed=false and no compiler errors.
Source review covered binding reuse, reset/removal, multiple copies, random batch
selection and tick catch-up. No tests, player build, combat or visual playtest.

## Elemental charges: Embers, Rime and Sparks (2026-10-18)

These three independent, untimed buffs use `ElementalChargeEffect`. Every charge
grants fixed Added 10 Fire, Cold or Lightning Damage respectively. There is no
gameplay stack cap. Same-type applications merge their charge counts; different
elements remain separate. Constructors accept an optional integer charge count,
default one: `new Embers(stacks)`, `new Rime(stacks)`, `new Sparks(stacks)`.
Nonpositive applications are rejected. The combat-start producer described below
grants these buffs after a combat reset; node assignment remains manual.

Like `RelentlessMomentum`, each element has a merged count. Like
`TimedNextAttackModifierEffect`, consumption happens at attack completion.
`OnAttackPrepared` adds 10 times the current charge count to that attack's typed
Added bucket, preserving ordinary Increased/More/critical scaling without changing
the owner's persistent stats or waiting for the Mods phase. `OnAttackCompleted`
removes exactly one charge from each participating buff, including missed or
blocked attacks. All charges contribute before consumption. Each extra attack
prepares and spends independently. DoT does neither. A buff first granted after
preparation waits for the next attack. Replenishing an existing buff during an
attack does not alter the already-prepared damage.

Zero charges remove the effect immediately through EffectController. Removal and
combat reset unsubscribe both attack callbacks. There is no source lifetime,
timer, separate runtime modifier, runtime binding or new attack hook. Calling
AttackProcessor directly without the Unit preparation/completion lifecycle does
not use these buffs, matching other preparation-event mechanics.

Appended `EffectVisualType` IDs: Embers 30, Rime 31, Sparks 32. Each status icon
shows the charge count, including one. Each effect resolves its registered
TooltipDescriptionData through `embers`, `rime` or `sparks` in TooltipTerms.
The matching assets are `Assets/Scripts/TooltipSystem/Descriptions/Embers.asset`,
`Rime.asset` and `Sparks.asset`. Titles use the corresponding effect-name keys
and are enabled for linked tooltips. Descriptions use the effect-description keys
below. Literal 10 keeps separately opened glossary tooltips independent of runtime
formatting arguments. The English code fallback uses the same text. Descriptions
contain no nested glossary links.

Added shared/English entries in **Descriptions**:

| Key | English text |
| --- | --- |
| effect.embers.name | Embers |
| effect.embers.description | Each charge grants +10 Fire Damage. Attacks consume 1 charge. |
| effect.rime.name | Rime |
| effect.rime.description | Each charge grants +10 Cold Damage. Attacks consume 1 charge. |
| effect.sparks.name | Sparks |
| effect.sparks.description | Each charge grants +10 Lightning Damage. Attacks consume 1 charge. |

These effect classes need no Modifiers keys; the producer's keys are listed below.
ru/de translations of these six keys remain translator TODOs; existing translations
are preserved and no empty locale entries were added.

**Manual Unity Editor Steps:** assign sprites for the three new visual types in
`Assets/Scripts/Visual/EffectIconsConfig.asset`. Until then status slots have charge
text and tooltips but no sprite. The combat-start producer is described below.
There is no ScriptableObject effect definition to create or link to a node.
The tooltip assets and database registration are already configured.

**Icon Generation Prompts** — proposed style; no image reference inspected:

- Embers: Square painterly fantasy RPG icon of clustered glowing embers, centered
  bold silhouette, orange-red palette, hot core lighting, dark charcoal background,
  readable at small sizes. No text, letters, numbers, logos or watermarks.
- Rime: Square painterly fantasy RPG icon of clustered sharp frost crystals,
  centered bold silhouette, icy blue-white palette, cold rim lighting, dark navy
  background, readable at small sizes. No text, letters, numbers, logos or watermarks.
- Sparks: Square painterly fantasy RPG icon of clustered electric sparks around
  a bright core, centered bold silhouette, gold-violet palette, electric glow
  lighting, dark purple background, readable at small sizes. No text, letters,
  numbers, logos or watermarks.

Source: `Assets/Scripts/Battle/Effects/ElementalChargeEffect.cs`, `Embers.cs`,
`Rime.cs`, `Sparks.cs` and `EffectVisualType.cs`. Localization assets:
`Assets/Localization/Descriptions_en.asset` and `Descriptions Shared Data.asset`.
Verification: Unity CLI `recompile` / `recompile_status`, explicitly targeting
`C:/UnityProjects/SkillTree`, completed with `failed=false`,
`compilationFailed=false` and no compiler errors. Live `run_script` saved and
verified the six shared/English entries and matching shared IDs. Scoped diff
checks covered the sources, generated script metas, localization and this page.
The live authoring script also verified all three term IDs resolve to their
corresponding description assets.
No tests, player build or gameplay playtest were run. Attack lifecycle behavior
is source-reviewed; runtime behavior and status-icon appearance remain unverified.

### Embers on enemy kill (2026-10-18)

`EnemyKillGrantsEmbers` is a reactive producer in
`Assets/Scripts/SkillTree/Modifiers/ReactMods/EnemyKillGrantsEmbers.cs`.
It follows `EnemyKillRestoresBarrier` for the per-owner `OnEnemyKilled`
delegate binding and `CombatStartGrantsElementalCharges` for powered charge
grants. Inspector `Stacks` is a nonnegative integer, default 1. Each notification
grants the active owner `max(0, floor(Stacks * powerContext.Multiplier))` Embers.
Zero grants nothing; rounding is per copy. Multiple copies add to the existing
Embers count without introducing a cap or changing damage per charge.

The existing `EnemyUnit.Death` notification credits enemy deaths to the injected
player target, including DoT deaths, without checking the final damage source.
The health death guard prevents duplicate notifications until reset. No new
battle event or flow change is introduced. Direct self-effect insertion supplies
no repeat factory/source attribution. Rebinding never grants charges; unbinding
stops future grants while earned charges retain their normal consumption/reset
lifecycle. A first grant during a killing attack waits for the next prepared
attack; a replenished existing Embers effect still consumes its usual one charge
at that attack's completion. No separate effect or visual type is needed.

Definition: `Assets/Scripts/SkillTree/Modifiers/ReactMods/EnemyKillGrantsEmbers_1.asset`,
with `stacks: 1`, ordinary PreAttribute priority and script GUID
`9d87cf2840135a64da44a18660aee6f5`. Added shared/English **Modifiers** entry
`modifier.enemyKillGrantsEmbers.description`:
`On Kill: gain [[0]] {embers|Embers} charges.`
The displayed count uses the same power scaling and rounding as the grant.
Existing `Descriptions` entries `effect.embers.name` = `Embers` and
`effect.embers.description` = `Each charge grants +10 Fire Damage. Attacks consume 1 charge.`
are reused without edits. The registered `embers` term resolves to the existing
Embers description asset. ru/de translation of the new modifier key remains a
translator TODO; no empty entries were added and existing translations remain.

**Manual Unity Editor Steps:** assign the supplied definition to the intended
node's modifier list; adjust Stacks if needed and choose a node icon. Existing
Embers status visuals, count and glossary are reused. No new effect-name key,
tooltip asset, database registration or effect-icon configuration is required.
The modifier is not assigned to gameplay content by this change.

**Icon Generation Prompt:** Create a square painterly dark-fantasy RPG skill
icon matching the angular brushwork and glowing charcoal of EmbersIcon.png.
Show a crumbling charcoal skull releasing a compact cluster of burning embers
upward, with one bold centered silhouette and a clear separation between the
fallen skull and rising coals. Use black charcoal, deep crimson and brilliant
orange-yellow cracks, intense hot-core lighting and a dark smoky background.
Keep the shapes simple and readable at small skill-tree icon sizes. No text,
letters, numbers, logos or watermarks.

Verification: Unity CLI `recompile` / `recompile_status`, targeting
`C:/UnityProjects/SkillTree`, completed with `failed=false`,
`compilationFailed=false` and no compiler errors. The Editor reported ready
before the authoring mutation. Live `run_script` created the definition and
English entry and verified stacks, script GUID, shared/English ID pairing and
the existing glossary reference. Source review covered binding cleanup, event
attribution, same-type stacking and killing-attack consumption boundaries.
No tests, player build or combat playtest were performed.

### Combat-start charge modifier (2026-10-18)

`CombatStartGrantsElementalCharges` is a reactive reset producer in
`Assets/Scripts/SkillTree/Modifiers/ReactMods/CombatStartGrantsElementalCharges.cs`.
It reuses the per-owner `DelegateModifierRuntimeBinding` and
`OnCombatStateReset` subscription pattern from `AilmentAbsorptionCooldown`, but
deliberately does not grant anything on Bind. Unit recalculation unbinds/rebinds
runtime subscriptions, so granting on Bind would refill charges during tree or
stat changes. There is no new effect class, battle event or combat-flow change.

Inspector fields select `ChargeType` (Embers=0, Rime=1, Sparks=2) and nonnegative
integer `Stacks`, default 1. Each reset grants the owner
`max(0, floor(Stacks * powerContext.Multiplier))` charges of the selected type.
Rounding happens per modifier copy. Multiple copies add through the existing
same-type charge merge; different types remain independent. Zero grants nothing.
Power scales the grant count, not the fixed 10 damage per charge. Description
formatting uses the same rounded count and powered-value highlighting.

`Unit.ResetCombatState` clears existing effects before emitting the event, so each
reset replaces the previous combat's reserves with fresh grants. PlayerUnit.Start
binds modifiers before its initial reset. Current level/location-entry and restart
paths reset combat, and map/shop transitions also reset it: charges can therefore
be visible outside battle, then be cleared and granted again on entry. This is
the existing reset boundary, not a new battle-start event. Allocating the modifier
mid-combat waits for the next reset; ordinary recalculation never refills spent
charges. Unbinding prevents future grants but leaves earned charges under their
normal attack-consumption/reset lifecycle. Direct self-effect insertion supplies
no repeat factory or source attribution, so generic effect-receipt repeats do not
multiply the grant.

Three definitions were created through the live Editor under
`Assets/Scripts/SkillTree/Modifiers/ReactMods/`, each with Stacks=1 and the matching
ChargeType:

- `CombatStartGrantsEmbers_1.asset`
- `CombatStartGrantsRime_1.asset`
- `CombatStartGrantsSparks_1.asset`

Their script GUID is `5125514bdb0058949bfeae0383167e6b`. No node is assigned.
Existing effect visual types, status icons/counts, display-name keys and registered
`embers`, `rime`, `sparks` tooltip descriptions are reused. No new tooltip asset,
database entry or effect-name key is required. Missing modifier translations use
the matching English code fallback through GameLocalization.

Added shared/English entries in **Modifiers**:

| Key | English text |
| --- | --- |
| modifier.combatStartGrantsEmbers.description | At the start of combat, gain [[0]] {embers\|Embers} charges. |
| modifier.combatStartGrantsRime.description | At the start of combat, gain [[0]] {rime\|Rime} charges. |
| modifier.combatStartGrantsSparks.description | At the start of combat, gain [[0]] {sparks\|Sparks} charges. |

Assets changed: `Assets/Localization/Modifiers Shared Data.asset` and
`Assets/Localization/Modifiers_en.asset`. ru/de translations of these three keys
are translator TODOs; no existing translations or empty locale entries were added.

**Manual Unity Editor Steps:** choose a supplied definition, set Stacks to the
desired starting amount and assign it to the intended node's modifier list. Choose
the node icon manually. To author another variant, use Create > Modifiers >
Reactive > Combat Start Grants Elemental Charges and select Charge Type/Stacks.
Existing effect-icon/glossary configuration is reused without changes by this task.

**Icon Generation Prompt** — proposed style, no image reference inspected:
Create a square painterly dark-fantasy RPG skill icon of an open silver reliquary
holding three compact clusters of stored elemental energy: glowing orange embers,
sharp icy blue frost crystals and golden electric sparks. Arrange them around one
bold centered silhouette to suggest a prepared reserve released as battle begins.
Use orange, icy blue and gold accents, dramatic rim lighting and a dark charcoal
background. Keep the composition simple, high contrast and readable at small
skill-tree icon sizes. No text, letters, numbers, logos or watermarks.

Verification: Unity CLI `recompile` / `recompile_status` targeting
`C:/UnityProjects/SkillTree` completed with `failed=false`,
`compilationFailed=false` and no compiler errors. Live `run_script` created and
checked all three definitions, their script GUID, selected type/count and the
shared/English key-ID pairs. Source review covered reset ordering, symmetric
binding cleanup, no Bind grant, stacking and matching description counts.
No tests, player build or combat playtest were performed. Actual gameplay timing
and UI appearance remain unverified.

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
