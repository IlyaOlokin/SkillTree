using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Overcharge : BaseEffect
    {
        private const float BASE_MORE_DAMAGE_BONUS = 0.1f;
        private const float BASE_MORE_CRIT_DAMAGE_BONUS = 0.2f;
        public BaseModifier MoreDamage;
        public BaseModifier MoreCritDamageBonus;
        public bool IsUsed = false;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Overcharge;
        public override bool CanDisplayMultipleIcons => false;

        public Overcharge(DamageInfo damageInfo, Unit defender)
        {
            CalculateBonuses(damageInfo, defender);
        }
        
        private void CalculateBonuses(DamageInfo damageInfo, Unit defender)
        {
            MoreDamage = ScriptableObject.CreateInstance<BaseModifier>();
            MoreDamage.modifierContainer = new ModifierContainer(ModifierType.More, StatType.Damage, 
                BASE_MORE_DAMAGE_BONUS * (1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.OverchargePower)));
            MoreDamage.SetPriorities(new List<ModifierPriority>() { ModifierPriority.OnAttack });
            
            MoreCritDamageBonus = ScriptableObject.CreateInstance<BaseModifier>();
            MoreCritDamageBonus.modifierContainer = new ModifierContainer(ModifierType.More, StatType.CritDamageBonus, 
                BASE_MORE_CRIT_DAMAGE_BONUS * (1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.OverchargePower)));
            MoreCritDamageBonus.SetPriorities(new List<ModifierPriority>() { ModifierPriority.OnAttack });
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return IsUsed;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null)
            {
                return string.Empty;
            }

            int count = 0;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                if (activeEffects[i]?.Effect is Overcharge overcharge && !overcharge.IsUsed)
                {
                    count++;
                }
            }

            return count > 1 ? count.ToString() : string.Empty;
        }

        public override void Consume(Unit unit)
        {
            IsUsed = true;
        }

        public void ApplyAttackBonus(DamageInfo damageInfo)
        {
            if (IsUsed)
            {
                return;
            }

            MoreDamage?.ApplyEffect(damageInfo);
            MoreCritDamageBonus?.ApplyEffect(damageInfo);
        }

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.AttackEffectPayload.IsSuppressed<Overcharge>()) return;
            if (damageInfo.DamageInstance.Damage[DamageType.Lightning] <= 0) return;
            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Lightning] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.OverchargeChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                if (Random.Range(0f, 1f) < defender.BaseUnitModifiers.GetStatValue(StatType.OverchargeAvoidanceChance))
                    return;
                Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Overcharge>() ? attacker : defender;
                effectTarget.effectController.AddEffect(new Overcharge(damageInfo, effectTarget));
                attacker.AilmentApplied(effectTarget);
            }
        }
        
        public static void ApplyOverchargeEffect(AttackContext context)
        {
            if (context?.Defender?.UnitObject == null)
            {
                return;
            }

            ITarget defender = context.Defender;
            foreach (var overchargeEffect in defender.UnitObject.effectController.GetAllEffectsOfType<Overcharge>())
            {
                var overcharge = (Overcharge)overchargeEffect.Effect;
                overcharge.ApplyAttackBonus(context.DamageInfo);
                context.QueueEffectConsumption(defender.UnitObject, overchargeEffect);
            }
        }
    }
}

