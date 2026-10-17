# StatType fixes — 2026-10-17

[Home](../Home.md) · [Stats](../Systems/StatsAndModifiers.md) · [Initial review](StatTypeReview20261017.md) · [Backlog](../Backlog.md)

Owner scope: fix the reviewed issues and optimize the inspected paths, excluding
damage-mask semantics; remove Poison completely from current code/content.

## Gameplay changes

- Chill duration is nonnegative. At 100% or higher duration reduction, ordinary
  application exits before spending ailment absorption, registering Chill for a
  Freeze upgrade or notifying ailment-application listeners. Chill.CanApply also
  rejects zero-duration repeat snapshots. Negative protection still extends its
  duration. Existing stacking, power and Freeze snapshot rules are retained.
- Armor.ApplyArmorMitigation clamps armor and physical damage to nonnegative
  values, matching the input policy of CalculatePhysicalMitigation. The formula
  and multiplication order for nonnegative inputs remain unchanged.
- ModifierPriority.AttributeFinalization is appended as ID 8. It runs after
  AllAttributes expansion and before attribute-derived modifiers. Redistribution
  overrides its phase to this boundary, consumes the rounded final STR/DEX/INT
  totals, clears their already-consumed multipliers and writes the distributed
  values. The existing remainder rule favors STR, then DEX, for positive totals
  and uses the corresponding subtraction order for negative totals. Multiple
  copies are idempotent at this boundary. Other phase ordering remains unchanged.
- NoArmor reads Armor plus the unexpanded Defence group. BalancedArmorEvasion
  reads that same effective defence without cloning all stat buckets. The
  highest-element modifier similarly evaluates concrete + Damage + ElementalDamage
  contributions without a full copy. Added/Increased summation and independent
  More factors keep their merge order.
- PlayerStatsWindow uses clamped expected critical layers for ordinary crit and
  uncapped nonnegative expected layers when an applicable OnAttack MultiCritModifier
  is collected. DPS remains a simplified estimate; it does not simulate arbitrary
  conditional attack modifiers or enemy defences. Nonpositive attack speed supplies
  zero estimated DPS. Disabled barrier regeneration displays an em dash instead of
  an invalid or negative interval. The dash is a language-independent symbol.

## Storage, collection and effect ownership

BaseUnitModifiers keeps its dictionaries and per-stat More lists between resets;
reset/clear zero scalar buckets and clear existing lists. Merging uses buckets
directly without temporary ModifierContainer allocations. CopyFrom keeps independent
target lists and now handles self-copy safely. Every stat is still copied into the
attack snapshot; the partial direct-stat refresh list is not used as a copy filter.
Stat/damage enum arrays are cached in the touched hot paths. Hash float conversion
uses BitConverter.SingleToInt32Bits without allocating byte arrays.

AttackProcessor computes the integrity hash only in UNITY_EDITOR. A Unit stat
revision lets the diagnostic ignore legitimate synchronous owner rebuilds during
paused/unregistered callbacks. It still detects persistent-stat mutation without
a rebuild. Combat phase order, effect-consumption timing and deferred Mods behavior
are retained.

Each modifier phase rents its own list and returns it in finally; nested attacks
therefore cannot overwrite a suspended phase's snapshot. Unit.GetAllModifiers()
and MainSkillTree.CollectAllModifiers() still return independently mutable lists.
Caller-owned buffer overloads avoid the temporary lists in the attack path.
Innate and outer modifier lists are read on each phase. Active node/gem collection
is cached and invalidated before node-change callbacks, when tree changes/rebuilds,
and on player recalculation requests/resets. During a nested tree operation it is
rebuilt on every collection. BonusZone.CollectModifier still executes each time,
including its restore-time count refresh. Runtime callers changing node definitions
without normal node notifications must request player recalculation; this refreshes
both the collection and stats.

EffectController uses reusable per-invocation tick lists and hash membership rather
than ToArray plus repeated linear Contains scans. Add/remove methods maintain the
membership set; each tick initially synchronizes it from Effects. All inspected
production effect insertions/removals use controller methods. Nested ticks have
separate snapshots; removals, clears, replacements and exceptions retain the prior
snapshot semantics. Public Effects remains available for inspection; mutations in
effect callbacks must use controller methods so lifetime cleanup and membership
stay consistent.

No arithmetic, grants, removal, node-power handling or serialized configuration of
BarrierDamageTypeMask / LifeStealTypeMask was changed. BL-040 is explicitly deferred.

## Poison removal and localization

Removed StatType.PoisonDamage and PoisonDamageMitigation; IDs 10 and 84 remain
retired. EnemyIntelFeatureType ID 9 is also retired. Remaining explicit IDs are
unchanged. Removed their offence classification, intel mapping, mitigation expansion
and display-rule entry. No Poison damage component existed in DamageType.

The scoped serialized scan found no stat/statType references to 10 or 84, or
featureType references to 9, in Assets .asset/.unity/.prefab files. A final search
found no remaining Poison text in current Assets C# or serialized assets. Historical
review documents retain their original observations.

Unity Localization APIs removed these 13 keys and their existing English entries:

- Descriptions: `enum.StatType.PoisonDamageMitigation`.
- Modifiers: `modifier.container.added.PoisonDamage`,
  `modifier.container.increased.PoisonDamage`,
  `modifier.container.decreased.PoisonDamage`,
  `modifier.container.more.PoisonDamage`, `modifier.container.less.PoisonDamage`,
  `modifier.container.none.PoisonDamage`.
- Modifiers: `modifier.container.added.PoisonDamageMitigation`,
  `modifier.container.increased.PoisonDamageMitigation`,
  `modifier.container.decreased.PoisonDamageMitigation`,
  `modifier.container.more.PoisonDamageMitigation`,
  `modifier.container.less.PoisonDamageMitigation`,
  `modifier.container.none.PoisonDamageMitigation`.

Changed localization assets: Assets/Localization/Descriptions Shared Data.asset,
Descriptions_en.asset, Modifiers Shared Data.asset and Modifiers_en.asset.
No new keys, new English texts, empty ru/de entries or translator TODOs are needed.
No ru/de table changed. Existing modifier description keys/text are retained,
including Modifiers `modifier.redistributeAttributesEvenly.description` =
`Redistributes total Strength, Dexterity and Intelligence evenly`.
No new effect name, EffectVisualType, status icon or tooltip registration is needed.

## Changed C# files

All paths below are relative to Assets/Scripts:

- StatType.cs, StatTypeDisplayRules.cs, StatCalculator.cs, ModifierPriority.cs.
- Battle/Armor.cs, AttackProcessor.cs, BaseUnitModifiers.cs, DamageCalculator.cs,
  Unit.cs.
- Battle/Effects/Chill.cs, EffectController.cs.
- Battle/EnemySystem/EnemyIntelData.cs, EnemyIntelProvider.cs,
  EnemyStatPackageBuilder.cs.
- SkillTree/MainSkillTree.cs.
- SkillTree/Modifiers/NoArmorModifiers/NoArmorModifier.cs.
- SkillTree/Modifiers/SpecialMods/RedistributeAttributesEvenlyModifier.cs,
  BalancedArmorEvasionModifier.cs, AddedDamageToHighestElement.cs.
- UI/PlayerStatsWindow.cs.

Updated documentation: this record, the initial stat review, Backlog, and the
StatsAndModifiers, SkillTree, CombatAndEffects, DefencesAndResources,
AilmentsAndDebuffs and HudAndTooltips system pages.

## Manual Unity Editor Steps

None required. Existing definitions/settings, script GUIDs, node assignments,
icons and tooltip entries are retained. No new modifier definition was created.
The affected existing definitions are:

- Assets/Scripts/SkillTree/Modifiers/SpecialMods/RedistributeAttributesEvenlyModifier.asset
- Assets/Scripts/SkillTree/Modifiers/NoArmorModifiers/NoArmorAddedAttackSpeed_0.1.asset
- Assets/Scripts/SkillTree/Modifiers/SpecialMods/BalancedArmorEvasionModifierIncreasedDamage_0.25.asset
- Assets/Scripts/SkillTree/Modifiers/SpecialMods/AddedDamageToHighestElement_6.asset

Redistribution's effective priority is supplied by its code override; its existing
serialized priority requires no migration. No scene or gameplay asset was changed.
The owner's staged MainScene and modifier additions were preserved.

## Icon Generation Prompt

Optional replacement prompts only; style is proposed, no visual reference was
inspected. No image was generated, imported or mapped by this task.

- **Redistribution:** Three equal steel, green and blue crystal pillars linked by
  a circular stream of energy. Centered symmetrical composition, bright rim lighting,
  dark smoky background, painterly fantasy RPG skill icon, bold silhouette readable
  at small sizes. No text, letters, numbers, logos or watermarks.
- **No Armor:** An unarmored gauntlet releasing a swift silver energy blade.
  Centered diagonal composition, silver and cyan palette, sharp rim lighting,
  dark smoky background, painterly fantasy RPG skill icon, clear silhouette readable
  at small sizes. No text, letters, numbers, logos or watermarks.
- **Balanced Armor and Evasion:** A steel shield and green wind spiral balanced
  around one glowing sword. Centered symmetrical composition, silver and emerald
  palette, dramatic rim lighting, dark smoky background, painterly fantasy RPG skill
  icon readable at small sizes. No text, letters, numbers, logos or watermarks.
- **Highest Element:** One large radiant elemental crystal drawing energy from
  smaller red, blue and violet sparks. Centered triangular composition, warm gold
  highlights, dramatic lighting, dark smoky background, painterly fantasy RPG skill
  icon readable at small sizes. No text, letters, numbers, logos or watermarks.

## Verification actually performed

- Unity CLI recompile / recompile_status targeting C:/UnityProjects/SkillTree:
  completed, failed=false, errors=[], compilationFailed=false.
- Editor readiness checked after domain reload before localization mutation.
  One recompile_status request timed out during reload; no mutation was retried.
- console_status: zero current Console errors, eight analyzer warnings. The
  warnings concern unchanged static-event/lifecycle code in TutorialEventAdapter,
  PlayerStatsWindow.OnValidate, PipelineDiscoveryRecovery and AilmentVisualAuthoring.
- run_script executed Temp/RemoveRetiredPoisonLocalization.cs through Unity APIs:
  no script diagnostics, success=true, all 13 removed keys reported. Only the four
  expected localization files changed, with entry deletions only.
- Scoped diff whitespace, remaining Poison references, retired serialized IDs,
  and documentation links/source paths checked.

No tests were written or run, no player build, gameplay playtest or performance
capture was performed. Compilation and source checks do not prove all combat
combinations are regression-free. BL-037 remains open for measured performance;
the corrected gameplay entries record implementation and pending Play Mode checks.
