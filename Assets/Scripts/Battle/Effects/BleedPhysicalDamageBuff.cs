using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class BleedPhysicalDamageBuff : BaseEffect
    {
        private readonly float _addedPhysicalDamage;
        private BaseModifier _physicalDamageModifier;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.BleedPhysicalDamageBuff;

        public BleedPhysicalDamageBuff(float duration, float addedPhysicalDamage)
        {
            Duration = Mathf.Max(0f, duration);
            _addedPhysicalDamage = Mathf.Max(0f, addedPhysicalDamage);
        }

        public override void OnApply(Unit unit)
        {
            if (unit == null || _addedPhysicalDamage <= 0f)
            {
                return;
            }

            _physicalDamageModifier = CreateRuntimeModifier<BaseModifier>();
            _physicalDamageModifier.modifierContainer =
                new ModifierContainer(ModifierType.Added, StatType.PhysicalDamage, _addedPhysicalDamage);
            unit.AddOuterModifier(_physicalDamageModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (unit == null || _physicalDamageModifier == null)
            {
                return;
            }

            unit.RemoveOuterModifier(_physicalDamageModifier);
            _physicalDamageModifier = null;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return activeEffects != null && activeEffects.Count > 1 ? activeEffects.Count.ToString() : string.Empty;
        }

        protected override string GetDescriptionId()
        {
            return "bleedPhysicalDamageBuff";
        }

        protected override string GetDescriptionFallback()
        {
            return "Grants [[0]] added {physical|Physical} {damage|Damage} for [[1]] seconds.";
        }

        protected override object[] GetDescriptionArguments()
        {
            return new object[]
            {
                FormatNumber(_addedPhysicalDamage),
                FormatNumber(Duration)
            };
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
