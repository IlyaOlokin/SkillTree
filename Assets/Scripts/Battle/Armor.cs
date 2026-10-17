using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class Armor
    {
        public static void ApplyArmorMitigation(DamageInstance damage, Unit defender, Unit attacker)
        {
            float armor = Mathf.Max(0f, defender.BaseUnitModifiers.GetStatValue(StatType.Armor));
            float physicalDamage = Mathf.Max(0f, damage.Damage[DamageType.Physical]);
            damage.Damage[DamageType.Physical] = armor == 0f || physicalDamage == 0f
                ? physicalDamage
                : physicalDamage * (physicalDamage / (armor + physicalDamage));
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

