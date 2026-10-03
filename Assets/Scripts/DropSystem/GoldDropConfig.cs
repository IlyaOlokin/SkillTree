using Battle;
using UnityEngine;

namespace DropSystem
{
    [CreateAssetMenu(menuName = "Drops/Gold Drop Config", fileName = "NewGoldDropConfig")]
    public sealed class GoldDropConfig : ScriptableObject
    {
        [Header("Chance")]
        [SerializeField] [Range(0f, 1f)] private float baseDropChance = 0.7f;
        [SerializeField] [Min(0f)] private float powerForMaxChanceBonus = 140f;
        [SerializeField] [Range(0f, 1f)] private float maxPowerChanceBonus = 0.2f;

        [Header("Amount")]
        [SerializeField] [Min(0)] private int baseGold = 2;
        [SerializeField] [Min(0f)] private float goldPerPower = 0.35f;
        [SerializeField] [Min(0f)] private float levelMultiplierPerLevel = 0.035f;
        [SerializeField] [Min(0f)] private float minRandomAmountMultiplier = 0.85f;
        [SerializeField] [Min(0f)] private float maxRandomAmountMultiplier = 1.15f;

        [Header("Rarity chance multipliers")]
        [SerializeField] [Min(0f)] private float normalChanceMultiplier = 1f;
        [SerializeField] [Min(0f)] private float magicChanceMultiplier = 1.1f;
        [SerializeField] [Min(0f)] private float rareChanceMultiplier = 1.25f;
        [SerializeField] [Min(0f)] private float eliteChanceMultiplier = 1.45f;
        [SerializeField] [Min(0f)] private float bossChanceMultiplier = 1f;

        [Header("Rarity amount multipliers")]
        [SerializeField] [Min(0f)] private float normalAmountMultiplier = 1f;
        [SerializeField] [Min(0f)] private float magicAmountMultiplier = 1.35f;
        [SerializeField] [Min(0f)] private float rareAmountMultiplier = 1.75f;
        [SerializeField] [Min(0f)] private float eliteAmountMultiplier = 2.4f;
        [SerializeField] [Min(0f)] private float bossAmountMultiplier = 5f;

        public float CalculateDropChance(GoldDropContext context)
        {
            if (context == null)
                return 0f;

            float chance = baseDropChance + EvaluatePowerChanceBonus(context.Power);
            chance *= GetChanceMultiplier(context.Rarity);
            chance *= context.ChanceMultiplier;
            return Mathf.Clamp01(chance);
        }

        public int RollAmount(GoldDropContext context)
        {
            if (context == null)
                return 0;

            float randomMin = Mathf.Min(minRandomAmountMultiplier, maxRandomAmountMultiplier);
            float randomMax = Mathf.Max(minRandomAmountMultiplier, maxRandomAmountMultiplier);
            float amount = baseGold + context.Power * goldPerPower;
            amount *= 1f + Mathf.Max(0, context.SourceLevel - 1) * levelMultiplierPerLevel;
            amount *= GetAmountMultiplier(context.Rarity);
            amount *= context.AmountMultiplier;
            amount *= Random.Range(randomMin, randomMax);
            return Mathf.Max(1, Mathf.RoundToInt(amount));
        }

        private float EvaluatePowerChanceBonus(float power)
        {
            if (maxPowerChanceBonus <= 0f)
                return 0f;

            if (powerForMaxChanceBonus <= 0f)
                return maxPowerChanceBonus;

            return maxPowerChanceBonus * Mathf.Clamp01(power / powerForMaxChanceBonus);
        }

        private float GetChanceMultiplier(EnemyRarity rarity)
        {
            return rarity switch
            {
                EnemyRarity.Magic => magicChanceMultiplier,
                EnemyRarity.Rare => rareChanceMultiplier,
                EnemyRarity.Elite => eliteChanceMultiplier,
                EnemyRarity.Boss => bossChanceMultiplier,
                _ => normalChanceMultiplier
            };
        }

        private float GetAmountMultiplier(EnemyRarity rarity)
        {
            return rarity switch
            {
                EnemyRarity.Magic => magicAmountMultiplier,
                EnemyRarity.Rare => rareAmountMultiplier,
                EnemyRarity.Elite => eliteAmountMultiplier,
                EnemyRarity.Boss => bossAmountMultiplier,
                _ => normalAmountMultiplier
            };
        }

        private void OnValidate()
        {
            powerForMaxChanceBonus = Mathf.Max(0f, powerForMaxChanceBonus);
            baseGold = Mathf.Max(0, baseGold);
            goldPerPower = Mathf.Max(0f, goldPerPower);
            levelMultiplierPerLevel = Mathf.Max(0f, levelMultiplierPerLevel);
            minRandomAmountMultiplier = Mathf.Max(0f, minRandomAmountMultiplier);
            maxRandomAmountMultiplier = Mathf.Max(0f, maxRandomAmountMultiplier);
        }
    }
}
