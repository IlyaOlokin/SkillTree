using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public abstract class AbsorptionEffect<T> : BaseEffect where T : AbsorptionEffect<T>
    {
        private int _stacks;

        public override bool IsStackable { get; set; } = true;
        public int Stacks => _stacks;

        private BaseModifier _ailmentGuardModifier;
        private const float AilmentGuardPerStack = -0.05f;
        private BaseModifier _secondaryModifier;
        private const float SecondaryPenaltyPerStack = -0.02f;
        private readonly StatType _secondaryStat;

        private bool _isReadyToBeRemoved;


        protected AbsorptionEffect(int stacks, StatType secondaryStat)
        {
            _stacks = Mathf.Max(0, stacks);
            _secondaryStat = secondaryStat;
        }

        public override void OnApply(Unit unit)
        {
            _ailmentGuardModifier = CreateRuntimeModifier<BaseModifier>();
            _ailmentGuardModifier.modifierContainer = new ModifierContainer(ModifierType.Added, StatType.AilmentGuard, AilmentGuardPerStack * _stacks);
            unit.AddOuterModifier(_ailmentGuardModifier);

            _secondaryModifier = CreateRuntimeModifier<BaseModifier>();
            _secondaryModifier.modifierContainer = new ModifierContainer(ModifierType.More, _secondaryStat, SecondaryPenaltyPerStack * _stacks);
            unit.AddOuterModifier(_secondaryModifier);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            unit.RemoveOuterModifier(_ailmentGuardModifier);
            unit.RemoveOuterModifier(_secondaryModifier);

            if (newEffect is T debuff)
            {
                _stacks = Mathf.Max(0, debuff.Stacks);
                _isReadyToBeRemoved = _stacks <= 0;
                if (_isReadyToBeRemoved) return;
            }

            _ailmentGuardModifier.modifierContainer.value = AilmentGuardPerStack * _stacks;
            unit.AddOuterModifier(_ailmentGuardModifier);

            _secondaryModifier.modifierContainer.value = SecondaryPenaltyPerStack * _stacks;
            unit.AddOuterModifier(_secondaryModifier);
        }

        public override bool IsReadyToBeRemoved(Unit unit) => _isReadyToBeRemoved;

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return _stacks > 1 ? _stacks.ToString() : string.Empty;
        }

        public override void OnRemove(Unit unit)
        {
            if (_ailmentGuardModifier != null)
            {
                unit.RemoveOuterModifier(_ailmentGuardModifier);
                _ailmentGuardModifier = null;
            }

            if (_secondaryModifier != null)
            {
                unit.RemoveOuterModifier(_secondaryModifier);
                _secondaryModifier = null;
            }
        }
    }
}
