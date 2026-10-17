# Stats and Modifiers

Source review, 2026-10-17: [StatType consumers, correctness findings and optimization
priorities](../Reference/StatTypeReview20261017.md). The subsequent
[fix record](../Reference/StatTypeFixes20261017.md) documents the implemented
corrections, collection/storage optimizations, Poison removal and successful Unity
compilation. Mask semantics remain unchanged; gameplay/performance checks are pending.

## Fortification limits as stats (2026-10-13)

`StatType.MinimumFortificationStacks = 95` and
`StatType.MaximumFortificationStacks = 96` replace the two dedicated modifier
classes. Existing serialized enum IDs remain unchanged. `Unit.ResetUnit` supplies
Added MaximumFortificationStacks = 30 before innate and collected modifiers;
MinimumFortificationStacks starts at zero. Ordinary `BaseModifier` / containers
can contribute Added, Increased or More and use normal node-power scaling.
The usual Added × (1 + Increased) × product(More factors) formula applies to the
whole maximum, including its base 30. For example Added +5 and -4 give 31; 20%
Increased without Added bonuses gives 36.

Both final stat values are floored after all arithmetic (in `StatCalculator` and
cached-value setters), never per contribution. Two contributions of 2.7 produce
5, not 4. `FortificationController` clamps the effective maximum to a nonnegative
integer and the effective minimum to 0–maximum. New stats display as counts with
zero decimal places; modifier descriptions may show fractional contributions
because those combine before flooring. They are not implicitly part of Defence
or attack-snapshot stat groups.

The plain C# `FortificationController` is constructed/disposed by `Unit`, without
new prefab components or Inspector wiring. It observes OnStatsRecalculated and
OnEffectsChanged, trims stacks above the maximum, fills missing minimum stacks
and reestablishes the minimum after combat reset/ordinary effect clearing.
Reentrant notifications during its own changes are guarded. `FortificationEffect`
uses generic `BaseEffect.CanApply` / `CanRemove` hooks to enforce upper/lower
bounds in every effect insertion/removal path. ClearAllEffects bypasses removal
restrictions; controller destruction clears change listeners before teardown.
`EffectController` contains no Fortification-specific formulas or registrations.
Barrier/Evade/Bleed producers read `unit.Fortification.Maximum`. Each stack grants
1% Damage Mitigation through one controller-owned runtime BaseModifier: 30 stacks
contribute one Added DamageMitigation value of 0.30. This aggregate expands to
per-type mitigation and combines with other contributions through ordinary stat arithmetic.
Stack changes queue the ordinary deferred stat recalculation; the controller
removes its modifier at zero stacks and disposes it on owner destruction.
AttackProcessor applies the resulting mitigation before ailments, block and damage
receipt, clamped to 0–100%. Existing DoT pools are not recalculated or reduced on each tick.
Receiving an attack with positive incoming damage consumes up to five stacks;
DoT does not consume Fortification stacks. Consumption respects the minimum
above the minimum. Existing status visuals/name/glossary are reused. Stat-name tooltip links map to the existing registered fortification term through StatTypeTooltipFormatter.

Removed `SpecialMods/MinimumFortificationStacks.cs` and
`SpecialMods/MaximumFortificationStacks.cs` with their script metas. Their old
assets, including MaximumFortificationStacks_1/_3, and assignments are intentionally
left for the owner's manual replacement with BaseModifier; they will have missing
scripts until replaced. No replacement BaseModifier assets or node changes were
made. Old modifier-description localization keys are retained for migration.

New shared/English localization entries (ru/de translator TODOs; no empty entries):

| Table | Key | English text |
| --- | --- | --- |
| Descriptions | enum.StatType.MinimumFortificationStacks | Minimum Fortification Stacks |
| Descriptions | enum.StatType.MaximumFortificationStacks | Maximum Fortification Stacks |
| Modifiers | modifier.container.added.MinimumFortificationStacks | [[0]] to minimum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.increased.MinimumFortificationStacks | [[0]]% increased minimum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.decreased.MinimumFortificationStacks | [[0]]% decreased minimum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.more.MinimumFortificationStacks | [[0]]% more minimum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.less.MinimumFortificationStacks | [[0]]% less minimum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.none.MinimumFortificationStacks | Your minimum number of {fortification\|Fortification} stacks is 0. |
| Modifiers | modifier.container.added.MaximumFortificationStacks | [[0]] to maximum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.increased.MaximumFortificationStacks | [[0]]% increased maximum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.decreased.MaximumFortificationStacks | [[0]]% decreased maximum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.more.MaximumFortificationStacks | [[0]]% more maximum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.less.MaximumFortificationStacks | [[0]]% less maximum {fortification\|Fortification} stacks. |
| Modifiers | modifier.container.none.MaximumFortificationStacks | Your maximum number of {fortification\|Fortification} stacks is 0. |

Manual Unity Editor Steps: replace old definitions/assignments with BaseModifier
assets, Modifier Type Added, desired Stat Type MinimumFortificationStacks or
MaximumFortificationStacks, and signed Value. Keep the ordinary PreAttribute
priority. Do not manually add another base maximum of 30. Configure node icons
manually. The registered fortification glossary remains valid; its static text
states the base maximum 30 and does not dynamically show a particular owner's
modified limits. A tooltip asset/configuration change remains manual.

Bonus update (2026-10-16): restored 1% Added DamageMitigation per stack,
replacing the 2026-10-14 More Defence bonus. English Descriptions entries
`effect.fortification.reduction` and `effect.fortification.description` now use
`Each stack grants 1% Damage Mitigation.` as their first paragraph. The full
description and `effect.fortification.consumption` describe consumption up to the minimum floor.
Existing ru/de translations need updating. The registered tooltip asset, keys,
effect display name and status icon are reused without serialized wiring changes.
Verification for the bonus update: Unity CLI `recompile` / `recompile_status`
targeting C:/UnityProjects/SkillTree completed with no compilation errors.
Source review checked one aggregate contribution, zero-stack removal and disposal.
No tests, player build or runtime playtest were performed.

Verification for the 2026-10-16 mitigation restoration: Unity CLI `recompile` /
`recompile_status` targeting C:/UnityProjects/SkillTree completed without compiler
errors. `run_script` updated both existing English keys and verified that the
`fortification` glossary term resolves to the existing description asset. Scoped
diff checks passed. No new definitions, localization keys, enum values, runtime
bindings or manual Editor configuration are needed. ru/de text remains a
translator TODO. No tests, player build or runtime playtest were performed.

Icon Generation Prompt (proposed style, minimum): Create a square painterly
fantasy RPG icon of a steel shield anchored by interlocking luminous cyan plates.
Bold centered silhouette, silver and deep blue palette, cold rim lighting, dark
smoky background, strong contrast readable at small sizes. No text, letters,
numbers, logos or watermarks.

Icon Generation Prompt (proposed style, maximum): Create a square painterly
fantasy RPG icon of a steel shield encircled by adjustable cyan armor segments,
with one segment sliding into place. Bold centered silhouette, silver and deep
blue palette, cold rim lighting, dark smoky background, strong contrast readable
at small sizes. No text, letters, numbers, logos or watermarks.

Verification: Unity CLI recompile/recompile_status targeting
C:/UnityProjects/SkillTree completed without compilation errors. Source review
covered final flooring, signed Added stacking, default initialization, cap/floor
hooks and lifecycle cleanup. New localization entries were authored through the
live Editor. No tests, player build or runtime playtest were performed.

## Fortification on Evade (2026-10-13)

`EvadeToMitigationCharges` retains its class, script GUID and existing definition
`Assets/Scripts/SkillTree/Modifiers/ReactMods/EvadeToMitigationCharges.asset` so
node references remain valid. Its Inspector now exposes `stacksPerEvade` (default
1, nonnegative integer), replacing the obsolete BaseModifier reference. Each
owner `OnEvade` grants that many Fortification stacks, limited by the shared thirty
active-stack cap. The amount is fixed rather than scaled by node power. Multiple
copies grant in subscription order and share the cap. Zero grants nothing.

The existing per-owner delegate binding unsubscribes on removal/recalculation;
earned Fortification remains under its own lifecycle. The grant pattern follows
`BarrierRestorationGrantsFortification`: direct independent effects, without a
repeat factory or source-side effect attribution. Fortification supplies its
existing status icon, display name and glossary description.

Updated English table `Modifiers`, key
`modifier.evadeToMitigationCharges.description`:
`On Evade: gain [[0]] stacks of {fortification|Fortification}.`
The existing registered Fortification term is reused. No new glossary asset,
description-name key or empty ru/de entries were added; ru/de translation is TODO.
The Editor creation menu is `Modifiers/Reactive/Evade Grants Fortification`.

Removed unused `NextHitDamageMitigation.cs`/`.meta`, its icon-config entry and
`BaseModifiers/DamageMitigation_0.1.asset`/`.meta` after checking references.
EffectVisualType ID 5 is retired; remaining serialized IDs are unchanged. The
icon sprite itself was retained. Manual Unity Editor Steps: adjust Stacks Per
Evade on the existing definition if a different grant amount is desired.

Icon Generation Prompt (proposed style; existing mappings retained):
Create a square painterly dark-fantasy RPG skill icon showing a silver shield
forming from luminous cyan armor plates as a blurred incoming blade passes beside
it. Centered bold shield silhouette, clear sense of an avoided strike becoming
protection, cold azure glow, dramatic rim lighting, dark smoky background, strong
contrast readable at small sizes. No text, letters, numbers, logos or watermark.

Verification: Unity CLI recompile/recompile_status targeting
`C:/UnityProjects/SkillTree` reported up_to_date with no compilation errors after
the Editor compiled the changes. Scoped diff whitespace checks passed. Serialized stacksPerEvade=1,
preserved script GUID, English localization and obsolete-reference removal were
checked. No tests, player build or runtime playtest requested or performed.

## Unblocked hits build Block Power Momentum (2026-10-12)

`UnblockedHitGrantsBlockPower` uses the `HitTakenGrantsVengeance` per-owner
delegate binding pattern and the `RelentlessMomentum` merged outer-modifier
pattern. Each copy grants one stack when the owner receives an unblocked attack,
including attacks fully absorbed by Barrier or mystic absorption. Evasion and DoT
do not emit the receipt event. `DamageInfo.IsBlocked` is reset with the attack
payload and set before block callbacks, so a partially damaging blocked hit does
not grant a new stack after consuming the old buff.

`BlockPowerMomentum` grants fixed 0.1 Increased BlockPower per stack, capped in
the effect at ten total stacks across all copies. Neither strength nor cap scales
with node power. There is no timer. Its own OnBlock subscription removes all
stacks after Block captures/applies the boosted flat power; Parry uses that captured
power too. A zero-power successful block also consumes the buff. Effect removal
unsubscribes the block listener, detaches the outer modifier and releases the owned
runtime ScriptableObject. Combat reset/ordinary effect clearing remove all stacks;
modifier rebinding preserves earned stacks. Removing the producer stops new gains
but earned stacks remain until a block or combat reset. Self-buff insertion has no
repeat factory or source attribution. Stat changes use the existing deferred Mods
phase contract. Multiple attacks in one Actions phase may precede recalculation.

Code:
- `Assets/Scripts/SkillTree/Modifiers/ReactMods/UnblockedHitGrantsBlockPower.cs`
- `Assets/Scripts/Battle/Effects/BlockPowerMomentum.cs`
- `Assets/Scripts/Battle/DamageInfo.cs`, `AttackProcessor.cs` and
  `Effects/EffectVisualType.cs` (new appended ID 29).

Definition: `Assets/Scripts/SkillTree/Modifiers/ReactMods/UnblockedHitGrantsBlockPower.asset`.
All settings are fixed in code; no Inspector tuning fields are needed.
`Assets/Scripts/Visual/EffectIconsConfig.asset` maps the status icon to the owner's
requested `Assets/Sprites/Icons/BlockPowerIcon.png` sliced sprite. The icon shows
the stack count, including one. The tooltip uses code fallback/English localization
without requiring a new glossary asset. The existing `blockPower` link resolves
through TooltipTerms to the existing BlockPower description.

Added shared/English localization entries (existing ru/de translations preserved;
these three keys remain translator TODOs, no empty locale entries added):

| Table | Key | English text |
| --- | --- | --- |
| Modifiers | `modifier.unblockedHitGrantsBlockPower.description` | When you receive an unblocked hit, gain 1 stack of Block Power Momentum. Each stack grants 10% increased {blockPower\|Block Power}. Maximum 30 stacks. Blocking a hit removes all stacks. |
| Descriptions | `effect.blockPowerMomentum.name` | Block Power Momentum |
| Descriptions | `effect.blockPowerMomentum.description` | Each stack grants 10% increased {blockPower\|Block Power}. Maximum 30 stacks. Blocking a hit removes all stacks. No time limit. |

Manual Unity Editor steps: assign the supplied modifier asset to the intended node;
use BlockPowerIcon for that node's NodeVisual.nodeImage. If a separately linked
Block Power Momentum glossary term is desired, create its TooltipDescriptionData
and register its ID/reference in TooltipTerms together. No such new link is emitted
by this modifier. Node and tooltip configuration were not changed.

Icon Generation Prompt (optional alternative; existing BlockPowerIcon was reused):
"A sturdy angular steel shield accumulating layered icy-blue energy along its
rim, releasing a bright blue-white impact burst as it blocks an incoming strike.
Centered shield with a clear silhouette, three-quarter view, dark charcoal and
brown textured background, silver metal and cool blue palette, strong highlights,
faceted painterly fantasy game icon matching BlockPowerIcon, readable at small
skill-tree sizes. No text, letters, numbers, logos, or watermark."

Verification: `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal -m:1
-p:CustomBeforeMicrosoftCommonTargets=C:\UnityProjects\SkillTree\Temp\BlockPowerMomentumCompile.targets`
completed with zero errors and 19 existing dependency-resolution/TutorialEventAdapter
warnings. The temporary target includes both new sources and the existing untracked
BleedGrantsFortificationChance source without changing generated project files.
Script GUID/asset reference, localization ID pairing, existing blockPower term,
icon sliced-sprite fileID and scoped diff whitespace were checked. No tests, player
build or runtime playtest requested or performed. Status-icon appearance in live
combat remains unverified.

## Bleed applications can grant Fortification (2026-10-12)

`BleedGrantsFortificationChance` uses `DelegateModifierRuntimeBinding` and the
accepted source-side `Unit.OnEffectApplied` event, following
`RepeatAppliedOverchargeChance`. It filters for `Bleed` and rolls independently
per modifier copy. Chance is `clamp01(powerContext.Scale(chance))`; the authored
asset has base chance 0.2. A success grants the owner exactly one existing
`FortificationEffect`, using the producer-side cap pattern from
`BarrierRestorationGrantsFortification`. No stack is added or refreshed at thirty
active stacks. Each stack reduces all incoming damage by 1%, has no time limit and is consumed
five at a time on positive damage receipt. Power scales chance, not stack strength,
duration or stack count. Binding removal unsubscribes the handler.

Rejected/absorbed effects and unattributed applications do not trigger this
modifier. `AddRepeatedEffect` is deliberately silent and does not trigger it.
Bleed ticks do not count as applications. Normal attack applications occur before
block, so a blocked attack can grant a stack if its Bleed was accepted. Redirected
Bleed still credits its source. The granted self-buff has no source attribution
and cannot feed back into source-side effect reactions.

Code: `Assets/Scripts/SkillTree/Modifiers/ReactMods/BleedGrantsFortificationChance.cs`.
Asset: `Assets/Scripts/SkillTree/Modifiers/ReactMods/BleedGrantsFortificationChance_0.2.asset`.
Added shared/English key in table `Modifiers`:
`modifier.bleedGrantsFortificationChance.description` =
`When you apply {bleed|Bleed}, you have a [[0]]% chance to gain 1 stack of {fortification|Fortification}.`
Russian/German translations remain translator TODOs; neither table was changed.
No new effect display-name entry is needed: existing `Descriptions` key
`effect.fortification.name` and `EffectVisualType.Fortification` are reused.
Existing registered `bleed` and `fortification` terms resolve to their description
assets; no tooltip assets or database entries were changed. The modifier description
has an English fallback. Existing Fortification status presentation is reused;
node assignment and modifier icon mapping remain manual Unity Editor work.

Verification: `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal -m:1
-p:CustomBeforeMicrosoftCommonTargets=C:\UnityProjects\SkillTree\Temp\BleedFortificationCompile.targets`
passed with zero errors and 19 existing dependency-resolution/TutorialEventAdapter
warnings. The temporary target explicitly includes the new source in gameplay
compilation without editing the generated project file. Script GUID/asset reference,
chance, localization ID pairing and existing term references were checked.
Scoped `git diff --check` passed. No tests, player build or runtime playtest ran.

## Attack modifier container per target Bleed (2026-10-12)

`BleedPowerForBleedStacks` now exposes an inline `ModifierContainer`, following
the container scaling/description pattern of `ModifierContainerPerWisp` while
retaining the existing class and script GUID. It always runs in `OnAttack`, after
evasion and OnHit callbacks, before critical calculation and new ailment application.
It counts the target's existing Bleed instances and adds one contribution to the
current attack snapshot: container value times source power multiplier times count.
More is one scaled factor, not one independent factor per Bleed. Copies add their
own contributions; the owner and shared container are not mutated. There is no
persistent buff, duration, runtime binding, new event or status icon. Missing
configuration/target or zero Bleeds adds nothing.

Asset: `Assets/Scripts/SkillTree/Modifiers/SpecialMods/BleedPowerForBleedStacks_0.02.asset`.
Its new container preserves Added BleedPower = 0.02 and existing asset identity.
New assets use the same default. The Editor creation menu is
`Modifiers/Special/Modifier Container Per Target Bleed`.
Only stats consumed by the attack snapshot affect that attack (damage groups,
critical/ailment stats and other supported attack stats). Choosing AttackSpeed,
Maximum Health or attributes here does not grant persistent stats or rerun
attribute conversion. Configure the container accordingly.

Added shared and English entries in the `Modifiers` table:
- `modifier.modifierContainerPerTargetBleed.description`: `For this attack, gain '[[0]]' per {bleed|Bleed} on the target`
- `modifier.modifierContainerPerTargetBleed.unconfigured`: `Requires a stat modifier`

Russian/German translations are translator TODOs; existing translations are
preserved. No effect display-name key is needed. The existing `bleed` glossary
term is reused; no tooltip asset or database configuration was changed. Configure
containers, node assignments and icon mapping manually in Unity. Icon prompt
uses the inspected `Assets/Sprites/Icons/BleedIcon.png` style: painterly glowing
crimson wounds and drops on a dark burgundy background.

Verification: `dotnet build Assembly-CSharp.csproj --no-restore -v:diag -m:1`
passed with zero errors and 19 warnings in dependency resolution and existing
`TutorialEventAdapter` code. The initial default parallel build returned failure
without diagnostics; serial compilation completed. Scoped `git diff --check`
passed. Script GUID, enum values and shared/English localization IDs were checked.
No tests, player build or Unity runtime playtest were performed.

## Enemy kills restore Barrier (2026-10-04)

`EnemyKillRestoresBarrier` follows `BlockRestoresBarrier` and binds to the owner's
`Unit.OnEnemyKilled` with symmetric delegate subscription cleanup. Each copy
restores exactly one charge through `Barrier.Restore(1)`, bounded by the maximum,
without power scaling or resetting regeneration progress. Normal restoration
reactions can grant additional charges. No charge is gained at zero capacity.

`EnemyUnit.Death` notifies its injected player target after the normal death
notification/deactivation and before experience delivery. This credits enemy deaths
to the player, including DoT, rather than tracking the final damage source. Health's
existing death guard prevents repeated notifications until combat state reset.
Inactive owners do not restore charges. Removing/recalculating the modifier
unsubscribes its runtime binding. No persistent effect or new status icon is used.

Asset: `Assets/Scripts/SkillTree/Modifiers/ReactMods/EnemyKillRestoresBarrier.asset`.
Modifiers table key: `modifier.enemyKillRestoresBarrier.description`.
English: `Killing an enemy restores 1 {barrier|Barrier}`.
Russian/German translations remain translator TODOs; no entries were overwritten
or added to those locale tables. Node assignment, icon mapping and optional glossary
configuration remain manual Editor steps. Source inspected; gameplay not playtested.
Verification: Visual Studio Roslyn `csc.exe @Temp/EnemyKillModifierCompile.rsp`
compiled gameplay sources against installed Unity/project DLL references with zero
errors. Existing field-assignment warnings were emitted; no warning pointed to the
new modifier. Script GUID/asset reference and shared/English localization ID pairing
were checked. No tests, player build or runtime playtest were performed.

[Home](../Home.md) · [Project map](../ProjectMap.md) · [Combat and effects](CombatAndEffects.md)

Status: **source-reviewed, 2026-09-27**. This page covers calculation and ownership
contracts, not an exhaustive catalog of every special modifier. No Unity playtest
was performed for this documentation pass.

## From a build to a stat

[Tree nodes](SkillTree.md) and [gems](Gems.md) contribute collected modifiers to
`PlayerUnit`. `Unit` combines them with innate and external modifiers, rebuilds its
stat buckets, evaluates calculation phases and caches the resulting values.
Combat reads those values and makes a separate snapshot for each attack.

| Type | Responsibility |
| --- | --- |
| `StatType` | Serialized identity of a stat; inspect the enum before interpreting asset numbers |
| `ModifierContainer` | One stat, modifier type and raw value; declared in `Modifier.cs` |
| `Modifier` / `BaseModifier` | Behavior asset / ordinary numeric modifier |
| `CollectedModifier` | A modifier paired with its source's power context |
| `BaseUnitModifiers` | Mutable modifier buckets and cached stat values |
| `StatCalculator` | Calculation phases, aggregate-stat expansion and final arithmetic |
| `Attributes` | Authored and runtime rules translating attributes into other modifiers |
| `IModifierRuntimeBinding` | Per-owner subscriptions and other reactive runtime setup |

## Numeric combination

For a scalar stat, `StatCalculator.GetStat` calculates:

```text
value = sum(Added) × (1 + sum(Increased)) × product(1 + each More)
```

The `Added` bucket includes the base contributions supplied by innate configuration.
For example, `Added = 100`, two `Increased` contributions of `0.20` and `0.30`,
and two `More` contributions of `0.10` and `0.20` produce `198`.
These numbers illustrate arithmetic, not approved balance values.

`ChangeModifierValue` accumulates contributions; `SetModifierValue` replaces the
selected bucket. Confusing these operations can erase another modifier's work.
Changing a bucket is also distinct from updating its cached value.

This formula is not a universal clamp or unit conversion. Individual mechanics
apply their own bounds. `WispStats.Normalize` floors the final nonnegative total
for Steel/Ash/Frost/Storm wisp counts, allowing fractional contributions to combine
before rounding. Type masks require their own interpretation and must not be
treated as scalar percentages.

## Full recalculation order

`Unit.RecalculateMods` first unbinds old runtimes, resets the stat/attribute state,
and applies its base and unit-specific innate configurations. It collects runtime
innate modifiers, player-tree modifiers when applicable, and outer modifiers.
`StatCalculator.RecalculateStats` then executes:

1. Applicable low-life-threshold modifiers.
2. `PreAttribute`, then `PreAttribute2` modifiers.
3. Expand `AllAttributes`, run `AttributeFinalization`, then apply attribute scaling rules.
4. `Secondary`, then `Special` modifiers.
5. Expand damage, mitigation, defence and ailment groups.
6. Cache final values.

Finally the unit binds the collected runtime bindings and emits
`OnStatsRecalculated`. Phase order is explicit in the calculator: the numeric order
of `ModifierPriority` enum members is not the execution order.

AttributeFinalization (appended ID 8) is the redistribution boundary. The modifier
reads rounded final individual totals including AllAttributes, replaces their
scalar buckets with the distributed results and consumes the already-applied
multipliers. No serialized priority migration is needed for the existing definition.

StatType IDs 10 and 84 and EnemyIntelFeatureType ID 9 are retired after Poison
removal; do not reuse them. Other explicit IDs are preserved.

`RequestModRecalculation` normally defers work to the combat `Mods` phase. When the
unit is unregistered, has no tick system or combat is paused, it recalculates
immediately. Do not assume that every change event completes recalculation in its
own callback, or that recalculation always waits until another frame.

As of 2026-10-03, PlayerUnit listens to MainSkillTree.OnActiveModifiersChanged instead
of the broader persistence event. Normal tree operations consolidate that notification
at the end of the synchronous operation, including nested zone/power changes. Queue-only
edits do not request a stat rebuild. Paused combat still recalculates synchronously at
that boundary; registered running combat still consumes requests in Mods. Arithmetic,
phase order, runtime binding rebuilds and attack snapshots are unchanged. The
Battle.RecalculateUnitModifiers profiler scope measures the full rebuild including
its stat observers. See the [tree implementation](SkillTree.md#allocation-optimization-implementation-2026-10-03).

## Aggregate stats

Selected expansion rules in the current calculator:

| Group | Expanded into |
| --- | --- |
| `AllAttributes` | Strength, Dexterity, Intelligence |
| `Damage` | Corresponding stat for each damage type |
| `ElementalDamage` | Fire, Cold, Lightning damage |
| `MysticDamage` | Light and Darkness damage |
| `Defence` | Armor, Evasion, BarrierCapacity, BlockChance |
| `AilmentChance` / `AilmentPower` | Ignite, Chill, Overcharge and Bleed chance/power |
| `AilmentGuard` | IgniteMitigation, ChillDurationReduction, OverchargeAvoidanceChance, BleedMitigation |

The calculator clears these aggregate buckets after merging them. They are inputs
to concrete stats, not independent damage/defence resources. Adding a new enum
member alone does not wire it into these groups, display rules or combat.

For the formulas consuming these stats, read [defences and resources](DefencesAndResources.md)
and [ailments and debuffs](AilmentsAndDebuffs.md). In particular, ailment chance
stats and flat debuff chances have different meanings; modifier arithmetic alone
does not define the application probability.

## Power and reactive behavior

`CollectedModifier` carries `ModifierPowerContext` into application, runtime
binding and description code. Ordinary nodes scale by power; infinite nodes scale
by invested points. See [tree power](SkillTree.md) for the rules and exclusions.
When implementing a modifier, keep its gameplay value and description consistent.

Reactive modifiers return a binding with symmetric `Bind` and `Unbind` operations.
`Unit` unbinds old subscriptions before rebuilding and on destruction. Store
owner-specific callback state in the runtime binding rather than shared asset
fields. Otherwise shared definitions can leak state between owners or copies.

`RepeatAppliedOverchargeChance` demonstrates a powered chance captured per binding,
source-event filtering and a repeated effect without recursive application events.
Read the [effect event contract](../Reference/EffectApplicationEvents.md) for the exact API.
Runtime binding creation is separate from phase application; implement the
conditions needed by the binding itself rather than assuming the phase loop filters it.

## Conditional and Wisp content clarification (2026-10-07)

Source inspection for the [Big-node design proposals](../../Design/BigNodes50.md)
confirmed that `HasBarrierModifier.IsApplicable` reads `Barrier.HasBarrier`, whose
condition is **maximum charges greater than zero**, not current charges greater
than zero. Spending the last current charge does not turn this condition off.
The existing description's "While Barrier is active" wording can obscure this
distinction. A current-charge, full-barrier or empty-barrier condition needs a
different predicate; no runtime or localization change was made in this review.

`FullLifeModifier` delegates to `Unit.IsOnFullLife`, which checks a current-health
fraction strictly greater than `0.999`. Its reversed condition means not Full Life,
not Low Life. New design copy uses the existing Full Life terminology.

Live Big-node inspection also found `IgnitePowerPerAshWisp`,
`ChillPowerPerFrostWisp` and `OverchargePowerPerStormWisp` configured as Increased
power modifiers. Increased for these power stats is forbidden by the current
[filling guide](../SkillTree/SkillTreeFillingGuide.md). Their presence is a content
discrepancy, not permission to author more; the proposals use Added power instead.
Existing assets were not changed. `ModifierContainerPerWisp` scales a configured
non-Wisp container by a normalized counter and has no inherent reward cap.

These statements distinguish inspected source and live serialized configuration
from gameplay verification. No playtest, test runner or build was performed.

## Attack-local modification

An attack modifier must change `DamageInfo.BaseUnitModifiers`, the attack snapshot,
rather than `DamageInfo.Owner.BaseUnitModifiers`. The attack processor includes an
Editor diagnostic hash check for unintended mutation of the attacker's persistent
buckets. Both hash computation and assertion calls are Editor-only; legitimate
synchronous stat rebuilds are excluded through a unit revision. Reused phase
buffers and the tree collection cache preserve independent snapshots and observe
changes between phases. See the [fix record](../Reference/StatTypeFixes20261017.md)
and [combat order](CombatAndEffects.md) before choosing a phase.

`LightRecalculateAttackStats` expands damage and ailment groups and refreshes damage
and its explicit list of attack stats. It does not rerun the full attribute/defence
pipeline. A new conditional modifier must target a stat actually consumed and
recalculated in that phase; merely adding a bucket value is insufficient.

## Authoring and verification

For skill-tree content, the [filling guide](../SkillTree/SkillTreeFillingGuide.md)
remains authoritative for allowed pairs, small-node values and large-node budgets.
Runtime support does not itself authorize a stat for new content. In particular,
the presence of Freeze code does not change the guide's restriction on using it
as a tree-content stat.

When changing this system, check arithmetic with multiple contributions, powered
descriptions, repeated recalculation without duplicate subscriptions, removal of
temporary modifiers, and isolation between attacks and between units. Full Unity
compilation and the affected play scenario are separate from stub-based tests.

## Source entry points

Repository-relative paths, outside this vault:

- `Assets/Scripts/StatCalculator.cs`
- `Assets/Scripts/StatType.cs`
- `Assets/Scripts/ModifierPriority.cs`
- `Assets/Scripts/WispStats.cs`
- `Assets/Scripts/Battle/BaseUnitModifiers.cs`
- `Assets/Scripts/Battle/Attributes.cs`
- `Assets/Scripts/Battle/Unit.cs`
- `Assets/Scripts/SkillTree/Modifiers/Modifier.cs`
- `Assets/Scripts/SkillTree/Modifiers/BaseModifiers/BaseModifier.cs`
- `Assets/Scripts/SkillTree/Modifiers/ModifierRuntimeBinding.cs`
- `Assets/Scripts/SkillTree/Modifiers/ReactMods/RepeatAppliedOverchargeChance.cs`

## Barrier sacrifice attacks

Source change, 2026-10-03: BarrierSacrificeAttackModifier uses a per-binding
completion counter (default interval 3), a maximum barrier spend (default 3), and a
powered attack-only ModifierContainer (default More ElementalDamage 0.25).
The normal attacker emits Unit.OnAttackPrepared after snapshot creation and
before evasion. The modifier consumes min(maxBarriersConsumed, available charges)
through Barrier.TryConsume and modifies only that snapshot. It adds one container
whose powered value is multiplied by the number spent, rather than separate More
factors. Three charges at 0.25 grant one 0.75 More ElementalDamage contribution
(1.75 multiplier), not three 1.25 factors. Misses spend the charges and discard the bonus;
completed attacks, including extra attacks and misses, advance the counter.
Zero charges skip the reward without postponing the cycle; any positive count up to the limit is spent. Combat reset
and runtime rebinding restart the counter. Copies have independent counters and
pay separately in subscription order. Direct processor calls do not emit the
normal attacker preparation/completion events. No separate status effect is used.

Consumption emits OnBarrierCountChanged and OnBarriersLost(spent), enabling
Barrier Surge stacks, and preserves regeneration progress. There is no distinct enemy-destruction event
in the inspected Barrier API; reactions requiring damage receipt or enemy source
are not synthesized. The default modifier asset is supplied beside its script at
`Assets/Scripts/SkillTree/Modifiers/ReactMods/BarrierSacrificeAttackModifier.asset`.
Node assignment, icons and tooltips remain manual Unity Editor work.
Only attack-recalculated stats are supported by
the configurable container; its default elemental damage is supported.

The former serialized barrierCost field migrates to maxBarriersConsumed with
FormerlySerializedAs; other assets retain their previous numeric limit. The supplied
modifier asset is explicitly configured for a limit of 3. Modifiers localization
keys modifier.barrierSacrificeAttack.description and .unconfigured now have shared
ID records matching their existing English entries. ru/de translations remain TODO.

Verification of the revised sacrifice rule (2026-10-03): MSBuild.exe
Assembly-CSharp.csproj /t:Build /p:Configuration=Debug completed with exit code 0.
The generated source path for the previously renamed Barrier Surge producer was
temporarily adjusted and restored. Existing dependency/API/analyzer warnings
remain; no tests or Unity playtest were run.

## Offensive and defensive stance (2026-10-03)

OffensiveDefensiveStanceModifier uses the cyclic indicator pattern, with passive
Special-phase stat contributions and a persistent OffensiveDefensiveStanceEffect.
The supplied asset is Assets/Scripts/SkillTree/Modifiers/SpecialMods/OffensiveDefensiveStance.asset.
It defaults to 3 completed attacks, 3 received attacks, 0.15 Increased Damage and
0.20 Increased Armor/Evasion. Thresholds are configurable and clamped to at least
one; node power scales bonuses only. Offence and Defence bonuses are exclusive.

OnAttackCompleted advances Offence, including misses and extra attacks. OnGettingHit
advances Defence, including blocked, zero-HP-loss and absorbed attacks; evasion and
DoT do not advance it. This follows inspected Attacker, AttackProcessor and Unit
receipt code. Transition requests are deferred until after the current attack;
stat rebuilding follows the normal Mods scheduling contract. Effect counters persist
through stat/runtime rebinding. ResetCombatState clears the old effect and the reset
binding restores Offence with zero progress and requests a stat rebuild. Removing
the source removes its stat contribution at recalculation; its indicator unsubscribes
when the Effects phase removes it. Different assets have independent cycles; repeated
copies of the same asset share one counter and their powered bonuses add.

EffectVisualType.OffensiveStance (24) and DefensiveStance (25) use one changing
status icon per source asset. Text shows completed count/threshold, and the existing
border stays fully filled in both stances, independently of the counter. Until manual icon mapping, EffectIconsConfig
uses its default icon. Tooltip strings have explicit English fallbacks. No node,
scene, prefab, icon mapping or tooltip database configuration was changed.

### Localization added

Modifiers table, modifier.offensiveDefensiveStance.description:
> Start combat in Offence: [[0]]% increased Damage. After [[1]] completed attacks, including misses, switch to Defence: [[2]]% increased Armor and Evasion instead. After [[3]] incoming attacks that are not evaded, return to Offence. Blocked and fully absorbed attacks count.

Descriptions table (GameLocalization.ContentTable):
- effect.offensiveStance.name: Offence
- effect.defensiveStance.name: Defence
- effect.offensiveStance.description: [[0]]% increased Damage. Completed attacks: [[1]]/[[2]], including misses. At [[2]], switch to Defence.
- effect.defensiveStance.description: [[0]]% increased Armor and Evasion. Incoming attacks: [[1]]/[[2]]. Evades do not count; blocked and fully absorbed attacks count. At [[2]], return to Offence.

English entries and shared key/ID records were added. All five keys are translator
TODOs for ru/de; no translations or empty entries were added. These plain-text
status descriptions do not require tooltip term records; linked node glossary
entries, if desired, remain manual.
Verification: MSBuild.exe Assembly-CSharp.csproj /t:Build /p:Configuration=Debug
was attempted with temporary new-source includes and a correction for a stale
Barrier Surge source path; the generated project was restored afterward. Compilation
did not reach C#: generated ProjectReference metadata failed with MSB3107 for
Assembly-CSharp-firstpass and Zenject projects. Compilation and gameplay remain
unverified. No tests or player build were run. Script GUID/asset settings and
localization key/ID pairing were checked directly.

## Deferred attack damage (2026-10-03)

DeferredAttackDamageModifier uses the DamageTakenAsPain delegate-binding pattern
and a separate DamageDebtEffect through the new pre-HP damage stage. See the
[combat contract](CombatAndEffects.md#deferred-attack-hp-damage-2026-10-03)
for timing, power, multiple copies, payments and cleanup. Default settings:
`deferredFraction = 0.3`, `debtDuration = 5` seconds. Duration is configurable.

### Localization added

Modifiers table, `modifier.deferredAttackDamage.description`:
> [[0]]% of attack damage that would remove Health after defences becomes a debt, paid evenly over [[1]] seconds. Each hit has its own deadline. Damage over Time and mystic absorption are not deferred. Debt payments bypass defences and cannot create new debt.

Descriptions table (GameLocalization.ContentTable), `effect.damageDebt.name`:
> Damage Debt

Descriptions table, `effect.damageDebt.description`:
> Deferred attack damage is paid evenly as Health loss. Each debt has its own deadline; new hits do not extend it. Payments bypass defences and cannot create new debt. The icon shows total remaining damage.

English entries and corresponding shared key/ID records were added. All three
keys remain translator TODOs for ru/de. No translations or empty entries were
added. English fallbacks keep the modifier and effect readable without glossary
configuration. Effect descriptions contain no linked terms requiring database entries.

### Manual Unity Editor Steps

1. Use the supplied `Assets/Scripts/SkillTree/Modifiers/ReactMods/DeferredAttackDamage.asset`;
   its own definition is already created, configured and linked to the script GUID.
   Adjust Deferred Fraction or Debt Duration on this asset if desired.
2. Assign that asset to the intended ordinary node's modifier list. No node or
   other gameplay configuration was changed, and the mechanic is not yet wired into a build.
3. Assign a sprite for EffectVisualType.DamageDebt (26) in
   `Assets/Scripts/Visual/EffectIconsConfig.asset`, and assign the intended node icon
   through its existing visual configuration. Until mapping, the status uses the
   configured default icon, or no sprite when that default is absent; its number
   and English tooltip use the existing view.
4. Optional glossary entry: register `damageDebt` in the active tooltip database
   only if a linked glossary is desired; the plain effect tooltip already works.
   Translate the three exact keys above through the normal translation workflow.

### Icon Generation Prompt

Proposed style; no visual icon reference was inspected:

Create a square dark-fantasy RPG skill icon: a single cracked crimson heart held
inside an antique iron hourglass, with glowing red droplets falling steadily from
its upper chamber into the lower chamber. The heart and hourglass form one bold,
centered silhouette, showing injury paid gradually over time. Deep burgundy and
charcoal palette, restrained warm amber highlights on the iron frame, dramatic
rim lighting, subtle smoky dark background, painterly detail with clear shapes
and strong contrast readable at small skill-tree and status-icon sizes. No text,
letters, numbers, logos, borders, or watermarks.

No image was generated or imported. Compilation is recorded on the linked combat
page; runtime and visual verification are pending.

## Bleed heals instead of damage (2026-10-04)

`BleedHealsInsteadOfDamage` is a passive marker modifier in
`Assets/Scripts/SkillTree/Modifiers/SpecialMods`. It follows the collected-source
lookup used by `AilmentAbsorption`; there is no runtime binding, separate effect,
new event or enum. A collected applicable copy enables conversion for its owner.
Multiple copies and node power do not multiply the conversion.

When a Bleed is created on an owner with this modifier, `CalculateTotalDamage`
uses zero Bleed Mitigation, including payload-redirection recipients. The initial
pool remains a snapshot: acquiring or removing the modifier does not recalculate
existing pools. Each tick and burst checks the recipient's currently collected
modifiers and consumes the normal remaining pool, calling `Health.TakeHeal`
instead of `ReceiveDoT` while conversion is enabled. Existing healing-received
scaling and maximum Health apply; excess healing is consumed, not saved. Removing
the modifier makes subsequent payments deal damage from the same remaining pool.
Merge retains its existing sum, multiplier and refreshed duration. Ailment
absorption still prevents application before this conversion can operate.

Bleed keeps its current status icon and remaining-pool number; no extra status
icon or effect display-name key is introduced. The plain modifier description has
an English fallback and requires no new glossary record.

Localization added to **Modifiers** and its shared key table:
`modifier.bleedHealsInsteadOfDamage.description` =
`Bleed on you restores Health instead of dealing Damage.`
The description intentionally omits mitigation details. ru/de translations are
TODOs; no translated or empty entries were added.

### Manual Unity Editor Steps

Use the supplied
`Assets/Scripts/SkillTree/Modifiers/SpecialMods/BleedHealsInsteadOfDamage.asset`.
Its script GUID is configured and it has no numeric settings. Assign it to the
intended ordinary node's modifier list and configure the node icon through the
existing visual setup. Infinite nodes do not support this special mechanic.
No nodes, icons, tooltip databases, scenes or prefabs were edited for this change.
No new tooltip record is required by the plain description. Translate the exact
key above into ru/de through the normal translation workflow.

### Icon Generation Prompt

Proposed style; no visual icon reference was inspected:

Create a square dark-fantasy RPG skill icon showing three crimson blood droplets
flowing into a wounded heart and sealing its crack with soft emerald light. A
single bold heart silhouette at the center, droplets clearly entering from above,
a subtle warm glow where the wound closes. Deep burgundy, charcoal and restrained
emerald palette, dramatic rim lighting, dark smoky background, painterly detail
with strong contrast and simple shapes readable at small skill-tree icon sizes.
No text, letters, numbers, logos, borders or watermarks.

No image was generated or imported.

Verification: Visual Studio Roslyn
`csc.exe @Temp/BleedHealingModifierCompile.rsp` compiled gameplay sources against
the installed Unity/project references with exit code 0. Existing field-assignment
warnings remain. Script GUID/asset reference and shared/English key ID were
inspected. No tests, player build, gameplay or visual playtest were run.

## Fortification from restored Barrier (updated 2026-10-13)

`BarrierRestorationGrantsFortification` retains its restoration delegate binding.
Each actual restored charge, including bonus restoration, adds the configured
`stacksPerRestoration` independent Fortification stacks (default 1, nonnegative
integer). The grant count does not scale with node power. RestoreFull/combat reset
is not a restoration trigger.
Restoration, Evade and Bleed producers share the thirty-stack cap.

Fortification grants one aggregate Added DamageMitigation contribution of 1% per stack,
owned by FortificationController and updated through deferred stat recalculation.
The ordinary per-type mitigation stage reduces incoming attack damage before
ailment generation and block. Existing DoT pools are unchanged. In `Unit.ReceiveDamage`, its helper
reads live effects and removes up to five stacks on positive attack damage,
respecting the minimum stack floor. Zero-damage/fully blocked attacks and evasion
consume nothing; positive attack damage absorbed by Barrier consumes stacks.
`ReceiveDoT` and deferred damage-debt payments do not consume stacks.

Stacks have no time limit. Removing a producer leaves earned stacks; ordinary
combat reset/effect clearing removes them. Node power does not scale stack
strength or cap. EffectVisualType.Fortification = 27 and grouped stack text remain;
there is no expiry timer and no new status-icon configuration.

Existing definition (unchanged):
`Assets/Scripts/SkillTree/Modifiers/ReactMods/BarrierRestorationGrantsFortification.asset`.
Existing glossary registration points to `Descriptions/Fortification.asset` and
uses Descriptions keys. On 2026-10-13 the tooltip asset was split into three
separate `descriptions` entries so the UI creates separate paragraph blocks:
Current English text for `effect.fortification.reduction` = `Each stack grants 1% Damage Mitigation.`,
`effect.fortification.limit` = `Maximum 30 stacks.`, and
`effect.fortification.consumption` =
`Receiving an attack removes up to 5 stacks, without going below your minimum.`
The shared/English entries were saved through Unity API; ru/de are translator
TODOs. TooltipTerms' existing fortification registration was maintained and its
lookup was verified against the same tooltip asset. The full description key
remains available for code fallback. This asset-only change required no compilation.

Historical English `Descriptions` / `effect.fortification.description` from
2026-10-13, superseded by the split entries and current code fallback:

> Each stack reduces incoming damage by 1%.
>
> Maximum 30 stacks.
>
> Taking damage removes 5 stacks after applying their damage reduction.

`effect.fortification.name` remains `Fortification`. The restoration producer's
existing definition now serializes `stacksPerRestoration: 1`. English `Modifiers` /
`modifier.barrierRestorationGrantsFortification.description` is
`Each restored {barrier|Barrier} grants [[0]] stacks of {fortification|Fortification}.`
ru/de description translation remains a translator TODO;
no existing translations were overwritten and no empty entries were added.

Icon Generation Prompt (proposed style, existing icon unchanged):
Create a square dark-fantasy RPG icon showing a steel shield built from layered
armor plates, one outer plate breaking away under an incoming impact while the
remaining shield holds firm. Centered bold silhouette, silver steel and cold
azure protective glow, dramatic rim lighting, dark smoky background, painterly
detail and strong contrast readable at small sizes. No text, letters, numbers,
logos or watermark.

Manual Unity Editor Steps: set Stacks Per Restoration on the existing definition
to the desired count. Existing node links, glossary term and icon mappings remain usable.
Verification: `unity command recompile` and `recompile_status` targeting
`C:/UnityProjects/SkillTree` completed with no compilation errors on 2026-10-13.
The English table was saved through Unity API with the revised three-paragraph
description. Scoped source/diff and existing glossary registration were inspected.
No tests, player build or runtime playtest were performed.

## Lowest elemental damage penetration (2026-10-04)

`LowestElementalDamagePenetratesResistance` follows the attack-local payload pattern
of `DexterityElementalResistanceBypass`. OnAttack enables a boolean in the attack
payload; Resistance selects the two lowest amounts among Fire, Cold and Lightning
immediately before resistance mitigation, after attack damage calculation and
IncomingPreMitigation modifiers. Armor only changes Physical damage.
The selected elements fully bypass their own positive capped resistances, preserving
negative resistance bonuses. Shared Elemental Resistance and its ordinary penetration
remain unchanged. Exactly two elements are selected, including zero-damage elements.
Ties leave the first highest element in Fire, Cold, Lightning order unselected.
Multiple copies are idempotent; node power does not scale this binary rule.
The payload resets between attacks. No runtime binding, persistent effect, status
icon, new event or enum value is required. Direct DoT does not use this attack rule.

Asset: `Assets/Scripts/SkillTree/Modifiers/SpecialMods/LowestElementalDamagePenetratesResistance.asset`.
Modifiers key: `modifier.lowestElementalDamagePenetratesResistance.description`.
English: `The two lowest-damage elements in each attack fully penetrate their Resistances.`
Shared/English entries added; Russian/German translations remain translator TODOs.
Node assignment and node icon mapping remain manual Unity Editor steps; the modifier
has no numeric settings. The description has an explicit English fallback.

Icon Generation Prompt (proposed style; no icon reference inspected):
Create a square dark-fantasy RPG skill icon: two slender luminous spears, one icy
cyan and one electric violet, piercing matching translucent shields, beneath a
large amber flame. Centered compact composition, clear silhouettes, painterly
metal and crystal textures, dramatic rim lighting, dark charcoal background,
strong contrast readable at small sizes. No text, letters, numbers, logos or watermarks.

Verification: Visual Studio Roslyn `csc.exe @Temp/LowestElementalCompile.rsp`
compiled gameplay sources against installed Unity/project DLL references with exit
code 0. Existing field-assignment warnings remain; none reference the new modifier.
Script GUID matches the asset reference; shared and English localization IDs match.
No tests, player build or runtime playtest were performed.

## Modifier container per any Wisp (2026-10-04)

`ModifierContainerPerAnyWisp` follows `ModifierContainerPerWisp` in `PreAttribute2`,
reading current buckets through `StatCalculator.GetStat` rather than cached values.
It sums the individually normalized Steel, Ash, Frost and Storm Wisp counts and
adds the referenced `BaseModifier.modifierContainer`, scaled by source power and
that total. More is a single scaled contribution, not one factor per Wisp.
The referenced asset supplies the container only; its priorities are not executed.
Missing references, empty stats and Wisp reward stats disable the modifier and
show an explicit English configuration message. No runtime binding, persistent
effect, status icon, event or enum change is needed. Ordinary stat rebuilds update
this bonus. Copies contribute independently; the referenced asset is not mutated.

Script and initially unconfigured asset:
`Assets/Scripts/SkillTree/Modifiers/SpecialMods/ModifierContainerPerAnyWisp.cs` and
`ModifierContainerPerAnyWisp.asset` in the same directory. Select a Base Modifier,
assign to nodes, and configure node icons/optional tooltips manually in Unity.

Added shared and English entries in the `Modifiers` table:
- `modifier.modifierContainerPerAnyWisp.description`: `Adds '[[0]]' for each Wisp you have, counting all Wisp types`
- `modifier.modifierContainerPerAnyWisp.unconfigured`: `Requires a Base Modifier with a non-Wisp stat`

Russian/German translations are translator TODOs; their tables were not changed.
No separate effect name is required. Icon style is proposed, without inspected art
references: four differently colored Wisps converging into one luminous core.
Source inspected; no tests, player build or gameplay playtest performed.

Verification: Visual Studio Roslyn `csc.exe @Temp/AnyWispCompile.rsp` completed
with exit code 0 against installed Unity/project references. Existing field-assignment
warnings remain; no compiler error. Unity runtime behavior remains unverified.

## Block restores Barrier proc notification (2026-10-07)

`BlockRestoresBarrier` retains its per-owner OnBlock delegate binding. After
`Barrier.Restore(1)` returns a positive restored count, it calls
`Unit.NotifyModifierProc(procIcon)`. Full or zero-capacity barriers do not notify.
Power scaling, restoration reactions and subscription cleanup remain unchanged.
Multiple copies can each notify if their own restoration succeeds.

`Unit.OnModifierProc(Sprite)` forwards the request to UnitVisual's existing
[floating icon notification](HudAndTooltips.md#floating-proc-icons-2026-10-07).
UnitVisual subscribes in Awake, unsubscribes on destruction, and queues requests
while disabled, resuming display when enabled. A missing sprite suppresses the visual without affecting restoration.
No separate effect or status icon is created.

The asset `Assets/Scripts/SkillTree/Modifiers/ReactMods/BlockRestoresBarrier.asset`
now assigns `Assets/Sprites/Icons/BlockrestoresBarrier.png` to Proc Icon. Change
this field on the modifier asset to customize the flying sprite; no node wiring
or tooltip changes are required for existing users of this modifier.
The existing Modifiers key `modifier.blockRestoresBarrier.description` retains
its English text: `After {block|Block}: restore 1 {barrier|Barrier}`.
No localization entries or translations were added or changed.
Source and sprite GUID/fileID inspected; Unity runtime appearance is unverified.

Verification: dotnet build Assembly-CSharp.csproj --no-restore -v:q /clp:ErrorsOnly
passed with zero errors and 13 dependency warnings. No tests or Unity playtest run.

## Additional queued modifier proc icons (2026-10-07)

Six reactive modifiers now follow BlockRestoresBarrier's serialized procIcon and
per-owner delegate-binding pattern. Each accepted visual request joins UnitVisual's
FIFO queue with at least 0.2 seconds between icon spawns. No new effects, enum
values, runtime components, combat rules, localization entries or node wiring.
Missing sprites suppress only the visual. Sprite choices are reusable existing art,
not newly generated mechanic-specific illustrations.

| Modifier / asset basename in Assets/Scripts/SkillTree/Modifiers/ReactMods | Notification gate | Assigned sprite in Assets/Sprites/Icons |
| --- | --- | --- |
| EnemyKillRestoresBarrier.asset | active owner and Restore(1) returns positive | BarrierCountIcon.png |
| BarrierRestorationGrantsAdditionalBarrierChance_0.1.asset | chance succeeds and RestoreAdditional(1) returns positive | BarrierRegenerationSpeedIcon.png |
| EvadeRestoresHealth_0.05.asset | HP increases after its ReceiveHeal call | RegenerationIcon.png |
| ParryRestoresHealth_0.1.asset | HP increases after its ReceiveHeal call | HealthIcon.png |
| ReduceTargetAttackProgressOnHit_0.05_0.2.asset | chance succeeds and target AttackProgress decreases | AttackSpeedIcon.png |
| RepeatReceivedEffectChance.asset | chance succeeds and AddRepeatedEffect accepts the copy | RepeatEffectIcon.png |

RepeatReceivedEffectChance does not filter beneficial effects; the proc indicates
successful repetition, including debuffs. Attack progress checks suppress icons
when progress is zero or externally locked. The progress-reduction icon appears on
the modifier owner, matching the shared notification path. Restoration/healing
notification gates preserve the existing event timing and scaling.

Existing Modifiers English keys/text are unchanged (ru/de also unchanged):

| Key | English text |
| --- | --- |
| modifier.enemyKillRestoresBarrier.description | Killing an enemy restores 1 {barrier\|Barrier} |
| modifier.barrierRestorationGrantsAdditionalBarrierChance.description | Each restored {barrier\|Barrier} has a [[0]]% chance to restore 1 additional Barrier, up to your maximum. Additional Barriers cannot trigger this bonus. |
| modifier.evadeRestoresHealth.description | On {evade\|Evade}: restore [[0]]% of Maximum Health. |
| modifier.parryRestoresHealth.description | On {parryChance\|Parry}: restore [[0]]% of Maximum Health. |
| modifier.reduceTargetAttackProgressOnHit.description | On Hit: [[0]]% chance to reduce target Attack Progress by [[1]]% |
| modifier.repeatReceivedEffectChance.description | [[0]]% chance when you receive an {effect\|Effect} to receive it again |

Manual customization: change Proc Icon on each listed modifier asset. Existing
modifier users need no new node, tooltip or status-icon configuration.

Verification: dotnet build Assembly-CSharp.csproj --no-restore -v:q /clp:ErrorsOnly
passed with zero errors and 13 dependency warnings. All six saved procIcon references
and script GUIDs were checked against their metadata. No tests or Unity playtest run.

## Pain received conversion (2026-10-12)

`PainReceived` (ID 94, renamed from PainReceivedMultiplier) is a percent stat
using ordinary Added/Increased/More arithmetic. Added 0.05 means 5% of actual
attack HP loss converted to Pain. Two such contributions with Increased 0.2 yield
0.12. No default base is injected. The owner will remove the previous innate
Added 1 manually; leaving it configured means 100% base conversion.
DamageTakenAsPain definitions were migrated to ordinary Added BaseModifiers with
preserved GUIDs, so node references now supply this stat. Unit.ReceiveDamage
calculates gain once after positive HP loss, retaining low-life doubling and DoT
exclusion. Pain stacking does not scale again. See
[reactive effects](ReactiveCombatEffects.md#pain-received-stat-2026-10-12).
