using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class RelentlessMomentum : BaseEffect
    {
        private const int MaxStacks = 10;
        private const float AttackSpeedIncreasePerStack = 0.05f;
        private const float DamageIncreasePerStack = 0.05f;

        private int _stacks;
        private BaseModifier _attackSpeedModifier;
        private BaseModifier _damageModifier;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.RelentlessMomentum;
        public int Stacks => _stacks;

        public RelentlessMomentum(int stacks)
        {
            _stacks = Mathf.Clamp(stacks, 0, MaxStacks);
        }

        public override void OnApply(Unit unit)
        {
            _attackSpeedModifier = CreateModifier(StatType.AttackSpeed);
            _damageModifier = CreateModifier(StatType.Damage);

            ApplyCurrentStackValues(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            RemoveCurrentModifiers(unit);

            if (newEffect is RelentlessMomentum relentlessMomentum)
            {
                _stacks = Mathf.Clamp(_stacks + relentlessMomentum._stacks, 0, MaxStacks);
            }

            ApplyCurrentStackValues(unit);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveCurrentModifiers(unit);
            _attackSpeedModifier = null;
            _damageModifier = null;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return _stacks > 1 ? _stacks.ToString() : string.Empty;
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new[]
            {
                GameLocalization.FormatContent(
                    "effect.relentlessMomentum.description",
                    "Each stack grants [[0]]% increased Attack Speed and [[1]]% increased Damage. Maximum [[2]] stacks.",
                    AttackSpeedIncreasePerStack * 100f,
                    DamageIncreasePerStack * 100f,
                    MaxStacks)
            };
        }

        protected override string GetDescriptionId()
        {
            return "relentlessMomentum";
        }

        private void ApplyCurrentStackValues(Unit unit)
        {
            if (_attackSpeedModifier == null || _damageModifier == null || _stacks <= 0)
            {
                return;
            }

            _attackSpeedModifier.modifierContainer.value = AttackSpeedIncreasePerStack * _stacks;
            _damageModifier.modifierContainer.value = DamageIncreasePerStack * _stacks;

            unit.AddOuterModifier(_attackSpeedModifier);
            unit.AddOuterModifier(_damageModifier);
        }

        private void RemoveCurrentModifiers(Unit unit)
        {
            if (_attackSpeedModifier != null)
            {
                unit.RemoveOuterModifier(_attackSpeedModifier);
            }

            if (_damageModifier != null)
            {
                unit.RemoveOuterModifier(_damageModifier);
            }
        }

        private static BaseModifier CreateModifier(StatType statType)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(ModifierType.Increased, statType, 0f);
            return modifier;
        }
    }
}
