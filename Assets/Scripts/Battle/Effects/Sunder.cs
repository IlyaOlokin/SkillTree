using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Sunder : BaseEffect
    {
        private const float BASE_DURATION = 5f;
        private const float BASE_REDUCED_ARMOR = -0.2f;

        private BaseModifier _cachedModifier;
        private float _armorReduction;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Sunder;

        private Sunder(DamageInfo damageInfo, Unit defender)
        {
            Duration = BASE_DURATION;
            _armorReduction = CalculateArmorReduction(damageInfo, defender);
        }

        public override void OnApply(Unit unit)
        {
            _cachedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier.modifierContainer =
                new ModifierContainer(ModifierType.Increased, StatType.Armor, _armorReduction);
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            existing.TimeLeft = Mathf.Max(newEffect.Duration, existing.TimeLeft);

            if (!(newEffect is Sunder sunder))
            {
                return;
            }

            unit.RemoveOuterModifier(_cachedModifier);
            _armorReduction = sunder._armorReduction;
            _cachedModifier.modifierContainer.value = _armorReduction;
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (_cachedModifier != null)
            {
                unit.RemoveOuterModifier(_cachedModifier);
            }
        }

        private static float CalculateArmorReduction(DamageInfo damageInfo, Unit defender)
        {
            float mitigation = Mathf.Clamp01(defender.BaseUnitModifiers.GetStatValue(StatType.SunderMitigation));
            float magnitude = Mathf.Max(0f, 1f + damageInfo.BaseUnitModifiers.GetStatValue(StatType.SunderMagnitude));
            return BASE_REDUCED_ARMOR * magnitude * (1f - mitigation);
        }

        public static void Apply(Unit _, DamageInfo damageInfo, Unit defender)
        {
            if (damageInfo.AttackEffectPayload.IsGuaranteed<Sunder>())
            {
                defender.effectController.AddEffect(new Sunder(damageInfo, defender));
                return;
            }

            float chance = Mathf.Clamp01(damageInfo.BaseUnitModifiers.GetStatValue(StatType.SunderChance));
            if (chance <= 0f)
            {
                return;
            }

            if (Random.Range(0f, 1f) < chance)
            {
                defender.effectController.AddEffect(new Sunder(damageInfo, defender));
            }
        }
    }
}
