using UnityEngine;

namespace Battle
{
    public static class EnemyPowerCalculator
    {
        public static float Calculate(float basePower, EnemyRarity rarity, GeneratedEnemyDefinition definition, bool applyRandomVariance = true)
        {
            float finalPower = Mathf.Max(0f, basePower);
            finalPower *= EnemyRarityHelper.GetMultiplier(rarity);

            if (definition != null)
                finalPower = definition.ApplyPowerMultiplier(finalPower);

            if (applyRandomVariance)
                finalPower *= Random.Range(0.9f, 1.1f);

            return finalPower;
        }
    }
}
