using System.Collections.Generic;
using System;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public static class AttackProcessor
    {
        private static readonly Queue<Action> _postAttackActions = new Queue<Action>();
        private static readonly Stack<List<CollectedModifier>> _modifierBuffers = new Stack<List<CollectedModifier>>();
        private static readonly DamageType[] DamageTypes = (DamageType[])Enum.GetValues(typeof(DamageType));
        private static int _attackDepth;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _postAttackActions.Clear();
            _modifierBuffers.Clear();
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
#if UNITY_EDITOR
                int attackerStateHashBefore = attackerUnit.BaseUnitModifiers.ComputeDeterministicHash();
                int attackerStatRevisionBefore = attackerUnit.StatRevision;
#endif
                AttackContext context = new AttackContext(attackerUnit, defender, damageInfo);
                
                //Evasion
                if (Evasion.ApplyEvasion(defender.UnitObject, attackerUnit))
                {
                    context.IsEvaded = true;
                    defender.OnHitEvaded(damageInfo.DamageInstance);
                    attackerUnit.OnAttackMissed(defender);
#if UNITY_EDITOR
                    AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore, attackerStatRevisionBefore);
#endif
                    return;
                }

                attackerUnit.OnHitLanded(defender);
                
                Overcharge.ApplyOverchargeEffect(context);
                Pain.ApplyPainEffect(context);
                Vengeance.ApplyVengeanceEffect(context);
                RunModifiers(ModifierPriority.OnAttack, attackerUnit, context);

                StatCalculator.RecalculateAttackStat(damageInfo.BaseUnitModifiers, StatType.CritChance);
                DamageCalculator.RollCriticalHit(damageInfo);
                CriticalCharge.ApplyCriticalCharge(context);
                RunModifiers(ModifierPriority.AfterCriticalHit, attackerUnit, context);

                DamageCalculator.CalculateAttackDamage(damageInfo);

                RunModifiers(ModifierPriority.IncomingPreMitigation, defender.UnitObject, context);
                ApplyElementalDamageTakenAsPhysical(damageInfo.DamageInstance, defender.UnitObject.BaseUnitModifiers);
                
                //Mitigation
                Armor.ApplyArmorMitigation(damageInfo.DamageInstance, defender.UnitObject, attackerUnit);
                Resistance.ApplyResistanceMitigation(damageInfo, defender.UnitObject);
                MysticNegation.ApplyMysticNegationMitigation(damageInfo.DamageInstance, defender.UnitObject, context.AdditionalMysticNegation);
                ApplyDamageMitigation(damageInfo.DamageInstance, defender.UnitObject.BaseUnitModifiers);
                
                RunModifiers(ModifierPriority.OnGettingHit, defender.UnitObject, context);
                
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
                    damageInfo.IsBlocked = true;
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
#if UNITY_EDITOR
                AssertAttackerSnapshotIntegrity(attackerUnit, attackerStateHashBefore, attackerStatRevisionBefore);
#endif
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

        private static void ApplyElementalDamageTakenAsPhysical(DamageInstance damage, BaseUnitModifiers defenderModifiers)
        {
            float chance = Mathf.Clamp01(defenderModifiers.GetStatValue(StatType.ElementalDamageTakenAsPhysicalChance));
            var amounts = damage.Damage;
            float elemental = amounts[DamageType.Fire] + amounts[DamageType.Cold] + amounts[DamageType.Lightning];
            if (elemental <= 0f || chance <= 0f || (chance < 1f && UnityEngine.Random.Range(0f, 1f) >= chance))
                return;

            amounts[DamageType.Physical] += elemental;
            amounts[DamageType.Fire] = 0f;
            amounts[DamageType.Cold] = 0f;
            amounts[DamageType.Lightning] = 0f;
        }

        public static void ApplyDamageMitigation(DamageInstance damage, BaseUnitModifiers defenderModifiers)
        {
            foreach (DamageType damageType in DamageTypes)
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
            ModifierPriority priority,
            Unit owner,
            AttackContext context)
        {
            // Each phase gets its own snapshot, including during nested attacks.
            // Recollect to preserve callbacks changing modifiers between phases.
            var mods = _modifierBuffers.Count > 0 ? _modifierBuffers.Pop() : new List<CollectedModifier>();
            try
            {
                owner.GetAllModifiers(mods);
                foreach (CollectedModifier mod in mods)
                {
                    if (mod.IsInPriority(priority) && mod.IsApplicable(owner))
                    {
                        mod.ApplyEffect(context);
                    }
                }
            }
            finally
            {
                mods.Clear();
                _modifierBuffers.Push(mods);
            }
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private static void AssertAttackerSnapshotIntegrity(Unit attackerUnit, int attackerStateHashBefore, int statRevisionBefore)
        {
            // Paused/unregistered actors can legitimately rebuild during callbacks.
            if (attackerUnit.StatRevision != statRevisionBefore) return;
            int attackerStateHashAfter = attackerUnit.BaseUnitModifiers.ComputeDeterministicHash();
            Debug.Assert(
                attackerStateHashBefore == attackerStateHashAfter,
                "Attack pipeline mutated attacker BaseUnitModifiers. Mutate DamageInfo snapshot instead.");
        }
    }
}

