using SkillTree;
using UnityEngine;
using System.Collections.Generic;

namespace Battle
{
    public class EnemyStatPackageBuilder
    {
        public EnemySpawnData Build(
            float power,
            EnemyArchetype archetype,
            EnemyRarity rarity)
        {
            float finalPower = power * EnemyRarityHelper.GetMultiplier(rarity);
            finalPower *= Random.Range(0.9f, 1.1f);
            
            var package =new EnemySpawnData(archetype, rarity, finalPower, ScriptableObject.CreateInstance<BaseInnateModifiers>());

            DistributePrimaryStats(package.Modifiers, finalPower, archetype);
            ApplyBarrier(package.Modifiers, finalPower, archetype);
            ApplyAttackSpeed(package.Modifiers, archetype);
            ApplyRarityScaling(package.Modifiers, rarity);
            ApplyAffixes(package.Modifiers, archetype, rarity);
            
            return package;
        }
        
        private void DistributePrimaryStats(
            BaseInnateModifiers package,
            float power,
            EnemyArchetype archetype)
        {
            float hp = power * archetype.healthWeight * 6f;
            Add(package, ModifierType.Added, StatType.MaximumHealth, hp);
            
            float damageBudget = power * archetype.damageWeight;

            foreach (var damageType in archetype.damageTypes)
            {
                float value = damageBudget * damageType.weight;
                Add(package, ModifierType.Added, damageType.statType, value);
            }
            
            float defenseBudget = power * archetype.defenseWeight * 0.6f;

            foreach (var defenseType in archetype.defenseTypes)
            {
                float value = defenseBudget * defenseType.weight;
                Add(package, ModifierType.Added, defenseType.statType, value);
            }
        }
        
        private void ApplyBarrier(
            BaseInnateModifiers package,
            float power,
            EnemyArchetype archetype)
        {
            if (archetype.barrierWeight <= 0f)
                return;
            
            int barrierCount = Mathf.FloorToInt(archetype.barrierWeight / 0.1f);

            if (barrierCount > 0)
            {
                Add(package, ModifierType.Added, StatType.BarrierCount, barrierCount);
            }
            
            float barrierPower = power * archetype.barrierWeight * 2f;
            Add(package, ModifierType.Added, StatType.BarrierPower, barrierPower);
        }
        
        private void ApplyAttackSpeed(
            BaseInnateModifiers package,
            EnemyArchetype archetype)
        {
            Add(package, ModifierType.Added, StatType.AttackSpeed, archetype.baseAttackSpeed);
        }
        
        private void ApplyRarityScaling(
            BaseInnateModifiers package,
            EnemyRarity rarity)
        {
            switch (rarity)
            {
                case EnemyRarity.Magic:
                    Add(package, ModifierType.Increased, StatType.Damage, 0.15f);
                    break;

                case EnemyRarity.Rare:
                    Add(package, ModifierType.Increased, StatType.Damage, 0.25f);
                    Add(package, ModifierType.Increased, StatType.MaximumHealth, 0.25f);
                    break;

                case EnemyRarity.Elite:
                    Add(package, ModifierType.More, StatType.Damage, 0.4f);
                    break;

                case EnemyRarity.Boss:
                    Add(package, ModifierType.More, StatType.Damage, 0.6f);
                    Add(package, ModifierType.More, StatType.MaximumHealth, 0.6f);
                    break;
            }
        }
        
        private void ApplyAffixes(
            BaseInnateModifiers package,
            EnemyArchetype archetype,
            EnemyRarity rarity)
        {
            if (rarity == EnemyRarity.Normal)
                return;

            int affixCount = rarity switch
            {
                EnemyRarity.Magic => 1,
                EnemyRarity.Rare => 2,
                EnemyRarity.Elite => 4,
                EnemyRarity.Boss => 6,
                _ => 0
            };

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
    }
}
