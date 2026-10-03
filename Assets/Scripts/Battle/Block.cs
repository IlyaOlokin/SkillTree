using UnityEngine;

namespace Battle
{
    public static class Block
    {
        private const float MaxBlockChance = 0.9f;
        private const float MaxBlockPower = 0.9f;

        public static bool ApplyBlock(DamageInstance damage, Unit defender)
        {
            if (damage?.Damage == null || defender?.BaseUnitModifiers == null)
            {
                return false;
            }

            float blockChance = Mathf.Clamp(defender.BaseUnitModifiers.GetStatValue(StatType.BlockChance), 0f, MaxBlockChance);
            if (Random.Range(0f, 1f) >= blockChance)
            {
                return false;
            }

            float blockPower = Mathf.Clamp(defender.BaseUnitModifiers.GetStatValue(StatType.BlockPower), 0f, MaxBlockPower);
            float damageMultiplier = 1f - blockPower;

            foreach (DamageType damageType in new System.Collections.Generic.List<DamageType>(damage.Damage.Keys))
            {
                damage.Damage[damageType] *= damageMultiplier;
            }

            return true;
        }
    }
}

