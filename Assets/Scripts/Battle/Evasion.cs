using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Evasion
    {
        public static bool ApplyEvasion(Unit defender, Unit attacker)
        {
            float evasion = defender.BaseUnitModifiers.GetStatValue(StatType.Evasion);
            
            float accuracy = attacker.BaseUnitModifiers.GetStatValue(StatType.Accuracy);
            float dodgeChance = CalculateDodgeChance(evasion, accuracy);
            return Random.Range(0f,1f) < dodgeChance;
        }

        public static float CalculateDodgeChance(float evasion, float accuracy)
        {
            float scaledEvasion = 0.6f * Mathf.Max(0f, evasion);
            return scaledEvasion / ((Mathf.Max(0f, accuracy) + 10f) + scaledEvasion);
        }
    }
}

