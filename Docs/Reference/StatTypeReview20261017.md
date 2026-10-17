# StatType source review — 2026-10-17

[Home](../Home.md) · [Stats and modifiers](../Systems/StatsAndModifiers.md) · [Backlog](../Backlog.md)

Historical pre-fix snapshot. The subsequent owner-authorized
[implementation record](StatTypeFixes20261017.md) supersedes the current-behavior
claims below: reviewed fixes and optimizations were applied, Poison was removed,
and mask semantics were explicitly left unchanged.

Scope: enum identities, numeric storage/calculation, attack snapshots, defence and
ailment consumers, selected dependent modifiers, and stat presentation. This is a
source review of the current working tree, including targeted serialized-reference
checks. No tests, compilation, live Editor commands, playtests or profiling ran.
Examples below are arithmetic traces of inspected code, not executed scenarios.
Gameplay code and serialized content were not changed. The owner's existing
MainScene and untracked modifier edits were preserved.

## How the stats reach gameplay

`StatType` has 97 declared entries, including Empty. Explicit IDs extend through
101; IDs 89–93 are retired. Preserve IDs when reorganizing the enum.

`Unit.ResetUnit` resets buckets and applies innate contributions. Collected tree,
gem and outer modifiers then run through the calculator's explicit phases:
PreAttribute, PreAttribute2, attribute expansion/scaling, Secondary, Special,
aggregate expansion, cached values. Scalar arithmetic is:

`Added sum * (1 + Increased sum) * product(1 + each More)`.

| Family | Main consumer / boundary |
| --- | --- |
| Damage, ElementalDamage, MysticDamage | Expanded into concrete damage buckets; group buckets are cleared |
| Physical, Fire, Cold, Lightning, Light, Darkness damage | DamageCalculator reads the attack snapshot; critical bonus multiplies its typed damage |
| PoisonDamage / PoisonDamageMitigation | Declared stats, but no Poison member in DamageType and no corresponding ordinary attack component; mitigation merging alone does not implement poison |
| Strength, Dexterity, Intelligence, AllAttributes | Attributes translates values into configured modifiers before Secondary/Special |
| AttackSpeed, Accuracy, Evasion | Attack progress and evasion; evasion occurs before OnAttack-priority modifiers |
| CritChance, CritDamageBonus | Snapshot critical roll/layers and damage multiplier |
| Ailment chance/power/guard groups | Expanded into Bleed, Ignite, Chill and Overcharge stats |
| Individual ailment chance/power/protection | Damage-derived chance and effect construction before block; existing DoT pools retain their application values |
| Sunder, Distract, Expose chance/power/mitigation | Separate flat chance rolls and temporary stat penalties; outside ailment group expansion |
| Defence | Armor, Evasion, BarrierCapacity and BlockChance; not every defensive stat |
| Armor, resistance/maxima/penetration, per-type mitigation | Separate mitigation stages; general and specific resistance multiply |
| BlockChance/Power, ParryChance/Power | Successful block and conditional parry; BlockPower is flat damage absorption |
| MaximumHealth, regeneration, HealingReceived, sacred-region percentages | Health values, healing and threshold regions of the same HP resource |
| BarrierCount/Capacity/RegenerationSpeed/TypeMask | Barrier charges, per-charge capacity, regeneration and eligible damage types |
| MysticNegation, ArmorAsMysticNegation, MysticCleansePerSecond | Flat net mystic reduction and signed absorption cleansing |
| LifeSteal/TypeMask | Snapshot bucket arithmetic and eligible post-defence damage |
| Steel/Ash/Frost/StormWisp | Floored nonnegative final counters read by per-Wisp modifiers |
| PainReceived | Actual attack HP loss converted to Pain; not the direct DoT path |
| Minimum/MaximumFortificationStacks | Floored limits; controller enforces bounds and supplies aggregate mitigation |
| AttackProgressOnHitTaken/Miss/AilmentApplied | Signed progress changes at their distinct Unit events |
| ElementalDamageTakenAsPhysicalChance | One conversion roll before physical/elemental mitigation |

AttackSnapshotDirectStats is a partial refresh list, not the complete set of
attack-readable stats. For example LifeSteal evaluates its buckets directly.
Changing an enum or adding a generic container does not automatically make that
stat meaningful in every phase. The reused attack snapshot already avoids
allocating a complete new BaseUnitModifiers for every ordinary attack.

## Correctness findings

1. **BL-003: Chill protection can make Chill permanent.** Chill.cs:23 calculates
   `3 * (1 - ChillDurationReduction)`. At 1.2 this is -0.6 seconds.
   EffectController.CombatTick treats negative TimeLeft as untimed. AilmentGuard
   also feeds this stat. Current assets include direct duration reduction and
   AilmentGuard grants, and node power can amplify scalar grants; the exact
   reachable build above 100% was not established. Define immunity at/above 100%
   and prevent a nonpositive duration from becoming an untimed Chill.

2. **BL-004: physical mitigation accepts unsafe negative armor.** Armor.cs:16
   applies `D * D / (Armor + D)` without the helper's nonnegative clamps.
   D=10 and Armor=-10 divide by zero; Armor=-20 produces -10 physical damage.
   Health.TakeDamage subtracts signed components, so negative damage can increase
   HP. Sunder's Increased Armor penalty scales with its power without a -100%
   floor, providing a source-level route to negative armor if sufficiently strong.
   Actual encounter/build reachability was not established. Reuse one agreed
   safe formula for combat and prediction.

3. **BL-039: attribute redistribution reapplies multipliers.**
   RedistributeAttributesEvenlyModifier.cs:12–43 reads final individual values,
   redistributes their sum, then replaces only Added. Existing Increased/More
   therefore multiply the redistributed results again. With Added STR=100,
   Increased STR=1, and zero DEX/INT, the input sum is 200. The assigned Added
   values are 67/67/66; final values become 134/67/66, totaling 267.
   Its asset runs in PreAttribute2, before AllAttributes expansion, so aggregate
   attribute contributions also miss the redistribution input. The asset is
   referenced by MainScene. Decide the redistribution boundary, include aggregate
   inputs, and prevent repeated application of already-consumed multipliers.

4. **BL-040: damage masks use scalar addition, multiplication and float casts.**
   BaseUnitModifiers.ChangeModifierValue and StatCalculator.GetStat do not
   distinguish masks. UnitInnateStats supplies Physical=1 for both masks. One
   Added 14 grant gives 15 as intended; two give 29, losing Fire and gaining Light
   instead of retaining 15. AddedLifeStealTypeMask_14 is referenced twice in
   MainScene; simultaneous allocation eligibility was not checked. Signed removal
   with -1 works only while the bit is present exactly once. Power scaling already
   exempts masks, but accumulation does not. Use explicit integer grant/removal
   semantics, define precedence for conflicting grants/removals, and exclude
   scalar Increased/More. A simple OR cannot represent the existing removal asset.

5. **BL-041: NoArmor sees a partial defence value.** Its configured asset runs in
   Special, but StatCalculator merges Defence afterward. NoArmorModifier reads
   only Armor, whereas BalancedArmorEvasionModifier explicitly includes the group
   on a temporary copy. With Added Defence > 0 and no direct Armor, the no-armor
   bonus activates even though the final unit has armor. This is a conditional
   source-level case; an attainable current build was not established. Read a
   consistent effective group value without destructively merging it twice.

6. **BL-042: displayed DPS overvalues ordinary crit above 100%.**
   PlayerStatsWindow.cs:190 uses raw CritChance in its expected damage formula.
   Without multi-crit, DamageCalculator awards at most one critical layer. At
   150% chance and +100% crit bonus, the panel uses 2.5x while ordinary attacks
   average 2x before other mechanics. This is a panel arithmetic mismatch, not a
   claim that the simplified DPS should include every combat effect. A separate
   display boundary divides BarrierCooldown by regeneration speed without handling
   zero/negative values. Decide how to show disabled regeneration explicitly.

## Optimization priorities

These are inspected sources of work/allocation, not measured current bottlenecks.
They refine BL-037 and do not attribute its historical capture to a specific cause.

1. **Remove player-build diagnostic work first.** AttackProcessor.cs:34 computes
   the before-hash unconditionally, while only the assertion method is conditional
   on UNITY_EDITOR. ComputeDeterministicHash traverses all 97 stats. Its current
   ToIntBits calls BitConverter.GetBytes three times per stat plus once per More
   contribution: at least 291 temporary byte arrays per invocation at source level.
   Guard the computation too, and use allocation-free bit reinterpretation where
   diagnostics remain enabled. Editor assertions can also flag legitimate immediate
   owner rebuilds in paused/unregistered execution; scope that diagnostic carefully.

2. **Avoid rebuilding all modifier lists four times per landed attack.**
   AttackProcessor gathers attacker modifiers twice and defender modifiers twice;
   Unit.GetAllModifiers creates lists, and player collection traverses allocated
   nodes and sockets. Prefer versioned collected lists and phase indexes. Invalidate
   on tree, power, socket, innate and outer-modifier changes. Do not merely reuse one
   list for the entire attack: callbacks can change modifiers between phases.

3. **Reuse stat bucket storage.** BaseUnitModifiers.Reset creates 97 fresh More
   lists per rebuild, plus additional lists from ClearModifier. Preserve and clear
   existing lists, cache enum arrays, and avoid allocating ModifierContainer objects
   merely to merge buckets. CopyFrom already reuses its target lists, but copies
   every stat on every attack. Optimize that only after profiling; custom attack
   modifiers can read attributes and other stats outside the direct refresh list.

4. **Avoid full copies to inspect two or three stats.**
   BalancedArmorEvasionModifier clones all stats to compare Armor/Evasion;
   AddedDamageToHighestElement does likewise for three damage types. Calculate
   effective aggregate-plus-concrete values without mutating the source, preserving
   additive Increased and multiplicative More semantics.

5. **Review effect-tick allocation if many effects are active.**
   EffectController creates a snapshot array every tick and performs linear
   Effects.Contains checks for each element before/after callbacks. Membership work
   can become quadratic. Any buffer/set optimization must preserve callback-driven
   removal, replacement and owner death; replacing it with a naive direct loop
   would lose existing correctness protection.

Keep stable serialized IDs if dictionaries are replaced with indexed storage.
Do not replace independent More factors with an Increased sum or a removal-by-division
scheme: -100% More factors and multiple independent sources require correct removal.

## Review outcome and remaining scope

Recommended order: Chill/armor boundaries, redistribution, mask semantics; then
the unconditional diagnostic hash and collection/reset allocations. Agree phase
semantics for conditional modifiers before a broad calculator refactor.

Documentation discrepancies corrected in the defence overview during this pass:
current Evasion.EqualRatingHitChance is 1.25 rather than the documented 0.8, and
MysticNegation also adds Armor * ArmorAsMysticNegation. These are documentation
corrections based on source, not gameplay changes or newly approved balance rules.

Remaining scope: exact current-build reachability of the conditional cases,
modifier ordering across all authored combinations, full serialized stat inventory,
localization completeness, live combat reproduction and allocation/performance
measurements. No gameplay fixes are claimed by this report.
