using System.Collections.Generic;
using SkillTree;

namespace Battle
{
    public static class AttackProcessor
    {
        public static void HandleAttack(Unit attackerUnit, DamageInfo damageInfo, ITarget defender)
        {
            // All Modifiers are applied on unit update

            List<Modifier> mods = attackerUnit.GetAllModifiers();
            foreach (Modifier mod in mods)
            {
                if (mod.Priority == ModifierPriority.OnAttack && mod.IsApplicable(attackerUnit)) mod.ApplyEffect(damageInfo);
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
            
            defender.ReceiveDamage(damageInfo.DamageInstance);
        }
    }
}
