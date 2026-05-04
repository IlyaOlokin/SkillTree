using SkillTree;
using UnityEngine;

namespace Battle
{
    public class CriticalMomentum : BaseEffect
    {
        private readonly float _critChanceLossPerStack;
        private readonly float _critDamageBonusPerStack;

        private int _stacks;
        private BaseModifier _critChanceModifier;
        private BaseModifier _critDamageBonusModifier;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.CriticalMomentum;
        public int Stacks => _stacks;

        public CriticalMomentum(
            int stacks,
            float critChanceLossPerStack,
            float critDamageBonusPerStack)
        {
            _stacks = Mathf.Max(0, stacks);
            _critChanceLossPerStack = Mathf.Max(0f, critChanceLossPerStack);
            _critDamageBonusPerStack = Mathf.Max(0f, critDamageBonusPerStack);
        }

        public override void OnApply(Unit unit)
        {
            _critChanceModifier = CreateModifier(StatType.CritChance, 0f);
            _critDamageBonusModifier = CreateModifier(StatType.CritDamageBonus, 0f);

            ApplyCurrentStackValues(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            RemoveCurrentModifiers(unit);

            if (newEffect is CriticalMomentum criticalMomentum)
            {
                _stacks += Mathf.Max(0, criticalMomentum._stacks);
            }

            ApplyCurrentStackValues(unit);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveCurrentModifiers(unit);
            _critChanceModifier = null;
            _critDamageBonusModifier = null;
        }

        protected override string GetDescriptionId()
        {
            return "criticalMomentum";
        }

        private void ApplyCurrentStackValues(Unit unit)
        {
            if (_critChanceModifier == null || _critDamageBonusModifier == null || _stacks <= 0)
            {
                return;
            }

            _critChanceModifier.modifierContainer.value = -_critChanceLossPerStack * _stacks;
            _critDamageBonusModifier.modifierContainer.value = _critDamageBonusPerStack * _stacks;

            unit.AddOuterModifier(_critChanceModifier);
            unit.AddOuterModifier(_critDamageBonusModifier);
        }

        private void RemoveCurrentModifiers(Unit unit)
        {
            if (_critChanceModifier != null)
            {
                unit.RemoveOuterModifier(_critChanceModifier);
            }

            if (_critDamageBonusModifier != null)
            {
                unit.RemoveOuterModifier(_critDamageBonusModifier);
            }
        }

        private static BaseModifier CreateModifier(StatType statType, float value)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(ModifierType.More, statType, value);
            return modifier;
        }
    }
}
