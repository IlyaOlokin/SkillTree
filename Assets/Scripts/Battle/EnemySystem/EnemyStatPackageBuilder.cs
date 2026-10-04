using SkillTree;
using UnityEngine;
using System.Collections.Generic;

namespace Battle
{
    public class EnemyStatPackageBuilder
    {
        private enum EnemyAffixStatWeightCategory
        {
            Health,
            Offence,
            Defence,
            Utility
        }

        public EnemySpawnData Build(
            float power,
            float totalPower,
            GeneratedEnemyDefinition definition,
            EnemyRarity rarity,
            EnemyStatBudgetConfig statBudgetConfig,
            EnemyAffixRollSettings affixRollSettings = null,
            EnemyAffixRollModifier affixRollModifier = default,
            float experienceRewardMultiplier = 1f,
            bool applyRandomVariance = true,
            IReadOnlyList<EnemyAffix> fallbackAffixPool = null)
        {
            float finalPower = EnemyPowerCalculator.Calculate(power, rarity, definition, applyRandomVariance);

            var modifiers = ScriptableObject.CreateInstance<BaseInnateModifiers>();

            ApplyCategoryBudgets(modifiers, finalPower, definition, statBudgetConfig);
            ApplyDefinitionModifiers(modifiers, definition);
            ApplyAttackSpeed(modifiers, definition, applyRandomVariance);
            ApplyAccuracy(modifiers, totalPower);
            float baseEvasion = ApplyEvasion(modifiers, totalPower);
            ApplyRarityScaling(modifiers, rarity);
            var rolledAffixes = new List<EnemyAffix>();
            float affixExperienceMultiplier = ApplyAffixes(
                modifiers,
                finalPower,
                definition,
                rarity,
                statBudgetConfig,
                affixRollSettings,
                affixRollModifier,
                rolledAffixes,
                fallbackAffixPool);

            var package = new EnemySpawnData(
                definition,
                rarity,
                finalPower,
                finalPower * affixExperienceMultiplier * Mathf.Max(0f, experienceRewardMultiplier),
                modifiers,
                rolledAffixes,
                baseEvasion,
                ownsModifiers: true);
            
            return package;
        }
        
        private void ApplyCategoryBudgets(
            BaseInnateModifiers package,
            float power,
            GeneratedEnemyDefinition definition,
            EnemyStatBudgetConfig statBudgetConfig)
        {
            if (definition == null || definition.CoreProfile == null)
                return;

            float totalCategoryWeight = definition.GetTotalCategoryWeight();
            if (totalCategoryWeight <= 0f)
                return;

            var entries = new List<EnemyStatWeightEntry>();

            entries.Clear();
            definition.AddHealthEntries(entries);
            float healthCategoryRatio = definition.CoreProfile.healthWeight / totalCategoryWeight;
            float healthBudget = power * healthCategoryRatio;
            ApplyConfiguredStats(package, healthBudget, healthCategoryRatio, entries, statBudgetConfig, power);

            entries.Clear();
            definition.AddOffenceEntries(entries);
            float offenceCategoryRatio = definition.CoreProfile.offenceWeight / totalCategoryWeight;
            float offenceBudget = power * offenceCategoryRatio;
            ApplyConfiguredStats(package, offenceBudget, offenceCategoryRatio, entries, statBudgetConfig, power);

            entries.Clear();
            definition.AddDefenceEntries(entries);
            float defenceCategoryRatio = definition.CoreProfile.defenceWeight / totalCategoryWeight;
            float defenceBudget = power * defenceCategoryRatio;
            ApplyConfiguredStats(package, defenceBudget, defenceCategoryRatio, entries, statBudgetConfig, power);

            entries.Clear();
            definition.AddUtilityEntries(entries);
            float utilityCategoryRatio = definition.CoreProfile.utilityWeight / totalCategoryWeight;
            float utilityBudget = power * utilityCategoryRatio;
            ApplyConfiguredStats(package, utilityBudget, utilityCategoryRatio, entries, statBudgetConfig, power);
        }
        
        private void ApplyConfiguredStats(
            BaseInnateModifiers package,
            float categoryBudget,
            float categoryAllocationRatio,
            List<EnemyStatWeightEntry> entries,
            EnemyStatBudgetConfig statBudgetConfig,
            float power)
        {
            if (categoryBudget <= 0f || entries == null || entries.Count == 0)
                return;

            float totalStatWeight = 0f;
            for (int i = 0; i < entries.Count; i++)
                totalStatWeight += Mathf.Max(0f, entries[i].Weight);

            if (totalStatWeight <= 0f)
                return;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Weight <= 0f)
                    continue;

                float normalizedStatWeight = entry.Weight / totalStatWeight;
                float statBudget = categoryBudget * normalizedStatWeight;
                float allocationRatio = categoryAllocationRatio * normalizedStatWeight;
                ApplyConfiguredStat(package, entry.StatType, statBudget, allocationRatio, statBudgetConfig, power);
            }
        }

        private void ApplyConfiguredStat(
            BaseInnateModifiers package,
            StatType statType,
            float budget,
            float allocationRatio,
            EnemyStatBudgetConfig statBudgetConfig,
            float power)
        {
            if (budget <= 0f)
                return;

            EnemyStatBudgetRule rule = statBudgetConfig != null
                ? statBudgetConfig.GetRule(statType)
                : EnemyStatBudgetRuleDefaults.Get(statType);

            if (statType == StatType.BarrierCount)
            {
                float barrierCount = Mathf.Floor(rule.Evaluate(budget, allocationRatio) + 0.5f);
                if (barrierCount > 0f)
                    Add(package, rule.modifierType, statType, barrierCount);

                return;
            }

            if (statType == StatType.HealthRegenerationPerSecond)
            {
                float maximumHealth = CalculatePackageStat(package, StatType.MaximumHealth);
                float healthRegenerationRatio = rule.Evaluate(budget, allocationRatio);
                float healthRegeneration = maximumHealth * healthRegenerationRatio;
                if (healthRegeneration > 0f)
                    Add(package, rule.modifierType, statType, healthRegeneration);

                return;
            }

            float value = rule.Evaluate(budget, allocationRatio);
            if (statType == StatType.MaximumHealth || statType == StatType.BarrierCapacity)
            {
                float survivabilityMultiplier = statBudgetConfig != null
                    ? statBudgetConfig.GetSurvivabilityMultiplier(power)
                    : EnemyStatBudgetConfig.CalculateSurvivabilityMultiplier(power);
                value *= survivabilityMultiplier;
            }

            if (value <= 0f)
                return;

            Add(package, rule.modifierType, statType, value);
        }

        private void ApplyAttackSpeed(
            BaseInnateModifiers package,
            GeneratedEnemyDefinition definition,
            bool applyRandomVariance)
        {
            float attackSpeed = definition != null ? definition.BaseAttackSpeed : 1f;
            if (applyRandomVariance)
                attackSpeed *= Random.Range(0.98f, 1.02f);

            Add(package, ModifierType.Added, StatType.AttackSpeed, attackSpeed);
        }

        private void ApplyDefinitionModifiers(
            BaseInnateModifiers package,
            GeneratedEnemyDefinition definition)
        {
            if (definition?.ExtraModifiers == null || definition.ExtraModifiers.Count == 0)
                return;

            package.AddRange(definition.ExtraModifiers);
        }
        
        private void ApplyAccuracy(
            BaseInnateModifiers package,
            float power)
        {
            Add(package, ModifierType.Added, StatType.Accuracy, power / 7f);
        }

        private float ApplyEvasion(
            BaseInnateModifiers package,
            float power)
        {
            float evasion = power / 30f;
            Add(package, ModifierType.Added, StatType.Evasion, evasion);
            return evasion;
        }
        
        private void ApplyRarityScaling(
            BaseInnateModifiers package,
            EnemyRarity rarity)
        {
            switch (rarity)
            {
                case EnemyRarity.Magic:
                    break;

                case EnemyRarity.Rare:
                    break;

                case EnemyRarity.Elite:
                    break;

                case EnemyRarity.Boss:
                    Add(package, ModifierType.More, StatType.Damage, -0.5f);
                    Add(package, ModifierType.More, StatType.MaximumHealth, -0.2f);
                    break;
            }
        }
        
        private float ApplyAffixes(
            BaseInnateModifiers package,
            float power,
            GeneratedEnemyDefinition definition,
            EnemyRarity rarity,
            EnemyStatBudgetConfig statBudgetConfig,
            EnemyAffixRollSettings affixRollSettings,
            EnemyAffixRollModifier affixRollModifier,
            List<EnemyAffix> rolledAffixes,
            IReadOnlyList<EnemyAffix> fallbackAffixPool)
        {
            bool canUsePrimaryPool = HasUsableAffixes(definition?.AffixPool);
            bool canUseFallbackPool = rarity == EnemyRarity.Boss && HasUsableAffixes(fallbackAffixPool);
            if (!canUsePrimaryPool && !canUseFallbackPool)
                return 1f;

            int affixCount = affixRollSettings != null
                ? affixRollSettings.RollAffixCount(rarity, affixRollModifier)
                : EnemyAffixRollRule.GetDefault(rarity).RollCount(affixRollModifier);
            if (affixCount <= 0)
                return 1f;

            float experienceMultiplier = 1f;
            var addedStatWeights = new List<EnemyStatWeightEntry>();
            var selectedAffixes = rolledAffixes ?? new List<EnemyAffix>();
            int remainingAffixes = affixCount;

            var primaryCandidates = new List<EnemyAffix>();
            AddUniqueAffixes(primaryCandidates, definition?.AffixPool, selectedAffixes);
            remainingAffixes -= ApplyRandomAffixes(
                package,
                primaryCandidates,
                remainingAffixes,
                selectedAffixes,
                addedStatWeights,
                ref experienceMultiplier);

            if (rarity == EnemyRarity.Boss && remainingAffixes > 0)
            {
                var fallbackCandidates = new List<EnemyAffix>();
                AddUniqueAffixes(fallbackCandidates, fallbackAffixPool, selectedAffixes);
                ApplyRandomAffixes(
                    package,
                    fallbackCandidates,
                    remainingAffixes,
                    selectedAffixes,
                    addedStatWeights,
                    ref experienceMultiplier);
            }

            ApplyAffixAddedStatWeights(package, power, definition, statBudgetConfig, addedStatWeights);

            return experienceMultiplier;
        }

        private static int ApplyRandomAffixes(
            BaseInnateModifiers package,
            List<EnemyAffix> candidates,
            int requestedCount,
            List<EnemyAffix> rolledAffixes,
            List<EnemyStatWeightEntry> addedStatWeights,
            ref float experienceMultiplier)
        {
            if (requestedCount <= 0 || candidates == null || candidates.Count == 0)
                return 0;

            int appliedCount = Mathf.Min(requestedCount, candidates.Count);
            for (int i = 0; i < appliedCount; i++)
            {
                int index = Random.Range(0, candidates.Count);
                var affix = candidates[index];
                candidates.RemoveAt(index);

                rolledAffixes?.Add(affix);

                if (affix.modifiers != null)
                    package.AddRange(affix.modifiers);

                AddAffixAddedStatWeights(addedStatWeights, affix);
                experienceMultiplier *= Mathf.Max(0f, 1f + affix.moreExperience);
            }

            return appliedCount;
        }

        private static void AddUniqueAffixes(
            List<EnemyAffix> target,
            IReadOnlyList<EnemyAffix> source,
            IReadOnlyList<EnemyAffix> excluded)
        {
            if (target == null || source == null)
                return;

            for (int i = 0; i < source.Count; i++)
            {
                var affix = source[i];
                if (affix == null || ContainsAffix(target, affix) || ContainsAffix(excluded, affix))
                    continue;

                target.Add(affix);
            }
        }

        private static bool HasUsableAffixes(IReadOnlyList<EnemyAffix> affixes)
        {
            if (affixes == null)
                return false;

            for (int i = 0; i < affixes.Count; i++)
            {
                if (affixes[i] != null)
                    return true;
            }

            return false;
        }

        private static bool ContainsAffix(IReadOnlyList<EnemyAffix> affixes, EnemyAffix affix)
        {
            if (affixes == null || affix == null)
                return false;

            for (int i = 0; i < affixes.Count; i++)
            {
                if (affixes[i] == affix)
                    return true;
            }

            return false;
        }

        private void ApplyAffixAddedStatWeights(
            BaseInnateModifiers package,
            float power,
            GeneratedEnemyDefinition definition,
            EnemyStatBudgetConfig statBudgetConfig,
            List<EnemyStatWeightEntry> addedStatWeights)
        {
            if (package == null || definition?.CoreProfile == null || addedStatWeights == null || addedStatWeights.Count == 0)
                return;

            float totalCategoryWeight = definition.GetTotalCategoryWeight();
            if (totalCategoryWeight <= 0f)
                return;

            var baseEntries = new List<EnemyStatWeightEntry>();

            ApplyAffixAddedStatWeightCategory(
                package,
                power,
                definition,
                EnemyAffixStatWeightCategory.Health,
                definition.CoreProfile.healthWeight / totalCategoryWeight,
                statBudgetConfig,
                addedStatWeights,
                baseEntries);

            ApplyAffixAddedStatWeightCategory(
                package,
                power,
                definition,
                EnemyAffixStatWeightCategory.Offence,
                definition.CoreProfile.offenceWeight / totalCategoryWeight,
                statBudgetConfig,
                addedStatWeights,
                baseEntries);

            ApplyAffixAddedStatWeightCategory(
                package,
                power,
                definition,
                EnemyAffixStatWeightCategory.Defence,
                definition.CoreProfile.defenceWeight / totalCategoryWeight,
                statBudgetConfig,
                addedStatWeights,
                baseEntries);

            ApplyAffixAddedStatWeightCategory(
                package,
                power,
                definition,
                EnemyAffixStatWeightCategory.Utility,
                definition.CoreProfile.utilityWeight / totalCategoryWeight,
                statBudgetConfig,
                addedStatWeights,
                baseEntries);
        }

        private void ApplyAffixAddedStatWeightCategory(
            BaseInnateModifiers package,
            float power,
            GeneratedEnemyDefinition definition,
            EnemyAffixStatWeightCategory category,
            float categoryRatio,
            EnemyStatBudgetConfig statBudgetConfig,
            List<EnemyStatWeightEntry> addedStatWeights,
            List<EnemyStatWeightEntry> baseEntries)
        {
            if (categoryRatio <= 0f)
                return;

            baseEntries.Clear();
            AddCategoryEntries(definition, category, baseEntries);

            const float epsilon = 0.0001f;
            float baseTotalWeight = 0f;
            for (int i = 0; i < baseEntries.Count; i++)
                baseTotalWeight += Mathf.Max(0f, baseEntries[i].Weight);

            float addedTotalWeight = 0f;
            for (int i = 0; i < addedStatWeights.Count; i++)
            {
                var entry = addedStatWeights[i];
                if (entry.Weight <= 0f || !IsAffixStatWeightCategory(entry.StatType, category))
                    continue;

                addedTotalWeight += entry.Weight;
            }

            float normalizationWeight = baseTotalWeight > epsilon
                ? baseTotalWeight
                : addedTotalWeight;
            if (normalizationWeight <= epsilon)
                return;

            float categoryBudget = power * categoryRatio;
            for (int i = 0; i < addedStatWeights.Count; i++)
            {
                var entry = addedStatWeights[i];
                if (entry.Weight <= 0f || !IsAffixStatWeightCategory(entry.StatType, category))
                    continue;

                float normalizedStatWeight = entry.Weight / normalizationWeight;
                float statBudget = categoryBudget * normalizedStatWeight;
                float allocationRatio = categoryRatio * normalizedStatWeight;
                ApplyConfiguredStat(package, entry.StatType, statBudget, allocationRatio, statBudgetConfig, power);
            }
        }

        private static void AddCategoryEntries(
            GeneratedEnemyDefinition definition,
            EnemyAffixStatWeightCategory category,
            List<EnemyStatWeightEntry> entries)
        {
            switch (category)
            {
                case EnemyAffixStatWeightCategory.Health:
                    definition.AddHealthEntries(entries);
                    break;
                case EnemyAffixStatWeightCategory.Offence:
                    definition.AddOffenceEntries(entries);
                    break;
                case EnemyAffixStatWeightCategory.Defence:
                    definition.AddDefenceEntries(entries);
                    break;
                case EnemyAffixStatWeightCategory.Utility:
                    definition.AddUtilityEntries(entries);
                    break;
            }
        }

        private static void AddAffixAddedStatWeights(List<EnemyStatWeightEntry> entries, EnemyAffix affix)
        {
            if (entries == null || affix?.addedStatWeights == null)
                return;

            for (int i = 0; i < affix.addedStatWeights.Count; i++)
            {
                var addedStatWeight = affix.addedStatWeights[i];
                if (addedStatWeight == null || addedStatWeight.weight <= 0f || addedStatWeight.statType == StatType.Empty)
                    continue;

                entries.Add(new EnemyStatWeightEntry(addedStatWeight.statType, addedStatWeight.weight));
            }
        }

        private static bool IsAffixStatWeightCategory(StatType statType, EnemyAffixStatWeightCategory category)
        {
            return category switch
            {
                EnemyAffixStatWeightCategory.Health => statType == StatType.MaximumHealth,
                EnemyAffixStatWeightCategory.Offence => IsOffenceStat(statType),
                EnemyAffixStatWeightCategory.Defence => IsDefenceStat(statType),
                EnemyAffixStatWeightCategory.Utility => IsUtilityStat(statType),
                _ => false
            };
        }

        private static bool IsOffenceStat(StatType statType)
        {
            return statType == StatType.Damage
                || statType == StatType.ElementalDamage
                || statType == StatType.MysticDamage
                || statType == StatType.PhysicalDamage
                || statType == StatType.FireDamage
                || statType == StatType.ColdDamage
                || statType == StatType.LightningDamage
                || statType == StatType.LightDamage
                || statType == StatType.DarknessDamage
                || statType == StatType.PoisonDamage;
        }

        private static bool IsDefenceStat(StatType statType)
        {
            return statType == StatType.Armor
                || statType == StatType.Evasion
                || statType == StatType.BlockChance
                || statType == StatType.Defence
                || statType == StatType.HealthRegenerationPerSecond
                || statType == StatType.BarrierCount
                || statType == StatType.BarrierCapacity
                || statType == StatType.BarrierRegenerationSpeed
                || statType == StatType.ElementalResistance
                || statType == StatType.FireResistance
                || statType == StatType.ColdResistance
                || statType == StatType.LightningResistance
                || statType == StatType.MaxElementalResistance
                || statType == StatType.MaxFireResistance
                || statType == StatType.MaxColdResistance
                || statType == StatType.MaxLightningResistance
                || statType == StatType.MysticNegation
                || statType == StatType.MysticCleansePerSecond;
        }

        private static bool IsUtilityStat(StatType statType)
        {
            return statType == StatType.CritChance
                || statType == StatType.CritDamageBonus
                || statType == StatType.AilmentPower
                || statType == StatType.IgnitePower
                || statType == StatType.ChillPower
                || statType == StatType.OverchargePower
                || statType == StatType.BleedPower
                || statType == StatType.AilmentChance
                || statType == StatType.IgniteChance
                || statType == StatType.ChillChance
                || statType == StatType.OverchargeChance
                || statType == StatType.BleedChance
                || statType == StatType.AilmentGuard
                || statType == StatType.IgniteMitigation
                || statType == StatType.ChillDurationReduction
                || statType == StatType.OverchargeAvoidanceChance
                || statType == StatType.BleedMitigation
                || statType == StatType.SunderChance
                || statType == StatType.SunderPower
                || statType == StatType.SunderMitigation
                || statType == StatType.DistractChance
                || statType == StatType.DistractPower
                || statType == StatType.DistractMitigation
                || statType == StatType.ExposeChance
                || statType == StatType.ExposePower
                || statType == StatType.ExposeMitigation;
        }
        
        private void Add(BaseInnateModifiers package,
            ModifierType type,
            StatType stat,
            float value)
        {
            package.AddModifier(new ModifierContainer(type, stat, value));
        }

        private static float CalculatePackageStat(BaseInnateModifiers package, StatType statType)
        {
            if (package == null)
                return 0f;

            var modifiers = new BaseUnitModifiers();
            foreach (var modifier in package.baseModifiers)
                modifiers.ChangeModifierValue(modifier);

            StatCalculator.MergeDamageModifiers(modifiers);
            StatCalculator.MergeDefenceModifiers(modifiers);
            StatCalculator.MergeAilmentModifiers(modifiers);

            return StatCalculator.GetStat(modifiers, statType);
        }
    }
}
