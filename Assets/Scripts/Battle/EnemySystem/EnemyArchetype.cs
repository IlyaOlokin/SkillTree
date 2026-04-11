using System;
using UnityEngine;
using System.Collections.Generic;
using DropSystem;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Archetype")]
    public class EnemyArchetype : ScriptableObject
    {
        [Header("Spawn Rules")]
        [Min(1)] public int minLevel = 1;
        [Min(0)] public int maxLevel = 0;
        public bool bossOnly;
        public List<EnemyRarity> allowedRarities = new();

        [Header("Power Weights")]
        [Range(0f, 1f)] public float healthWeight = 1f;
        [Range(0f, 1f)] public float damageWeight = 1f;
        [Range(0f, 1f)] public float defenseWeight = 1f;
        [Range(0f, 1f)] public float barrierWeight = 0f;
        
        private float _prevHealth;
        private float _prevDamage;
        private float _prevDefense;
        private float _prevBarrier;

        [Header("Attack")]
        public float baseAttackSpeed = 1f;

        [Header("Damage Types")]
        public List<WeightedStat> damageTypes = new();

        [Header("Defense Types")]
        public List<WeightedStat> defenseTypes = new();

        [Header("Affixes")]
        public List<EnemyAffix> possibleAffixes = new();

        [Header("Drops")]
        [SerializeField] private GemDropTable gemDropTable;

        public GemDropTable GemDropTable => gemDropTable;

        private void OnValidate()
        {
            if (!Mathf.Approximately(healthWeight, _prevHealth))
            {
                Redistribute(WeightType.Health);
            }
            else if (!Mathf.Approximately(damageWeight, _prevDamage))
            {
                Redistribute(WeightType.Damage);
            }
            else if (!Mathf.Approximately(defenseWeight, _prevDefense))
            {
                Redistribute(WeightType.Defense);
            }
            else if (!Mathf.Approximately(barrierWeight, _prevBarrier))
            {
                Redistribute(WeightType.Barrier);
            }

            CachePrevious();
        }

        private void Redistribute(WeightType changedType)
        {
            float changedValue = GetValue(changedType);
            changedValue = Mathf.Clamp01(changedValue);

            float othersSum = GetTotal() - changedValue;
            
            if (othersSum <= 0f)
            {
                SetAllZero();
                SetValue(changedType, 1f);
                return;
            }

            float remainingBudget = 1f - changedValue;
            float scale = remainingBudget / othersSum;

            foreach (WeightType type in System.Enum.GetValues(typeof(WeightType)))
            {
                if (type == changedType)
                    continue;

                float v = GetValue(type);
                SetValue(type, v * scale);
            }

            SetValue(changedType, changedValue);
        }

        private float GetTotal()
        {
            return healthWeight + damageWeight + defenseWeight + barrierWeight;
        }

        private float GetValue(WeightType type)
        {
            return type switch
            {
                WeightType.Health => healthWeight,
                WeightType.Damage => damageWeight,
                WeightType.Defense => defenseWeight,
                WeightType.Barrier => barrierWeight,
                _ => 0f
            };
        }

        private void SetValue(WeightType type, float value)
        {
            switch (type)
            {
                case WeightType.Health: healthWeight = value; break;
                case WeightType.Damage: damageWeight = value; break;
                case WeightType.Defense: defenseWeight = value; break;
                case WeightType.Barrier: barrierWeight = value; break;
            }
        }

        private void SetAllZero()
        {
            healthWeight = 0f;
            damageWeight = 0f;
            defenseWeight = 0f;
            barrierWeight = 0f;
        }

        private void CachePrevious()
        {
            _prevHealth = healthWeight;
            _prevDamage = damageWeight;
            _prevDefense = defenseWeight;
            _prevBarrier = barrierWeight;
        }

        public bool Matches(WaveContext context, EnemyRarity rarity)
        {
            if (context.Level < minLevel)
                return false;

            if (maxLevel > 0 && context.Level > maxLevel)
                return false;

            if (bossOnly && rarity != EnemyRarity.Boss)
                return false;

            if (context.IsBossWave == false && rarity == EnemyRarity.Boss)
                return false;

            if (allowedRarities != null && allowedRarities.Count > 0 && allowedRarities.Contains(rarity) == false)
                return false;

            return true;
        }

        private enum WeightType
        {
            Health,
            Damage,
            Defense,
            Barrier
        }
    }

    [System.Serializable]
    public class WeightedStat
    {
        public StatType statType;
        public float weight = 1f;
    }
}
