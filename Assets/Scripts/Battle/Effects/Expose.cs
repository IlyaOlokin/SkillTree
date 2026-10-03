using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Expose : BaseEffect
    {
        private const float BASE_DURATION = 5f;
        private const float BASE_REDUCED_AILMENT_GUARD = -0.3f;

        private BaseModifier _cachedAilmentGuardModifier;
        private float _ailmentGuardReduction;
        private readonly List<ModifierContainer> _additionalModifierContainers = new List<ModifierContainer>();
        private readonly List<BaseModifier> _cachedAdditionalModifiers = new List<BaseModifier>();

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Expose;

        private Expose(DamageInfo damageInfo, Unit defender)
        {
            Duration = BASE_DURATION;
            _ailmentGuardReduction = CalculateAilmentGuardReduction(damageInfo, defender);
            CopyModifierContainers(damageInfo.AttackEffectPayload.GetEffectModifiers<Expose>());
        }

        public override void OnApply(Unit unit)
        {
            ApplyModifiers(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);

            if (!(newEffect is Expose expose))
            {
                return;
            }

            RemoveModifiers(unit);
            _ailmentGuardReduction = expose._ailmentGuardReduction;
            CopyModifierContainers(expose._additionalModifierContainers);
            ApplyModifiers(unit);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifiers(unit);
        }

        private void ApplyModifiers(Unit unit)
        {
            _cachedAilmentGuardModifier = CreateRuntimeModifier<BaseModifier>();
            _cachedAilmentGuardModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.AilmentGuard, _ailmentGuardReduction);
            unit.AddOuterModifier(_cachedAilmentGuardModifier);

            for (int i = 0; i < _additionalModifierContainers.Count; i++)
            {
                BaseModifier modifier = CreateRuntimeModifier<BaseModifier>();
                modifier.modifierContainer = CloneModifierContainer(_additionalModifierContainers[i]);
                _cachedAdditionalModifiers.Add(modifier);
                unit.AddOuterModifier(modifier);
            }
        }

        private void RemoveModifiers(Unit unit)
        {
            if (_cachedAilmentGuardModifier != null)
            {
                unit.RemoveOuterModifier(_cachedAilmentGuardModifier);
                ReleaseRuntimeModifier(_cachedAilmentGuardModifier);
                _cachedAilmentGuardModifier = null;
            }

            for (int i = 0; i < _cachedAdditionalModifiers.Count; i++)
            {
                unit.RemoveOuterModifier(_cachedAdditionalModifiers[i]);
                ReleaseRuntimeModifier(_cachedAdditionalModifiers[i]);
            }

            _cachedAdditionalModifiers.Clear();
        }

        private void CopyModifierContainers(IReadOnlyList<ModifierContainer> modifierContainers)
        {
            _additionalModifierContainers.Clear();
            for (int i = 0; i < modifierContainers.Count; i++)
            {
                _additionalModifierContainers.Add(CloneModifierContainer(modifierContainers[i]));
            }
        }

        private static ModifierContainer CloneModifierContainer(ModifierContainer modifierContainer)
        {
            return new ModifierContainer(
                modifierContainer.modifierType,
                modifierContainer.statType,
                modifierContainer.value);
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
            if (damageInfo.AttackEffectPayload.IsSuppressed<Expose>()) return;
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Expose>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Expose>())
            {
                effectTarget.effectController.AddEffect(() => new Expose(damageInfo, effectTarget), attacker);
                return;
            }

            float chance = Mathf.Clamp01(damageInfo.BaseUnitModifiers.GetStatValue(StatType.ExposeChance));
            if (chance <= 0f)
            {
                return;
            }

            if (Random.Range(0f, 1f) < chance)
            {
                effectTarget.effectController.AddEffect(() => new Expose(damageInfo, effectTarget), attacker);
            }
        }
    }
}
