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
            
            float hitChance = accuracy / (evasion + accuracy);
            return Random.Range(0f,1f) > hitChance;
        }
    }
}

