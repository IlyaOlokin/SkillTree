using System.Collections.Generic;
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

        private readonly List<ModifierContainer> _modifierContainers = new List<ModifierContainer>();
        private readonly List<BaseModifier> _cachedModifiers = new List<BaseModifier>();
        
        private Chill(DamageInfo damageInfo, Unit defender, float duration)
        {
            Duration = duration * (1f - defender.BaseUnitModifiers.GetStatValue(StatType.ChillDurationReduction));
            CalculateChillPowerMultiplier(damageInfo);
            SetModifierContainers(damageInfo.AttackEffectPayload.GetEffectModifiers<Chill>());
        }
        
        public override void OnApply(Unit unit)
        {
            ApplyModifiers(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);
            if (newEffect is Chill chill)
            {
                RemoveModifiers(unit);
                _chillPower = chill._chillPower;
                CopyModifierContainers(chill._modifierContainers);
                ApplyModifiers(unit);
            }
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifiers(unit);
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return string.Empty;
        }

        public void CalculateChillPowerMultiplier(DamageInfo damageInfo)
        {
            _chillPower = 1f + damageInfo.BaseUnitModifiers.GetStatValue(StatType.ChillPower);
        }

        private void SetModifierContainers(IReadOnlyList<ModifierContainer> additionalModifiers)
        {
            _modifierContainers.Clear();
            _modifierContainers.Add(new ModifierContainer(
                ModifierType.Increased,
                StatType.AttackSpeed,
                ScaleByChillPower(CHILL_BASE_SLOW)));

            for (int i = 0; i < additionalModifiers.Count; i++)
            {
                ModifierContainer modifier = additionalModifiers[i];
                _modifierContainers.Add(new ModifierContainer(
                    modifier.modifierType,
                    modifier.statType,
                    ScaleByChillPower(modifier.value)));
            }
        }

        private void CopyModifierContainers(IReadOnlyList<ModifierContainer> modifiers)
        {
            _modifierContainers.Clear();
            for (int i = 0; i < modifiers.Count; i++)
            {
                ModifierContainer modifier = modifiers[i];
                _modifierContainers.Add(new ModifierContainer(
                    modifier.modifierType,
                    modifier.statType,
                    modifier.value));
            }
        }

        private void ApplyModifiers(Unit unit)
        {
            for (int i = 0; i < _modifierContainers.Count; i++)
            {
                BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
                ModifierContainer modifierContainer = _modifierContainers[i];
                modifier.modifierContainer = new ModifierContainer(
                    modifierContainer.modifierType,
                    modifierContainer.statType,
                    modifierContainer.value);
                _cachedModifiers.Add(modifier);
                unit.AddOuterModifier(modifier);
            }
        }

        private void RemoveModifiers(Unit unit)
        {
            for (int i = 0; i < _cachedModifiers.Count; i++)
            {
                unit.RemoveOuterModifier(_cachedModifiers[i]);
            }

            _cachedModifiers.Clear();
        }

        private float ScaleByChillPower(float value)
        {
            return value * _chillPower;
        }
        
        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.AttackEffectPayload.IsSuppressed<Chill>()) return;
            if (damageInfo.DamageInstance.Damage[DamageType.Cold] <= 0) return;
            float damagePercentOfMaxHealth = damageInfo.DamageInstance.Damage[DamageType.Cold] / defender.health.MaxHealth;
            damagePercentOfMaxHealth *= 1 + damageInfo.BaseUnitModifiers.GetStatValue(StatType.ChillChance);
            if (Random.Range(0f, 1f) < damagePercentOfMaxHealth)
            {
                Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Chill>() ? attacker : defender;
                effectTarget.effectController.AddEffect(() => new Chill(damageInfo, effectTarget, BASE_DURATION));
                attacker.AilmentApplied(effectTarget);
            }
        }
    }
}



