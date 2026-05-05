using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Distract : BaseEffect
    {
        private const float BASE_DURATION = 5f;
        private const float BASE_REDUCED_ACCURACY = -0.2f;

        private BaseModifier _cachedModifier;
        private float _accuracyReduction;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Distract;

        private Distract(DamageInfo damageInfo, Unit defender)
        {
            Duration = BASE_DURATION;
            _accuracyReduction = CalculateAccuracyReduction(damageInfo, defender);
        }

        public override void OnApply(Unit unit)
        {
            _cachedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.Accuracy, _accuracyReduction);
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);

            if (!(newEffect is Distract distract))
            {
                return;
            }

            unit.RemoveOuterModifier(_cachedModifier);
            _accuracyReduction = distract._accuracyReduction;
            _cachedModifier.modifierContainer.value = _accuracyReduction;
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (_cachedModifier != null)
            {
                unit.RemoveOuterModifier(_cachedModifier);
            }
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return string.Empty;
        }

        private static float CalculateAccuracyReduction(DamageInfo damageInfo, Unit defender)
        {
            float mitigation = Mathf.Clamp01(defender.BaseUnitModifiers.GetStatValue(StatType.DistractMitigation));
            float power = Mathf.Max(0f, 1f + damageInfo.BaseUnitModifiers.GetStatValue(StatType.DistractPower));
            return BASE_REDUCED_ACCURACY * power * (1f - mitigation);
        }

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Distract>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Distract>())
            {
                effectTarget.effectController.AddEffect(new Distract(damageInfo, effectTarget));
                return;
            }

            float chance = Mathf.Clamp01(damageInfo.BaseUnitModifiers.GetStatValue(StatType.DistractChance));
            if (chance <= 0f)
            {
                return;
            }

            if (Random.Range(0f, 1f) < chance)
            {
                effectTarget.effectController.AddEffect(new Distract(damageInfo, effectTarget));
            }
        }
    }
}
