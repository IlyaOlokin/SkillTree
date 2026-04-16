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

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.DamageInstance.Damage[DamageType.Lightning] <= 0) return;
            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Lightning] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + attacker.BaseUnitModifiers.GetStatValue(StatType.OverchargeChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                if (Random.Range(0f, 1f) < defender.BaseUnitModifiers.GetStatValue(StatType.OverchargeAvoidanceChance))
                    return;
                defender.effectController.AddEffect(new Overcharge(damageInfo, defender));
            }
        }
        
        public static void ApplyOverchargeEffect(ITarget defender, List<Modifier> mods)
        {
            foreach (var overchargeEffect in defender.UnitObject.effectController.GetAllEffectsOfType<Overcharge>())
            {
                var overcharge = (Overcharge)overchargeEffect.Effect;
                mods.Add(overcharge.MoreDamage);
                mods.Add(overcharge.MoreCritDamageBonus);
                overcharge.IsUsed = true;
            }
        }
    }
}

