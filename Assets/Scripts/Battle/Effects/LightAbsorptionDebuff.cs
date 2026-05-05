using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class LightAbsorptionDebuff : BaseEffect
    {
        private int _stacks;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.LightAbsorptionDebuff;
        public int Stacks => _stacks;
        
        private BaseModifier _cachedModifier1;
        private readonly ModifierContainer _modifierContainer1 = new ModifierContainer(ModifierType.Added, StatType.AilmentGuard, -0.05f);
        private BaseModifier _cachedModifier2;
        private readonly ModifierContainer _modifierContainer2 = new ModifierContainer(ModifierType.More, StatType.Accuracy, -0.02f);
        
        private bool _isReadyToBeRemoved;


        public LightAbsorptionDebuff(int stacks)
        {
            _stacks = Mathf.Max(0, stacks);
        }

        public override void OnApply(Unit unit)
        {
            _cachedModifier1 = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier1.modifierContainer = new ModifierContainer(_modifierContainer1.modifierType, _modifierContainer1.statType, _modifierContainer1.value * _stacks);
            unit.AddOuterModifier(_cachedModifier1);
            
            _cachedModifier2 = ScriptableObject.CreateInstance<BaseModifier>();
            _cachedModifier2.modifierContainer = new ModifierContainer(_modifierContainer2.modifierType, _modifierContainer2.statType, _modifierContainer2.value * _stacks);
            unit.AddOuterModifier(_cachedModifier2);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            unit.RemoveOuterModifier(_cachedModifier1);
            unit.RemoveOuterModifier(_cachedModifier2);
            
            if (newEffect is LightAbsorptionDebuff debuff)
            {
                if (debuff._stacks <= 0)
                {
                    _isReadyToBeRemoved = true;
                    return;
                }
                _stacks = Mathf.Max(0, debuff._stacks);
            }

            _cachedModifier1.modifierContainer.value = _modifierContainer1.value * _stacks;
            unit.AddOuterModifier(_cachedModifier1);
            
            _cachedModifier2.modifierContainer.value = _modifierContainer2.value * _stacks;
            unit.AddOuterModifier(_cachedModifier2);
        }

        public override bool IsReadyToBeRemoved(Unit unit) => _isReadyToBeRemoved;

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return _stacks > 1 ? _stacks.ToString() : string.Empty;
        }

        public override void OnRemove(Unit unit)
        {
            if (_cachedModifier1 != null)
            {
                unit.RemoveOuterModifier(_cachedModifier1);
                _cachedModifier1 = null;
            }

            if (_cachedModifier2 != null)
            {
                unit.RemoveOuterModifier(_cachedModifier2);
                _cachedModifier2 = null;
            }
        }
    }
}
