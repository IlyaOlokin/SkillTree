using SkillTree;
using UnityEngine;

namespace Battle
{
    public class NextAttackModifierEffect : BaseEffect
    {
        private readonly Unit _owner;
        private readonly Modifier _modifier;
        private readonly bool _ownsModifier;
        private bool _isUsed;
        private bool _isApplied;
        private bool _isSubscribed;

        public override bool IsStackable { get; set; } = true;
        
        public override EffectVisualType VisualType => EffectVisualType.NextAttackModifierEffect;
        

        public NextAttackModifierEffect(Unit owner, Modifier modifier, bool ownsModifier = false)
        {
            _owner = owner;
            _modifier = modifier;
            _ownsModifier = ownsModifier;
        }

        public override void OnApply(Unit unit)
        {
            if (_isApplied || _modifier == null)
            {
                return;
            }

            if (_owner != null && !_isSubscribed)
            {
                _owner.OnHit += HandleHit;
                _isSubscribed = true;
            }

            unit.AddOuterModifier(_modifier);
            _isApplied = true;
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _isUsed;
        }

        public override void OnRemove(Unit unit)
        {
            if (_owner != null)
            {
                _owner.OnHit -= HandleHit;
                _isSubscribed = false;
            }

            if (_isApplied && _modifier != null)
            {
                unit.RemoveOuterModifier(_modifier);
                _isApplied = false;
            }

            if (_ownsModifier && _modifier != null)
            {
                Object.Destroy(_modifier);
            }
        }

        private void HandleHit(ITarget _)
        {
            _isUsed = true;
        }
    }
}
