using Battle;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Chill : BaseEffect
    {
        public const float BASE_DURATION = 5f;
        public const float CHILL_BASE_SLOW = -0.5f;
        public override bool IsStackable { get; set; } = true;
        
        private float _chillPower;

        private BaseModifier _cachedModifier;
        
        private Chill(Unit owner, float duration)
        {
            Duration = duration;
            CalculateChillPower(owner);
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
            CalculateChillPower(unit);
        }

        public override void OnRemove(Unit unit)
        {
            unit.RemoveOuterModifier(_cachedModifier);
        }

        public void CalculateChillPower(Unit unit)
        {
            _chillPower = CHILL_BASE_SLOW * (1 + unit.BaseUnitModifiers.StatValues[StatType.ChillMagnitude]);
        }
        
        public static void Apply(Unit attacker, DamageInstance damageInstance, Unit defender)
        {
            if (damageInstance.Damage[DamageType.Cold] <= 0) return;
            float damagePercentOfMaxHealth = damageInstance.Damage[DamageType.Cold] / defender.health.MaxHealth;
            //damagePercentOfMaxHealth *= 1 + attacker.baseUnitModifiers.StatValues[StatType.IgniteChance];
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                defender.effectController.AddEffect(new Chill(attacker, BASE_DURATION));
            }
        }
    }
}



