using UnityEngine;

namespace Battle
{
    public static class Block
    {
        private const float MaxBlockChance = 0.9f;

        public static bool ApplyBlock(DamageInstance damage, Unit defender)
            => ApplyBlock(damage, defender, out _);

        public static bool ApplyBlock(DamageInstance damage, Unit defender, out float blockPower)
        {
            blockPower = 0f;
            if (damage?.Damage == null || defender?.BaseUnitModifiers == null)
            {
                return false;
            }

            float blockChance = Mathf.Clamp(defender.BaseUnitModifiers.GetStatValue(StatType.BlockChance), 0f, MaxBlockChance);
            if (Random.Range(0f, 1f) >= blockChance)
            {
                return false;
            }

            blockPower = Mathf.Max(0f, defender.BaseUnitModifiers.GetStatValue(StatType.BlockPower));
            ApplyBlockPower(damage, blockPower);
            return true;
        }

        internal static void ApplyBlockPower(DamageInstance damage, float blockPower)
        {
            if (damage?.Damage == null) return;
            float totalDamage = 0f;
            foreach (float component in damage.Damage.Values)
            {
                totalDamage += component;
            }

            float damageMultiplier = totalDamage > 0f
                ? Mathf.Max(0f, totalDamage - blockPower) / totalDamage
                : 0f;

            foreach (DamageType damageType in new System.Collections.Generic.List<DamageType>(damage.Damage.Keys))
            {
                damage.Damage[damageType] *= damageMultiplier;
            }

        }
    }
}

