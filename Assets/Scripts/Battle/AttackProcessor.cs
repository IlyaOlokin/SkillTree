using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class AttackProcessor
    {
        public static void HandleAttack(Unit attackerUnit, DamageInfo damageInfo, ITarget defender)
        {
            // All Modifiers are applied on unit update
            
            List<Modifier> mods = attackerUnit.GetAllModifiers();
            
            Overcharge.ApplyOverchargeEffect(defender, mods);
            
            foreach (Modifier mod in mods)
            {
                if (mod.IsInPriority(ModifierPriority.OnAttack) && mod.IsApplicable(attackerUnit)) mod.ApplyEffect(damageInfo);
            }
            
            DamageCalculator.CalculateAttackDamage(damageInfo);
            
            // defence with Defences Defence(DamageInstance damageInfo)
            if (Evasion.ApplyEvasion(damageInfo.DamageInstance, defender.UnitObject, attackerUnit))
            {
                defender.OnEvaded(damageInfo.DamageInstance);
                return;
            }
            Armor.ApplyArmorMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            Resistance.ApplyResistanceMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            
            // Ailments
            Bleed.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Ignite.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Chill.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Overcharge.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            
            DamageInstance damageDealt = defender.ReceiveDamage(damageInfo.DamageInstance);
            LifeSteel.Apply(attackerUnit, damageDealt);
            attackerUnit.DamageDealt(damageDealt);
        }
    }
}
