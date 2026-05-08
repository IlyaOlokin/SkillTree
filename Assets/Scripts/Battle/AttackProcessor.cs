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
            AttackContext context = new AttackContext(attackerUnit, defender, damageInfo);
            
            //Evasion
            if (Evasion.ApplyEvasion(defender.UnitObject, attackerUnit))
            {
                context.IsEvaded = true;
                defender.OnHitEvaded(damageInfo.DamageInstance);
                AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
                return;
            }

            attackerUnit.OnHitLanded(defender);
            
            Overcharge.ApplyOverchargeEffect(context);
            Pain.ApplyPainEffect(context);
            RunModifiers(attackerUnit.GetAllModifiers(), ModifierPriority.OnAttack, attackerUnit, context);
            
            StatCalculator.LightRecalculateAttackStats(damageInfo.BaseUnitModifiers);
            
            DamageCalculator.CalculateAttackDamage(damageInfo);

            RunModifiers(defender.UnitObject.GetAllModifiers(), ModifierPriority.IncomingPreMitigation, defender.UnitObject, context);
            
            //Mitigation
            Armor.ApplyArmorMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            Resistance.ApplyResistanceMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
            
            RunModifiers(defender.UnitObject.GetAllModifiers(), ModifierPriority.OnGettingHit, defender.UnitObject, context);
            
            // Ailments
            Bleed.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Ignite.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Chill.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Overcharge.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Sunder.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Distract.Apply(attackerUnit, damageInfo, defender.UnitObject);
            Expose.Apply(attackerUnit, damageInfo, defender.UnitObject);

            //Block
            if (Block.ApplyBlock(defender.UnitObject))
            {
                context.IsBlocked = true;
                defender.OnHitBlock(damageInfo.DamageInstance);
                context.ConsumeQueuedEffects();
                AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
                return;
            }
            
            //Damage
            DamageInstance damageDealt = defender.ReceiveDamage(damageInfo);
            context.ResolveSuccessfulHitSideEffects();
            LifeSteal.Apply(attackerUnit, damageDealt);
            if (damageInfo.IsCritical)
            {
                attackerUnit.OnCritLanded(defender);
            }
            else
            {
                attackerUnit.OnNonCritLanded(defender);
            }
            attackerUnit.DamageDealt(damageDealt);
            context.ConsumeQueuedEffects();
            AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
        }

        private static void RunModifiers(List<CollectedModifier> mods, ModifierPriority priority, Unit owner, AttackContext context)
        {
            foreach (CollectedModifier mod in mods)
            {
                if (mod.IsInPriority(priority) && mod.IsApplicable(owner))
                {
                    mod.ApplyEffect(context);
                }
            }
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
