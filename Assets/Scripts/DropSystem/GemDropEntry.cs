using System;
using Battle;
using Gems;
using InventorySystem;
using UnityEngine;

namespace DropSystem
{
    [Serializable]
    public class GemDropEntry
    {
        [SerializeField] private GemDefinition gemDefinition;
        [SerializeField] [Range(0f, 1f)] private float baseDropChance = 1f;
        [SerializeField] [Min(0f)] private float powerForMaxBonus = 100f;
        [SerializeField] [Range(0f, 1f)] private float maxPowerBonusChance;
        [SerializeField] [Range(0f, 5f)] private float normalRarityMultiplier = 1f;
        [SerializeField] [Range(0f, 5f)] private float magicRarityMultiplier = 1.1f;
        [SerializeField] [Range(0f, 5f)] private float rareRarityMultiplier = 1.25f;
        [SerializeField] [Range(0f, 5f)] private float eliteRarityMultiplier = 1.5f;
        [SerializeField] [Range(0f, 5f)] private float bossRarityMultiplier = 2f;

        public GemDefinition GemDefinition => gemDefinition;
        public float BaseDropChance => baseDropChance;

        public float CalculateDropChance(GemDropContext context = null)
        {
            if (gemDefinition == null || baseDropChance <= 0f)
                return 0f;

            float chance = baseDropChance;
            chance += EvaluatePowerBonus(context);
            chance *= GetRarityMultiplier(context != null ? context.Rarity : EnemyRarity.Normal);
            return Mathf.Clamp01(chance);
        }

        public bool ShouldDrop(GemDropContext context = null)
        {
            float dropChance = CalculateDropChance(context);
            if (dropChance <= 0f)
                return false;

            return UnityEngine.Random.value <= dropChance;
        }

        public InventoryItem CreateDroppedItem()
        {
            if (gemDefinition == null)
                return null;

            return InventoryItem.FromGem(gemDefinition.CreateInstance());
        }

        private float EvaluatePowerBonus(GemDropContext context)
        {
            if (context == null || maxPowerBonusChance <= 0f)
                return 0f;

            if (powerForMaxBonus <= 0f)
                return maxPowerBonusChance;

            float normalizedPower = Mathf.Clamp01(context.Power / powerForMaxBonus);
            return maxPowerBonusChance * normalizedPower;
        }

        private float GetRarityMultiplier(EnemyRarity rarity)
        {
            return rarity switch
            {
                EnemyRarity.Magic => magicRarityMultiplier,
                EnemyRarity.Rare => rareRarityMultiplier,
                EnemyRarity.Elite => eliteRarityMultiplier,
                EnemyRarity.Boss => bossRarityMultiplier,
                _ => normalRarityMultiplier
            };
        }
    }
}
