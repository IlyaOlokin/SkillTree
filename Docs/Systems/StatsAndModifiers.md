# Stats and Modifiers

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
3. Expand `AllAttributes`, then apply attribute scaling rules.
4. `Secondary`, then `Special` modifiers.
5. Expand damage, mitigation, defence and ailment groups.
6. Cache final values.

Finally the unit binds the collected runtime bindings and emits
`OnStatsRecalculated`. Phase order is explicit in the calculator: the numeric order
of `ModifierPriority` enum members is not the execution order.

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

## Attack-local modification

An attack modifier must change `DamageInfo.BaseUnitModifiers`, the attack snapshot,
rather than `DamageInfo.Owner.BaseUnitModifiers`. The attack processor includes an
Editor diagnostic hash check for unintended mutation of the attacker's persistent
buckets. See [combat order](CombatAndEffects.md) before choosing a phase.

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
