using System;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Repeat Received Effect Chance", fileName = "New RepeatReceivedEffectChance")]
    public class RepeatReceivedEffectChance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController effectController = unit.effectController;
            if (effectController == null)
            {
                return null;
            }

            float scaledChance = Mathf.Clamp01(powerContext.Scale(chance));

            void HandleEffectReceived(Func<BaseEffect> effectFactory)
            {
                if (effectFactory == null || scaledChance <= 0f)
                {
                    return;
                }

                if (UnityEngine.Random.Range(0f, 1f) >= scaledChance)
                {
                    return;
                }

                effectController.AddRepeatedEffect(effectFactory);
            }

            return new DelegateModifierRuntimeBinding(
                () => effectController.OnEffectReceived += HandleEffectReceived,
                () => effectController.OnEffectReceived -= HandleEffectReceived);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.repeatReceivedEffectChance.description",
                "[[0]]% chance when you receive an {effect|Effect} to receive it again",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(chance)) * 100f));
        }
    }
}
