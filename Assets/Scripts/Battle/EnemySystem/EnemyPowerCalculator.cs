using UnityEngine;

namespace Battle
{
    public static class EnemyPowerCalculator
    {
        public static float Calculate(float basePower, EnemyRarity rarity, EnemyArchetype archetype, bool applyRandomVariance = true)
        {
            float finalPower = Mathf.Max(0f, basePower);
            finalPower *= EnemyRarityHelper.GetMultiplier(rarity);

            if (archetype != null)
                finalPower = archetype.ApplyPowerMultiplier(finalPower);

            if (applyRandomVariance)
                finalPower *= Random.Range(0.9f, 1.1f);

            return finalPower;
        }
    }
}
