using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Armor
    {
        public static void ApplyArmorMitigation(DamageInstance damage, Unit defender, Unit attacker)
        {
            float armor = defender.BaseUnitModifiers.GetStatValue(StatType.Armor);
            
            float K = damage.Damage[DamageType.Physical];
            if (armor == 0 || K == 0) return;
            damage.Damage[DamageType.Physical] *= K / (armor + K);
        }
    }
}

