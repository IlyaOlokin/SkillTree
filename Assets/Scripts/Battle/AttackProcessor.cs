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
            int attackerStateHashBefore = attackerUnit.BaseUnitModifiers.ComputeDeterministicHash(); // Diagnostics
            
            //Evasion
            if (Evasion.ApplyEvasion(defender.UnitObject, attackerUnit))
            {
                defender.OnHitEvaded(damageInfo.DamageInstance);
                AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
                return;
            }
            
            List<Modifier> mods = attackerUnit.GetAllModifiers();
            
            Overcharge.ApplyOverchargeEffect(defender, mods);
            
            foreach (Modifier mod in mods)
            {
                if (mod.IsInPriority(ModifierPriority.OnAttack) && mod.IsApplicable(attackerUnit)) mod.ApplyEffect(damageInfo);
            }
            
            DamageCalculator.CalculateAttackDamage(damageInfo);
            
            
            //Mitigation
            Armor.ApplyArmorMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            Resistance.ApplyResistanceMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            
            foreach (Modifier mod in defender.UnitObject.GetAllModifiers())
            {
                if (mod.IsInPriority(ModifierPriority.OnGettingHit) && mod.IsApplicable(defender.UnitObject)) mod.ApplyEffect(damageInfo);
            }
            
            // Ailments
            Bleed.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Ignite.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Chill.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);
            Overcharge.Apply(attackerUnit, damageInfo.DamageInstance, defender.UnitObject);

            //Block
            if (Block.ApplyBlock(defender.UnitObject))
            {
                defender.OnHitBlock(damageInfo.DamageInstance);
                AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
                return;
            }
            
            //Damage
            DamageInstance damageDealt = defender.ReceiveDamage(damageInfo.DamageInstance);
            LifeSteel.Apply(attackerUnit, damageDealt);
            attackerUnit.DamageDealt(damageDealt);
            AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void AssertAttackerSnapshotIntegrity(Unit attackerUnit, int attackerStateHashBefore)
        {
            int attackerStateHashAfter = attackerUnit.BaseUnitModifiers.ComputeDeterministicHash();
            Debug.Assert(
                attackerStateHashBefore == attackerStateHashAfter,
                "Attack pipeline mutated attacker BaseUnitModifiers. Mutate DamageInfo snapshot instead.");
        }
    }
}
