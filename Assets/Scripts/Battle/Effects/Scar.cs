using System.Collections.Generic;
using System.Globalization;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Scar : BaseEffect
    {
        public float Amount { get; private set; }

        private BaseModifier _armorModifier;
        private BaseModifier _ailmentGuardModifier;
        private BaseModifier _healingReceivedModifier;
        private float _scarPercent;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Scar;
        public override bool CanDisplayMultipleIcons => true;

        public Scar(float duration, float amount)
        {
            Duration = Mathf.Max(0f, duration);
            Amount = Mathf.Max(0f, amount);
        }

        public override void OnApply(Unit unit)
        {
            _scarPercent = CalculateScarPercent(unit);
            if (_scarPercent <= 0f)
            {
                return;
            }

            _armorModifier = CreateModifier(ModifierType.Increased, StatType.Armor, _scarPercent);
            _ailmentGuardModifier = CreateModifier(ModifierType.Added, StatType.AilmentGuard, _scarPercent);
            _healingReceivedModifier = CreateModifier(ModifierType.Added, StatType.HealingReceived, -_scarPercent);

            unit.AddOuterModifier(_armorModifier);
            unit.AddOuterModifier(_ailmentGuardModifier);
            unit.AddOuterModifier(_healingReceivedModifier);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifiers(unit);
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return _scarPercent > 0f
                ? _scarPercent.ToString("0.#%", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        protected override string GetDescriptionId()
        {
            return "scar";
        }

        private void RemoveModifiers(Unit unit)
        {
            if (_armorModifier != null)
            {
                unit.RemoveOuterModifier(_armorModifier);
                _armorModifier = null;
            }

            if (_ailmentGuardModifier != null)
            {
                unit.RemoveOuterModifier(_ailmentGuardModifier);
                _ailmentGuardModifier = null;
            }

            if (_healingReceivedModifier != null)
            {
                unit.RemoveOuterModifier(_healingReceivedModifier);
                _healingReceivedModifier = null;
            }
        }

        private float CalculateScarPercent(Unit unit)
        {
            if (unit?.health == null || unit.health.MaxHealth <= 0f)
            {
                return 0f;
            }

            return Amount / unit.health.MaxHealth;
        }

        private static BaseModifier CreateModifier(ModifierType modifierType, StatType statType, float value)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(modifierType, statType, value);
            return modifier;
        }
    }
}
