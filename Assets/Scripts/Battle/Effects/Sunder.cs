using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Sunder : BaseEffect
    {
        private const float BASE_DURATION = 5f;
        private const float BASE_REDUCED_ARMOR = -0.2f;

        private BaseModifier _cachedArmorModifier;
        private float _armorReduction;
        private readonly List<ModifierContainer> _additionalModifierContainers = new List<ModifierContainer>();
        private readonly List<BaseModifier> _cachedAdditionalModifiers = new List<BaseModifier>();

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Sunder;

        private Sunder(DamageInfo damageInfo, Unit defender)
        {
            Duration = BASE_DURATION;
            _armorReduction = CalculateArmorReduction(damageInfo, defender);
            CopyModifierContainers(damageInfo.AttackEffectPayload.GetEffectModifiers<Sunder>());
        }

        public override void OnApply(Unit unit)
        {
            ApplyModifiers(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);

            if (!(newEffect is Sunder sunder))
            {
                return;
            }

            RemoveModifiers(unit);
            _armorReduction = sunder._armorReduction;
            CopyModifierContainers(sunder._additionalModifierContainers);
            ApplyModifiers(unit);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifiers(unit);
        }

        private void ApplyModifiers(Unit unit)
        {
            _cachedArmorModifier = CreateRuntimeModifier<BaseModifier>();
            _cachedArmorModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.Armor, _armorReduction);
            unit.AddOuterModifier(_cachedArmorModifier);

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
            if (_cachedArmorModifier != null)
            {
                unit.RemoveOuterModifier(_cachedArmorModifier);
                ReleaseRuntimeModifier(_cachedArmorModifier);
                _cachedArmorModifier = null;
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

        private static float CalculateArmorReduction(DamageInfo damageInfo, Unit defender)
        {
            float mitigation = Mathf.Clamp01(defender.BaseUnitModifiers.GetStatValue(StatType.SunderMitigation));
            float power = Mathf.Max(0f, 1f + damageInfo.BaseUnitModifiers.GetStatValue(StatType.SunderPower));
            return BASE_REDUCED_ARMOR * power * (1f - mitigation);
        }

        public static void Apply(Unit attacker, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.AttackEffectPayload.IsSuppressed<Sunder>()) return;
            Unit effectTarget = damageInfo.AttackEffectPayload.IsRedirectedToOwner<Sunder>() ? attacker : defender;

            if (damageInfo.AttackEffectPayload.IsGuaranteed<Sunder>())
            {
                effectTarget.effectController.AddEffect(() => new Sunder(damageInfo, effectTarget), attacker);
                return;
            }

            float chance = Mathf.Clamp01(damageInfo.BaseUnitModifiers.GetStatValue(StatType.SunderChance));
            if (chance <= 0f)
            {
                return;
            }

            if (Random.Range(0f, 1f) < chance)
            {
                effectTarget.effectController.AddEffect(() => new Sunder(damageInfo, effectTarget), attacker);
            }
        }
    }
}
