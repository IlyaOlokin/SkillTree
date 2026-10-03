using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class BarrierSurge : BaseEffect
    {
        private readonly float _barrierRegenerationSpeedIncrease;
        private readonly float _elementalDamageIncrease;
        private BaseModifier _barrierRegenerationSpeedModifier;
        private BaseModifier _elementalDamageModifier;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.BarrierSurge;
        public override bool CanDisplayMultipleIcons => false;

        public BarrierSurge(float duration, float barrierRegenerationSpeedIncrease, float elementalDamageIncrease)
        {
            Duration = duration;
            _barrierRegenerationSpeedIncrease = barrierRegenerationSpeedIncrease;
            _elementalDamageIncrease = elementalDamageIncrease;
        }

        public override void OnApply(Unit unit)
        {
            _barrierRegenerationSpeedModifier = CreateModifier(StatType.BarrierRegenerationSpeed, _barrierRegenerationSpeedIncrease);
            _elementalDamageModifier = CreateModifier(StatType.ElementalDamage, _elementalDamageIncrease);

            unit.AddOuterModifier(_barrierRegenerationSpeedModifier);
            unit.AddOuterModifier(_elementalDamageModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (_barrierRegenerationSpeedModifier != null)
            {
                unit.RemoveOuterModifier(_barrierRegenerationSpeedModifier);
            }

            if (_elementalDamageModifier != null)
            {
                unit.RemoveOuterModifier(_elementalDamageModifier);
            }
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return activeEffects != null && activeEffects.Count > 1 ? activeEffects.Count.ToString() : string.Empty;
        }

        public override float GetIconTimerProgress(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (activeEffects == null || activeEffects.Count == 0)
            {
                return 1f;
            }

            float closestProgress = 1f;
            bool hasTimedEffect = false;
            for (int i = 0; i < activeEffects.Count; i++)
            {
                ActiveEffect activeEffect = activeEffects[i];
                if (activeEffect?.Effect == null || activeEffect.Effect.Duration <= 0f)
                {
                    continue;
                }

                float progress = activeEffect.TimeLeft / activeEffect.Effect.Duration;
                if (!hasTimedEffect || progress < closestProgress)
                {
                    closestProgress = progress;
                    hasTimedEffect = true;
                }
            }

            return hasTimedEffect ? closestProgress : 1f;
        }

        protected override object[] GetDescriptionArguments()
        {
            return new object[] { _barrierRegenerationSpeedIncrease * 100f, _elementalDamageIncrease * 100f, Duration };
        }

        private BaseModifier CreateModifier(StatType statType, float value)
        {
            BaseModifier modifier = CreateRuntimeModifier<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(ModifierType.Increased, statType, value);
            return modifier;
        }
    }
}
