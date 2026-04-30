using SkillTree;
using UnityEngine;

namespace Battle
{
    public class EvasiveMomentum : BaseEffect
    {
        public const int MaxStacks = 3;

        private readonly float _evasionLossPerStack;
        private readonly float _critChancePerStack;

        private BaseModifier _evasionMoreModifier;
        private BaseModifier _critChanceMoreModifier;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.EvasiveMomentum;
        public int Stacks => 1;

        public EvasiveMomentum(
            float duration,
            float evasionLossPerStack,
            float critChancePerStack)
        {
            Duration = Mathf.Max(0f, duration);
            _evasionLossPerStack = Mathf.Max(0f, evasionLossPerStack);
            _critChancePerStack = Mathf.Max(0f, critChancePerStack);
        }

        public override void OnApply(Unit unit)
        {
            _evasionMoreModifier = CreateModifier(ModifierType.More, StatType.Evasion, 0f);
            _critChanceMoreModifier = CreateModifier(ModifierType.More, StatType.CritChance, 0f);

            ApplyCurrentStackValues(unit);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveCurrentModifiers(unit);
            _evasionMoreModifier = null;
            _critChanceMoreModifier = null;
        }

        protected override string GetDescriptionId()
        {
            return "evasiveMomentum";
        }

        private void ApplyCurrentStackValues(Unit unit)
        {
            if (_evasionMoreModifier == null || _critChanceMoreModifier == null)
            {
                return;
            }

            _evasionMoreModifier.modifierContainer.value = -_evasionLossPerStack;
            _critChanceMoreModifier.modifierContainer.value = _critChancePerStack;

            unit.AddOuterModifier(_evasionMoreModifier);
            unit.AddOuterModifier(_critChanceMoreModifier);
        }

        private void RemoveCurrentModifiers(Unit unit)
        {
            if (_evasionMoreModifier != null)
            {
                unit.RemoveOuterModifier(_evasionMoreModifier);
            }

            if (_critChanceMoreModifier != null)
            {
                unit.RemoveOuterModifier(_critChanceMoreModifier);
            }
        }

        private static BaseModifier CreateModifier(ModifierType modifierType, StatType statType, float value)
        {
            BaseModifier modifier = ScriptableObject.CreateInstance<BaseModifier>();
            modifier.modifierContainer = new ModifierContainer(modifierType, statType, value);
            return modifier;
        }
    }
}
