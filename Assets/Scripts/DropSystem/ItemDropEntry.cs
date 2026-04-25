using System;
using Battle;
using InventorySystem;
using Items;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace DropSystem
{
    [MovedFrom(true, "DropSystem", null, "GemDropEntry")]
    [Serializable]
    public class ItemDropEntry
    {
        [SerializeField] private ItemDefinition itemDefinition;
        [SerializeField] [Min(1)] private int stackSize = 1;
        [SerializeField] [Range(0f, 1f)] private float baseDropChance = 1f;
        [SerializeField] [Min(0f)] private float powerForMaxBonus = 100f;
        [SerializeField] [Range(0f, 1f)] private float maxPowerBonusChance;
        [SerializeField] [Range(0f, 5f)] private float normalRarityMultiplier = 1f;
        [SerializeField] [Range(0f, 5f)] private float magicRarityMultiplier = 1.1f;
        [SerializeField] [Range(0f, 5f)] private float rareRarityMultiplier = 1.25f;
        [SerializeField] [Range(0f, 5f)] private float eliteRarityMultiplier = 1.5f;
        [SerializeField] [Range(0f, 5f)] private float bossRarityMultiplier = 2f;

        public ItemDefinition ItemDefinition => itemDefinition;
        public float BaseDropChance => baseDropChance;

        public float CalculateDropChance(ItemDropContext context = null)
        {
            if (itemDefinition == null || baseDropChance <= 0f)
                return 0f;

            float chance = baseDropChance;
            chance += EvaluatePowerBonus(context);
            chance *= GetRarityMultiplier(context != null ? context.Rarity : EnemyRarity.Normal);
            return Mathf.Clamp01(chance);
        }

        public bool ShouldDrop(ItemDropContext context = null)
        {
            float dropChance = CalculateDropChance(context);
            if (dropChance <= 0f)
                return false;

            return UnityEngine.Random.value <= dropChance;
        }

        public InventoryItem CreateDroppedItem()
        {
            if (itemDefinition == null)
                return null;

            return InventoryItem.FromItemDefinition(itemDefinition, stackSize);
        }

        private float EvaluatePowerBonus(ItemDropContext context)
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
