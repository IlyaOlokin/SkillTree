using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class EnemyIntelProvider
    {
        private const float Epsilon = 0.0001f;
        private const float FastAttackSpeedThreshold = 0.35f;
        private const float CriticalChanceThreshold = 0.07f;

        public static EnemyIntelData Build(EnemyUnit unit)
        {
            return Build(unit != null ? unit.SpawnData : null);
        }

        public static EnemyIntelData Build(EnemySpawnData spawnData)
        {
            if (spawnData == null)
            {
                return EnemyIntelData.Empty;
            }

            List<EnemyIntelFeature> features = new();
            HashSet<EnemyIntelFeature> seenFeatures = new();
            List<string> affixKeys = new();
            HashSet<string> seenAffixKeys = new();

            AddDefinitionFeatures(spawnData.Definition, features, seenFeatures);
            AddAffixFeatures(spawnData.Affixes, affixKeys, seenAffixKeys, features, seenFeatures);
            AddNonBaseModifierFeatures(spawnData.Modifiers, features, seenFeatures);
            AddCalculatedStatFeatures(spawnData.Modifiers, spawnData.BaseEvasion, features, seenFeatures);

            return new EnemyIntelData(features, affixKeys);
        }

        private static void AddDefinitionFeatures(
            GeneratedEnemyDefinition definition,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (definition == null)
            {
                return;
            }

            AddAttackModuleFeatures(definition.AttackModule, features, seenFeatures);
            AddDefenceModuleFeatures(definition.DefenceModule, features, seenFeatures);
            AddUtilityModuleFeatures(definition.UtilityModule, features, seenFeatures);

            AddModifierFeatures(definition.ExtraModifiers, features, seenFeatures, includeAddedModifiers: true);
        }

        private static void AddAttackModuleFeatures(
            EnemyAttackModule attackModule,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (attackModule == null)
            {
                return;
            }

            AddWeightedFeature(attackModule.physical, EnemyIntelFeatureType.PhysicalDamage, features, seenFeatures);
            AddWeightedFeature(attackModule.fire, EnemyIntelFeatureType.FireDamage, features, seenFeatures);
            AddWeightedFeature(attackModule.cold, EnemyIntelFeatureType.ColdDamage, features, seenFeatures);
            AddWeightedFeature(attackModule.lightning, EnemyIntelFeatureType.LightningDamage, features, seenFeatures);
            AddWeightedFeature(attackModule.light, EnemyIntelFeatureType.LightDamage, features, seenFeatures);
            AddWeightedFeature(attackModule.dark, EnemyIntelFeatureType.DarknessDamage, features, seenFeatures);
        }

        private static void AddDefenceModuleFeatures(
            EnemyDefenceModule defenceModule,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            EnemyDefenceWeights weights = defenceModule != null ? defenceModule.Weights : null;
            if (weights == null)
            {
                return;
            }

            AddWeightedFeature(weights.healthRegeneration, EnemyIntelFeatureType.HealthRegeneration, features, seenFeatures);
            AddWeightedFeature(weights.blockChance, EnemyIntelFeatureType.Block, features, seenFeatures);
            AddWeightedFeature(weights.mysticCleanse, EnemyIntelFeatureType.MysticCleanse, features, seenFeatures);
        }

        private static void AddUtilityModuleFeatures(
            EnemyUtilityModule utilityModule,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (utilityModule == null)
            {
                return;
            }

            AddEffectWeightsFeature(utilityModule.physical, EnemyIntelFeatureType.Bleed, features, seenFeatures);
            AddEffectWeightsFeature(utilityModule.fire, EnemyIntelFeatureType.Ignite, features, seenFeatures);
            AddEffectWeightsFeature(utilityModule.cold, EnemyIntelFeatureType.Chill, features, seenFeatures);
            AddEffectWeightsFeature(utilityModule.lightning, EnemyIntelFeatureType.Overcharge, features, seenFeatures);
        }

        private static void AddAffixFeatures(
            IReadOnlyList<EnemyAffix> affixes,
            List<string> affixKeys,
            HashSet<string> seenAffixKeys,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (affixes == null)
            {
                return;
            }

            for (int i = 0; i < affixes.Count; i++)
            {
                EnemyAffix affix = affixes[i];
                if (affix == null)
                {
                    continue;
                }

                AddAffixKey(affix.affixName, affixKeys, seenAffixKeys);
                AddAffixStatWeightFeatures(affix.addedStatWeights, features, seenFeatures);
                AddModifierFeatures(affix.modifiers, features, seenFeatures, includeAddedModifiers: true);
            }
        }

        private static void AddAffixKey(string key, List<string> affixKeys, HashSet<string> seenAffixKeys)
        {
            if (string.IsNullOrWhiteSpace(key) || !seenAffixKeys.Add(key))
            {
                return;
            }

            affixKeys.Add(key);
        }

        private static void AddAffixStatWeightFeatures(
            IReadOnlyList<EnemyAffixStatWeight> statWeights,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (statWeights == null)
            {
                return;
            }

            for (int i = 0; i < statWeights.Count; i++)
            {
                EnemyAffixStatWeight statWeight = statWeights[i];
                if (statWeight == null || statWeight.weight <= Epsilon)
                {
                    continue;
                }

                if (IsCalculatedStat(statWeight.statType))
                {
                    continue;
                }

                if (TryGetFeatureType(statWeight.statType, out EnemyIntelFeatureType featureType))
                {
                    AddFeature(features, seenFeatures, featureType, EnemyIntelFeatureDisposition.Strength);
                }
            }
        }

        private static void AddNonBaseModifierFeatures(
            BaseInnateModifiers modifiers,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            AddModifierFeatures(
                modifiers != null ? modifiers.baseModifiers : null,
                features,
                seenFeatures,
                includeAddedModifiers: false);
        }

        private static void AddModifierFeatures(
            IReadOnlyList<ModifierContainer> modifiers,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures,
            bool includeAddedModifiers)
        {
            if (modifiers == null)
            {
                return;
            }

            for (int i = 0; i < modifiers.Count; i++)
            {
                AddModifierFeature(modifiers[i], features, seenFeatures, includeAddedModifiers);
            }
        }

        private static void AddModifierFeature(
            ModifierContainer modifier,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures,
            bool includeAddedModifiers)
        {
            if (modifier == null || modifier.value == 0f || !TryGetFeatureType(modifier.statType, out EnemyIntelFeatureType featureType))
            {
                return;
            }

            if (IsCalculatedStat(modifier.statType))
            {
                return;
            }

            if (modifier.value < 0f && !CanShowWeakness(modifier.statType))
            {
                return;
            }

            if (!includeAddedModifiers && modifier.modifierType == ModifierType.Added)
            {
                return;
            }

            AddFeature(features, seenFeatures, featureType, GetDisposition(modifier));
        }

        private static EnemyIntelFeatureDisposition GetDisposition(ModifierContainer modifier)
        {
            return modifier.value < 0f
                ? EnemyIntelFeatureDisposition.Weakness
                : EnemyIntelFeatureDisposition.Strength;
        }

        private static void AddWeightedFeature(
            float weight,
            EnemyIntelFeatureType featureType,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (weight <= Epsilon)
            {
                return;
            }

            AddFeature(features, seenFeatures, featureType, EnemyIntelFeatureDisposition.Strength);
        }

        private static void AddCalculatedStatFeatures(
            BaseInnateModifiers modifiers,
            float baseEvasion,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (modifiers == null)
            {
                return;
            }

            BaseUnitModifiers calculatedModifiers = BuildCalculatedModifiers(modifiers);
            AddAttackSpeedFeature(calculatedModifiers, features, seenFeatures);
            AddCriticalFeatures(calculatedModifiers, features, seenFeatures);
            AddZeroBaselineFeature(calculatedModifiers, StatType.Armor, EnemyIntelFeatureType.Armor, features, seenFeatures);
            AddEvasionFeature(calculatedModifiers, baseEvasion, features, seenFeatures);
            AddBarrierFeature(calculatedModifiers, features, seenFeatures);
            AddResistanceFeature(calculatedModifiers, StatType.ElementalResistance, EnemyIntelFeatureType.ElementalResistance, features, seenFeatures);
            AddResistanceFeature(calculatedModifiers, StatType.FireResistance, EnemyIntelFeatureType.FireResistance, features, seenFeatures);
            AddResistanceFeature(calculatedModifiers, StatType.ColdResistance, EnemyIntelFeatureType.ColdResistance, features, seenFeatures);
            AddResistanceFeature(calculatedModifiers, StatType.LightningResistance, EnemyIntelFeatureType.LightningResistance, features, seenFeatures);
        }

        private static BaseUnitModifiers BuildCalculatedModifiers(BaseInnateModifiers modifiers)
        {
            BaseUnitModifiers calculatedModifiers = new BaseUnitModifiers();
            if (modifiers?.baseModifiers == null)
            {
                return calculatedModifiers;
            }

            for (int i = 0; i < modifiers.baseModifiers.Count; i++)
            {
                ModifierContainer modifier = modifiers.baseModifiers[i];
                if (modifier != null)
                {
                    calculatedModifiers.ChangeModifierValue(modifier);
                }
            }

            StatCalculator.MergeDamageModifiers(calculatedModifiers);
            StatCalculator.MergeDefenceModifiers(calculatedModifiers);
            StatCalculator.MergeAilmentModifiers(calculatedModifiers);

            return calculatedModifiers;
        }

        private static void AddAttackSpeedFeature(
            BaseUnitModifiers modifiers,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            float attackSpeed = StatCalculator.GetStat(modifiers, StatType.AttackSpeed);
            if (attackSpeed <= FastAttackSpeedThreshold + Epsilon)
            {
                return;
            }

            AddFeature(features, seenFeatures, EnemyIntelFeatureType.AttackSpeed, EnemyIntelFeatureDisposition.Strength);
        }

        private static void AddCriticalFeatures(
            BaseUnitModifiers modifiers,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            float criticalChance = StatCalculator.GetStat(modifiers, StatType.CritChance);
            if (criticalChance <= CriticalChanceThreshold + Epsilon)
            {
                return;
            }

            AddFeature(features, seenFeatures, EnemyIntelFeatureType.CriticalChance, EnemyIntelFeatureDisposition.Strength);

            float criticalDamageBonus = StatCalculator.GetStat(modifiers, StatType.CritDamageBonus);
            if (criticalDamageBonus > Epsilon)
            {
                AddFeature(features, seenFeatures, EnemyIntelFeatureType.CriticalDamage, EnemyIntelFeatureDisposition.Strength);
            }
        }

        private static void AddZeroBaselineFeature(
            BaseUnitModifiers modifiers,
            StatType statType,
            EnemyIntelFeatureType featureType,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            float value = StatCalculator.GetStat(modifiers, statType);
            if (Mathf.Abs(value) <= Epsilon)
            {
                return;
            }

            AddFeature(
                features,
                seenFeatures,
                featureType,
                value > 0f
                    ? EnemyIntelFeatureDisposition.Strength
                    : EnemyIntelFeatureDisposition.Weakness);
        }

        private static void AddEvasionFeature(
            BaseUnitModifiers modifiers,
            float baseEvasion,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            float value = StatCalculator.GetStat(modifiers, StatType.Evasion);
            float difference = value - baseEvasion;
            if (Mathf.Abs(difference) <= Epsilon)
            {
                return;
            }

            AddFeature(
                features,
                seenFeatures,
                EnemyIntelFeatureType.Evasion,
                difference > 0f
                    ? EnemyIntelFeatureDisposition.Strength
                    : EnemyIntelFeatureDisposition.Weakness);
        }

        private static void AddBarrierFeature(
            BaseUnitModifiers modifiers,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            float barrierCount = StatCalculator.GetStat(modifiers, StatType.BarrierCount);
            if (barrierCount <= Epsilon)
            {
                return;
            }

            AddFeature(features, seenFeatures, EnemyIntelFeatureType.Barrier, EnemyIntelFeatureDisposition.Strength);
        }

        private static void AddResistanceFeature(
            BaseUnitModifiers modifiers,
            StatType statType,
            EnemyIntelFeatureType featureType,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            AddZeroBaselineFeature(modifiers, statType, featureType, features, seenFeatures);
        }

        private static void AddEffectWeightsFeature(
            EnemyEffectWeights weights,
            EnemyIntelFeatureType featureType,
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures)
        {
            if (weights == null)
            {
                return;
            }

            if (weights.power > Epsilon || weights.chance > Epsilon || weights.mitigation > Epsilon)
            {
                AddFeature(features, seenFeatures, featureType, EnemyIntelFeatureDisposition.Strength);
            }
        }

        private static void AddFeature(
            List<EnemyIntelFeature> features,
            HashSet<EnemyIntelFeature> seenFeatures,
            EnemyIntelFeatureType type,
            EnemyIntelFeatureDisposition disposition)
        {
            EnemyIntelFeature feature = new EnemyIntelFeature(type, disposition);
            if (!seenFeatures.Add(feature))
            {
                return;
            }

            features.Add(feature);
        }

        private static bool TryGetFeatureType(StatType statType, out EnemyIntelFeatureType featureType)
        {
            switch (statType)
            {
                case StatType.Damage:
                    featureType = EnemyIntelFeatureType.Damage;
                    return true;
                case StatType.ElementalDamage:
                    featureType = EnemyIntelFeatureType.ElementalDamage;
                    return true;
                case StatType.MysticDamage:
                    featureType = EnemyIntelFeatureType.MysticDamage;
                    return true;
                case StatType.PhysicalDamage:
                    featureType = EnemyIntelFeatureType.PhysicalDamage;
                    return true;
                case StatType.FireDamage:
                    featureType = EnemyIntelFeatureType.FireDamage;
                    return true;
                case StatType.ColdDamage:
                    featureType = EnemyIntelFeatureType.ColdDamage;
                    return true;
                case StatType.LightningDamage:
                    featureType = EnemyIntelFeatureType.LightningDamage;
                    return true;
                case StatType.LightDamage:
                    featureType = EnemyIntelFeatureType.LightDamage;
                    return true;
                case StatType.DarknessDamage:
                    featureType = EnemyIntelFeatureType.DarknessDamage;
                    return true;
                case StatType.PoisonDamage:
                    featureType = EnemyIntelFeatureType.PoisonDamage;
                    return true;
                case StatType.Armor:
                    featureType = EnemyIntelFeatureType.Armor;
                    return true;
                case StatType.Evasion:
                    featureType = EnemyIntelFeatureType.Evasion;
                    return true;
                case StatType.BlockChance:
                    featureType = EnemyIntelFeatureType.Block;
                    return true;
                case StatType.BarrierCount:
                case StatType.BarrierCapacity:
                case StatType.BarrierRegenerationSpeed:
                    featureType = EnemyIntelFeatureType.Barrier;
                    return true;
                case StatType.HealthRegenerationPerSecond:
                    featureType = EnemyIntelFeatureType.HealthRegeneration;
                    return true;
                case StatType.ElementalResistance:
                    featureType = EnemyIntelFeatureType.ElementalResistance;
                    return true;
                case StatType.FireResistance:
                    featureType = EnemyIntelFeatureType.FireResistance;
                    return true;
                case StatType.ColdResistance:
                    featureType = EnemyIntelFeatureType.ColdResistance;
                    return true;
                case StatType.LightningResistance:
                    featureType = EnemyIntelFeatureType.LightningResistance;
                    return true;
                case StatType.MysticNegation:
                    featureType = EnemyIntelFeatureType.MysticNegation;
                    return true;
                case StatType.MysticCleansePerSecond:
                    featureType = EnemyIntelFeatureType.MysticCleanse;
                    return true;
                case StatType.AttackSpeed:
                    featureType = EnemyIntelFeatureType.AttackSpeed;
                    return true;
                case StatType.CritChance:
                    featureType = EnemyIntelFeatureType.CriticalChance;
                    return true;
                case StatType.CritDamageBonus:
                    featureType = EnemyIntelFeatureType.CriticalDamage;
                    return true;
                case StatType.AilmentPower:
                case StatType.AilmentChance:
                case StatType.AilmentGuard:
                    featureType = EnemyIntelFeatureType.Ailment;
                    return true;
                case StatType.IgnitePower:
                case StatType.IgniteChance:
                case StatType.IgniteMitigation:
                    featureType = EnemyIntelFeatureType.Ignite;
                    return true;
                case StatType.ChillPower:
                case StatType.ChillChance:
                case StatType.ChillDurationReduction:
                    featureType = EnemyIntelFeatureType.Chill;
                    return true;
                case StatType.OverchargePower:
                case StatType.OverchargeChance:
                case StatType.OverchargeAvoidanceChance:
                    featureType = EnemyIntelFeatureType.Overcharge;
                    return true;
                case StatType.BleedPower:
                case StatType.BleedChance:
                case StatType.BleedMitigation:
                    featureType = EnemyIntelFeatureType.Bleed;
                    return true;
                case StatType.SunderChance:
                case StatType.SunderPower:
                case StatType.SunderMitigation:
                    featureType = EnemyIntelFeatureType.Sunder;
                    return true;
                case StatType.DistractChance:
                case StatType.DistractPower:
                case StatType.DistractMitigation:
                    featureType = EnemyIntelFeatureType.Distract;
                    return true;
                case StatType.ExposeChance:
                case StatType.ExposePower:
                case StatType.ExposeMitigation:
                    featureType = EnemyIntelFeatureType.Expose;
                    return true;
                default:
                    featureType = default;
                    return false;
            }
        }

        private static bool IsResistanceStat(StatType statType)
        {
            return statType == StatType.ElementalResistance
                || statType == StatType.FireResistance
                || statType == StatType.ColdResistance
                || statType == StatType.LightningResistance;
        }

        private static bool IsCalculatedStat(StatType statType)
        {
            return statType == StatType.AttackSpeed
                || statType == StatType.CritChance
                || statType == StatType.CritDamageBonus
                || statType == StatType.Armor
                || statType == StatType.Evasion
                || statType == StatType.BarrierCount
                || statType == StatType.BarrierCapacity
                || statType == StatType.BarrierRegenerationSpeed
                || IsResistanceStat(statType);
        }

        private static bool CanShowWeakness(StatType statType)
        {
            return statType == StatType.Armor
                || statType == StatType.Evasion
                || IsResistanceStat(statType);
        }
    }
}
