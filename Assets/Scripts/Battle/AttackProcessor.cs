using System.Collections.Generic;
using System;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class AttackProcessor
    {
        private static readonly Queue<Action> _postAttackActions = new Queue<Action>();
        private static int _attackDepth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _postAttackActions.Clear();
            _attackDepth = 0;
        }

        public static void HandleAttack(Unit attackerUnit, DamageInfo damageInfo, ITarget defender)
        {
            // Pin dynamic target providers once, including callers outside Attacker.
            Unit defenderUnit = defender?.UnitObject;
            if (defenderUnit == null)
            {
                return;
            }

            defender = defenderUnit;
            _attackDepth++;
            try
            {
                // All Modifiers are applied on unit update
                int attackerStateHashBefore = attackerUnit.BaseUnitModifiers.ComputeDeterministicHash(); // Diagnostics
                AttackContext context = new AttackContext(attackerUnit, defender, damageInfo);
                
                //Evasion
                if (Evasion.ApplyEvasion(defender.UnitObject, attackerUnit))
                {
                    context.IsEvaded = true;
                    defender.OnHitEvaded(damageInfo.DamageInstance);
                    attackerUnit.OnAttackMissed(defender);
                    AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore); // Diagnostics
                    return;
                }

                attackerUnit.OnHitLanded(defender);
                
                Overcharge.ApplyOverchargeEffect(context);
                Pain.ApplyPainEffect(context);
                Vengeance.ApplyVengeanceEffect(context);
                RunModifiers(attackerUnit.GetAllModifiers(), ModifierPriority.OnAttack, attackerUnit, context);

                StatCalculator.RecalculateAttackStat(damageInfo.BaseUnitModifiers, StatType.CritChance);
                DamageCalculator.RollCriticalHit(damageInfo);
                CriticalCharge.ApplyCriticalCharge(context);
                RunModifiers(attackerUnit.GetAllModifiers(), ModifierPriority.AfterCriticalHit, attackerUnit, context);

                DamageCalculator.CalculateAttackDamage(damageInfo);

                RunModifiers(defender.UnitObject.GetAllModifiers(), ModifierPriority.IncomingPreMitigation, defender.UnitObject, context);
                
                //Mitigation
                Armor.ApplyArmorMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
                Resistance.ApplyResistanceMitigation(damageInfo, defender.UnitObject);
                MysticNegation.ApplyMysticNegationMitigation(damageInfo.DamageInstance, defender.UnitObject, context.AdditionalMysticNegation);
                ApplyDamageMitigation(damageInfo.DamageInstance, defender.UnitObject.BaseUnitModifiers);
                
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
                if (Block.ApplyBlock(damageInfo.DamageInstance, defender.UnitObject, out float blockPower))
                {
                    context.IsBlocked = true;
                    defender.OnHitBlock(damageInfo.DamageInstance);
                    Parry.Apply(context, blockPower);
                    defender.UnitObject.NotifyBlockResolved(context);
                }
                
                //Damage
                DamageInstance damageDealt = defender.ReceiveDamage(damageInfo);
                Chill.TryUpgradeAppliedChillToFreeze(damageInfo, defender.UnitObject);
                context.ResolveSuccessfulHitSideEffects();
                LifeSteal.Apply(attackerUnit, damageInfo, damageDealt);
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
            finally
            {
                _attackDepth--;
                if (_attackDepth == 0)
                {
                    FlushPostAttackActions();
                }
            }
        }

        public static void ApplyDamageMitigation(DamageInstance damage, BaseUnitModifiers defenderModifiers)
        {
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                if (!damage.Damage.ContainsKey(damageType))
                    continue;

                float mitigation = defenderModifiers.GetStatValue(StatCalculator.GetCorrespondingDamageMitigationStat(damageType));
                damage.Damage[damageType] *= 1f - Mathf.Clamp01(mitigation);
            }
        }

        public static void RunAfterCurrentAttack(Action action)
        {
            if (action == null)
            {
                return;
            }

            if (_attackDepth > 0)
            {
                _postAttackActions.Enqueue(action);
                return;
            }

            action();
        }

        private static void FlushPostAttackActions()
        {
            while (_postAttackActions.Count > 0)
            {
                Action action = _postAttackActions.Dequeue();
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private static void RunModifiers(
            List<CollectedModifier> mods,
            ModifierPriority priority,
            Unit owner,
            AttackContext context)
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

