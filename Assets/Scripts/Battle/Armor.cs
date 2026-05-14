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

        public static float CalculatePhysicalMitigation(float armor, float physicalDamage)
        {
            armor = Mathf.Max(0f, armor);
            physicalDamage = Mathf.Max(0f, physicalDamage);

            if (armor == 0f || physicalDamage == 0f)
                return 0f;

            return armor / (armor + physicalDamage);
        }
    }
}

