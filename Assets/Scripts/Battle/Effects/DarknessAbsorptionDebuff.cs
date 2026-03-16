using SkillTree;
using UnityEngine;

namespace Battle
{
    public class DarknessAbsorptionDebuff : BaseEffect
    {
        private int _stacks;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.DarknessAbsorptionDebuff;
        public int Stacks => _stacks;

        private BaseModifier _cachedModifier;
        
        private bool _isReadyToBeRemoved;

        public DarknessAbsorptionDebuff(int stacks)
        {
            _stacks = Mathf.Max(0, stacks);
        }

        public override void OnApply(Unit unit)
        {
            _cachedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier.modifierContainer = new ModifierContainer(ModifierType.Increased, StatType.AttackSpeed, -0.05f * _stacks);
            unit.AddOuterModifier(_cachedModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            unit.RemoveOuterModifier(_cachedModifier);
            if (newEffect is DarknessAbsorptionDebuff debuff)
            {
                if (debuff._stacks <= 0)
                {
                    _isReadyToBeRemoved = true;
                    return;
                }
                _stacks = Mathf.Max(0, debuff._stacks);
            }

            _cachedModifier.modifierContainer.value = -0.05f * _stacks;
            unit.AddOuterModifier(_cachedModifier);
        }

        public override bool IsReadyToBeRemoved(Unit unit) => _isReadyToBeRemoved;

        public override void OnRemove(Unit unit) { }
    }
}
