using SkillTree;
using UnityEngine;

namespace Battle
{
    public class NextAttackModifierEffect : BaseEffect
    {
        private readonly Unit _owner;
        private readonly Modifier _modifier;
        private readonly bool _ownsModifier;
        private readonly System.Action<ITarget> _onHitHandler;
        private bool _isUsed;
        private bool _isApplied;

        public override bool IsStackable { get; set; } = true;
        
        public override EffectVisualType VisualType => EffectVisualType.NextAttackModifierEffect;
        

        public NextAttackModifierEffect(Unit owner, Modifier modifier, bool ownsModifier = false)
        {
            _owner = owner;
            _modifier = modifier;
            _ownsModifier = ownsModifier;
            _onHitHandler = HandleHit;
            _owner.OnHit += _onHitHandler;
        }

        public override void OnApply(Unit unit)
        {
            if (_isApplied || _modifier == null)
            {
                return;
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
                _owner.OnHit -= _onHitHandler;
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
