using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class BarrierSurge : BaseEffect
    {
        private readonly float _ailmentPowerIncrease;
        private readonly float _mysticDamageIncrease;
        private BaseModifier _ailmentPowerModifier;
        private BaseModifier _mysticDamageModifier;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.BarrierRestorationPowerBuff;
        public override bool CanDisplayMultipleIcons => false;

        public BarrierSurge(float duration, float ailmentPowerIncrease, float mysticDamageIncrease)
        {
            Duration = duration;
            _ailmentPowerIncrease = ailmentPowerIncrease;
            _mysticDamageIncrease = mysticDamageIncrease;
        }

        public override void OnApply(Unit unit)
        {
            _ailmentPowerModifier = CreateModifier(StatType.AilmentPower, _ailmentPowerIncrease);
            _mysticDamageModifier = CreateModifier(StatType.MysticDamage, _mysticDamageIncrease);

            unit.AddOuterModifier(_ailmentPowerModifier);
            unit.AddOuterModifier(_mysticDamageModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (_ailmentPowerModifier != null)
            {
                unit.RemoveOuterModifier(_ailmentPowerModifier);
            }

            if (_mysticDamageModifier != null)
            {
                unit.RemoveOuterModifier(_mysticDamageModifier);
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

        private static BaseModifier CreateModifier(StatType statType, float value)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(ModifierType.Increased, statType, value);
            return modifier;
        }
    }
}
