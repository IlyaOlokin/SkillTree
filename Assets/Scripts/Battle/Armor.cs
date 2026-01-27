using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Armor
    {
        public static void ApplyArmorMitigation(DamageInstance damage, Unit defender, Unit attacker)
        {
            float armor = StatCalculator.GetStat(defender,
                new List<StatModifierAddedType>() {StatModifierAddedType.AddedArmor}, 
                new List<StatModifierIncreasedType>() {StatModifierIncreasedType.IncreasedArmor}, 
                new List<StatModifierMoreType>() {StatModifierMoreType.MoreArmor});
            
            float K = damage.Damage[DamageType.Physical];
            damage.Damage[DamageType.Physical] *= K / (armor + K);
        }
    }
}

