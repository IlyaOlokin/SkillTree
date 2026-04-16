using Battle;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Chill : BaseEffect
    {
        public const float BASE_DURATION = 3f;
        public const float CHILL_BASE_SLOW = -0.2f;
        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Chill;
        
        private float _chillPower;

        private BaseModifier _cachedModifier;
        
        private Chill(DamageInfo damageInfo, Unit defender, float duration)
        {
            Duration = duration * (1f - defender.BaseUnitModifiers.GetStatValue(StatType.ChillDurationReduction));
            CalculateChillPower(damageInfo);
        }
        
        public override void OnApply(Unit unit)
        {
            _cachedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.AttackSpeed, _chillPower);
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);
            if (newEffect is Chill chill)
            {
                _chillPower = chill._chillPower;
            }
        }

        public override void OnRemove(Unit unit)
        {
            unit.RemoveOuterModifier(_cachedModifier);
        }

        public void CalculateChillPower(DamageInfo damageInfo)
        {
            _chillPower = CHILL_BASE_SLOW * (1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.ChillPower));
        }
        
        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.DamageInstance.Damage[DamageType.Cold] <= 0) return;
            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Cold] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + attacker.BaseUnitModifiers.GetStatValue(StatType.ChillChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                defender.effectController.AddEffect(new Chill(damageInfo, defender, BASE_DURATION));
            }
        }
    }
}



