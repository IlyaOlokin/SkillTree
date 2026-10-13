using System;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Bleed Grants Fortification Chance", fileName = "New BleedGrantsFortificationChance")]
    public sealed class BleedGrantsFortificationChance : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float chance = 0.2f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
            => CreateRuntimeBinding(unit, ModifierPowerContext.None);

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null || unit.effectController == null) return null;

            EffectController controller = unit.effectController;
            float scaledChance = Mathf.Clamp01(powerContext.Scale(chance));

            void HandleEffectApplied(Unit target, Type effectType, Func<BaseEffect> effectFactory)
            {
                if (effectType != typeof(Bleed) || scaledChance <= 0f) return;

                int count = 0;
                foreach (var active in controller.Effects)
                    if (active.Effect is FortificationEffect) count++;
                if (count >= unit.Fortification.Maximum) return;

                if (scaledChance < 1f && UnityEngine.Random.Range(0f, 1f) >= scaledChance) return;

                // Reuse the existing independent stack without attributing a self-buff.
                controller.AddEffect(new FortificationEffect());
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEffectApplied += HandleEffectApplied,
                () => unit.OnEffectApplied -= HandleEffectApplied);
        }

        public override string GetDescription() => GetDescription(ModifierPowerContext.None);

        public override string GetDescription(ModifierPowerContext powerContext)
            => GameLocalization.FormatModifier(
                "modifier.bleedGrantsFortificationChance.description",
                "When you apply {bleed|Bleed}, you have a [[0]]% chance to gain 1 stack of {fortification|Fortification}.",
                powerContext.HighlightValue(Mathf.Clamp01(powerContext.Scale(chance)) * 100f));
    }
}
