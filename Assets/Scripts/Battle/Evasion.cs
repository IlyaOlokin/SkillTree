using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Evasion
    {
        public static bool ApplyEvasion(DamageInstance damage, Unit defender, Unit attacker)
        {
            float evasion = defender.baseUnitModifiers.StatValues[StatType.Evasion];
            
            float accuracy = attacker.baseUnitModifiers.StatValues[StatType.Accuracy];
            
            float hitChance = accuracy / (evasion + accuracy);
            return Random.Range(0f,1f) > hitChance;
        }
    }
}

