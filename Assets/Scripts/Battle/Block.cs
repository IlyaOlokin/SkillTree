using UnityEngine;

namespace Battle
{
    public static class Block
    {
        public static bool ApplyBlock(Unit defender)
        {
            float blockChance = defender.BaseUnitModifiers.GetStatValue(StatType.BlockChance);
            
            return Random.Range(0f,1f) < blockChance;
        }
    }
}

