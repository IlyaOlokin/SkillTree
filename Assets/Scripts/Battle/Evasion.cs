using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Evasion
    {
        public static bool ApplyEvasion(DamageInstance damage, Unit defender, Unit attacker)
        {
            float evasion = StatCalculator.GetStat(defender,
                new List<StatModifierAddedType>() {StatModifierAddedType.AddedEvasion}, 
                new List<StatModifierIncreasedType>() {StatModifierIncreasedType.IncreasedEvasion}, 
                new List<StatModifierMoreType>() {StatModifierMoreType.MoreEvasion});
            
            float accuracy = StatCalculator.GetStat(defender,
                new List<StatModifierAddedType>() {StatModifierAddedType.AddedAccuracy}, 
                new List<StatModifierIncreasedType>() {StatModifierIncreasedType.IncreasedAccuracy}, 
                new List<StatModifierMoreType>() {StatModifierMoreType.MoreAccuracy});
            float chance = accuracy / (evasion + accuracy);
            return Random.Range(0f,1f) > chance;
        }
    }
}

