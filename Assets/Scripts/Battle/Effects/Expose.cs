using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Expose : BaseEffect
    {
        private const float BASE_DURATION = 5f;
        private const float BASE_REDUCED_AILMENT_GUARD = -0.3f;

        private BaseModifier _cachedModifier;
        private float _ailmentGuardReduction;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Expose;

        private Expose(DamageInfo damageInfo, Unit defender)
        {
            Duration = BASE_DURATION;
            _ailmentGuardReduction = CalculateAilmentGuardReduction(damageInfo, defender);
        }

        public override void OnApply(Unit unit)
        {
            _cachedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.AilmentGuard, _ailmentGuardReduction);
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);

            if (!(newEffect is Expose expose))
            {
                return;
            }

            unit.RemoveOuterModifier(_cachedModifier);
            _ailmentGuardReduction = expose._ailmentGuardReduction;
            _cachedModifier.modifierContainer.value = _ailmentGuardReduction;
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

        private static float CalculateAilmentGuardReduction(DamageInfo damageInfo, Unit defender)
        {
            float mitigation = Mathf.Clamp01(defender.BaseUnitModifiers.GetStatValue(StatType.ExposeMitigation));
            float power = Mathf.Max(0f, 1f + damageInfo.BaseUnitModifiers.GetStatValue(StatType.ExposePower));
            return BASE_REDUCED_AILMENT_GUARD * power * (1f - mitigation);
        }

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Expose>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Expose>())
            {
                effectTarget.effectController.AddEffect(new Expose(damageInfo, effectTarget));
                return;
            }

            float chance = Mathf.Clamp01(damageInfo.BaseUnitModifiers.GetStatValue(StatType.ExposeChance));
            if (chance <= 0f)
            {
                return;
            }

            if (Random.Range(0f, 1f) < chance)
            {
                effectTarget.effectController.AddEffect(new Expose(damageInfo, effectTarget));
            }
        }
    }
}
