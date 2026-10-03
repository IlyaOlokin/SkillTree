using System;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Repeat Applied Overcharge Chance", fileName = "New RepeatAppliedOverchargeChance")]
    public class RepeatAppliedOverchargeChance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null)
            {
                return null;
            }

            float scaledChance = Mathf.Clamp01(powerContext.Scale(chance));

            void HandleEffectApplied(Unit target, Type effectType, Func<BaseEffect> effectFactory)
            {
                if (effectType != typeof(Overcharge) || target == null ||
                    target.effectController == null || effectFactory == null || scaledChance <= 0f)
                {
                    return;
                }

                if (scaledChance < 1f && UnityEngine.Random.Range(0f, 1f) >= scaledChance)
                {
                    return;
                }

                if (target.effectController.AddRepeatedEffect(effectFactory))
                {
                    unit.AilmentApplied(target);
                }
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEffectApplied += HandleEffectApplied,
                () => unit.OnEffectApplied -= HandleEffectApplied);
        }

        public override string GetDescription()
        {
            return GetDescription(ModifierPowerContext.None);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.repeatAppliedOverchargeChance.description",
                "[[0]]% chance when you apply {overcharge|Overcharge} to apply it again",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(chance)) * 100f));
        }
    }
}
