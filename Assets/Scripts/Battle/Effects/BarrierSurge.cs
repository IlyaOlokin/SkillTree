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

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.BarrierRestorationPowerBuff;

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

        private static BaseModifier CreateModifier(StatType statType, float value)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(ModifierType.Increased, statType, value);
            return modifier;
        }
    }
}
