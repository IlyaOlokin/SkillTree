using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class GeneratedEnemyDefinition
    {
        public GeneratedEnemyDefinition(
            float waveWeight,
            EnemyCoreProfile coreProfile,
            EnemyAttackSpeedModule attackSpeedModule,
            EnemyAttackModule attackModule,
            EnemyDefenceModule defenceModule,
            EnemyUtilityModule utilityModule,
            IReadOnlyList<EnemyAffix> affixPool,
            WeaponType weaponType,
            float powerMultiplier = 1f)
        {
            WaveWeight = Mathf.Clamp(waveWeight, 0.2f, 1f);
            CoreProfile = coreProfile;
            AttackSpeedModule = attackSpeedModule;
            AttackModule = attackModule;
            DefenceModule = defenceModule;
            UtilityModule = utilityModule;
            AffixPool = affixPool;
            WeaponType = weaponType;
            PowerMultiplier = Mathf.Max(0f, powerMultiplier);
        }

        public float WaveWeight { get; }
        public float PowerMultiplier { get; }
        public EnemyCoreProfile CoreProfile { get; }
        public EnemyAttackSpeedModule AttackSpeedModule { get; }
        public EnemyAttackModule AttackModule { get; }
        public EnemyDefenceModule DefenceModule { get; }
        public EnemyUtilityModule UtilityModule { get; }
        public IReadOnlyList<EnemyAffix> AffixPool { get; }
        public WeaponType WeaponType { get; }
        public float BaseAttackSpeed => AttackSpeedModule != null ? AttackSpeedModule.BaseAttackSpeed : 1f;
        public IReadOnlyList<ModifierContainer> ExtraModifiers => CoreProfile != null ? CoreProfile.ExtraModifiers : null;

        public float ApplyPowerMultiplier(float power)
        {
            float finalPower = Mathf.Max(0f, power) * PowerMultiplier;
            return AttackSpeedModule != null ? AttackSpeedModule.ApplyPowerMultiplier(finalPower) : finalPower;
        }

        public float GetTotalCategoryWeight()
        {
            return CoreProfile != null ? CoreProfile.GetTotalCategoryWeight() : 0f;
        }

        public void AddHealthEntries(List<EnemyStatWeightEntry> entries)
        {
            if (entries == null)
                return;

            entries.Add(new EnemyStatWeightEntry(StatType.MaximumHealth, 1f));
        }

        public void AddOffenceEntries(List<EnemyStatWeightEntry> entries)
        {
            AttackModule?.AddEntries(entries);
        }

        public void AddDefenceEntries(List<EnemyStatWeightEntry> entries)
        {
            DefenceModule?.AddEntries(entries);
        }

        public void AddUtilityEntries(List<EnemyStatWeightEntry> entries)
        {
            UtilityModule?.AddEntries(entries);
        }
    }
}
