using SkillTree;
using UnityEngine;
using System.Collections.Generic;

namespace Battle
{
    public class EnemyStatPackageBuilder
    {
        public EnemySpawnData Build(
            float power,
            float totalPower,
            EnemyArchetype archetype,
            EnemyRarity rarity,
            EnemyStatBudgetConfig statBudgetConfig,
            int? affixCountOverride = null,
            bool applyRandomVariance = true)
        {
            float finalPower = EnemyPowerCalculator.Calculate(power, rarity, archetype, applyRandomVariance);
            
            var package = new EnemySpawnData(archetype, rarity, finalPower, ScriptableObject.CreateInstance<BaseInnateModifiers>());

            ApplyCategoryBudgets(package.Modifiers, finalPower, archetype, statBudgetConfig);
            ApplyArchetypeModifiers(package.Modifiers, archetype);
            ApplyAttackSpeed(package.Modifiers, archetype, applyRandomVariance);
            ApplyAccuracy(package.Modifiers, totalPower);
            ApplyRarityScaling(package.Modifiers, rarity);
            ApplyAffixes(package.Modifiers, archetype, rarity, affixCountOverride);
            
            return package;
        }
        
        private void ApplyCategoryBudgets(
            BaseInnateModifiers package,
            float power,
            EnemyArchetype archetype,
            EnemyStatBudgetConfig statBudgetConfig)
        {
            if (archetype == null)
                return;

            float totalCategoryWeight = archetype.GetTotalCategoryWeight();
            if (totalCategoryWeight <= 0f)
                return;

            var entries = new List<EnemyStatWeightEntry>();

            entries.Clear();
            archetype.AddHealthEntries(entries);
            float healthCategoryRatio = archetype.healthWeight / totalCategoryWeight;
            float healthBudget = power * healthCategoryRatio;
            ApplyConfiguredStats(package, healthBudget, healthCategoryRatio, entries, statBudgetConfig);

            entries.Clear();
            archetype.AddOffenceEntries(entries);
            float offenceCategoryRatio = archetype.offenceWeight / totalCategoryWeight;
            float offenceBudget = power * offenceCategoryRatio;
            ApplyConfiguredStats(package, offenceBudget, offenceCategoryRatio, entries, statBudgetConfig);

            entries.Clear();
            archetype.AddDefenceEntries(entries);
            float defenceCategoryRatio = archetype.defenceWeight / totalCategoryWeight;
            float defenceBudget = power * defenceCategoryRatio;
            ApplyConfiguredStats(package, defenceBudget, defenceCategoryRatio, entries, statBudgetConfig);

            entries.Clear();
            archetype.AddUtilityEntries(entries);
            float utilityCategoryRatio = archetype.utilityWeight / totalCategoryWeight;
            float utilityBudget = power * utilityCategoryRatio;
            ApplyConfiguredStats(package, utilityBudget, utilityCategoryRatio, entries, statBudgetConfig);
        }
        
        private void ApplyConfiguredStats(
            BaseInnateModifiers package,
            float categoryBudget,
            float categoryAllocationRatio,
            List<EnemyStatWeightEntry> entries,
            EnemyStatBudgetConfig statBudgetConfig)
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
                ApplyConfiguredStat(package, entry.StatType, statBudget, allocationRatio, statBudgetConfig);
            }
        }

        private void ApplyConfiguredStat(
            BaseInnateModifiers package,
            StatType statType,
            float budget,
            float allocationRatio,
            EnemyStatBudgetConfig statBudgetConfig)
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
            if (value <= 0f)
                return;

            Add(package, rule.modifierType, statType, value);
        }

        private void ApplyAttackSpeed(
            BaseInnateModifiers package,
            EnemyArchetype archetype,
            bool applyRandomVariance)
        {
            float attackSpeed = archetype.baseAttackSpeed;
            if (applyRandomVariance)
                attackSpeed *= Random.Range(0.98f, 1.02f);

            Add(package, ModifierType.Added, StatType.AttackSpeed, attackSpeed);
        }

        private void ApplyArchetypeModifiers(
            BaseInnateModifiers package,
            EnemyArchetype archetype)
        {
            if (archetype == null || archetype.extraModifiers == null || archetype.extraModifiers.Count == 0)
                return;

            package.AddRange(archetype.extraModifiers);
        }
        
        private void ApplyAccuracy(
            BaseInnateModifiers package,
            float power)
        {
            Add(package, ModifierType.Added, StatType.Accuracy, power);
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
        
        private void ApplyAffixes(
            BaseInnateModifiers package,
            EnemyArchetype archetype,
            EnemyRarity rarity,
            int? affixCountOverride)
        {
            if (rarity == EnemyRarity.Normal)
                return;

            if (archetype == null || archetype.possibleAffixes == null || archetype.possibleAffixes.Count == 0)
                return;

            int affixCount = affixCountOverride ?? (rarity switch
            {
                EnemyRarity.Magic => 1,
                EnemyRarity.Rare => 2,
                EnemyRarity.Elite => 4,
                EnemyRarity.Boss => 6,
                _ => 0
            });

            for (int i = 0; i < affixCount; i++)
            {
                var affix = archetype.possibleAffixes[
                    Random.Range(0, archetype.possibleAffixes.Count)];

                package.AddRange(affix.modifiers);
            }
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
