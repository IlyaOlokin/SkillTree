using SkillTree;
using UnityEngine;

namespace Battle
{
    public class NextHitDamageMitigation : BaseEffect
    {
        private readonly Unit _owner;
        private readonly BaseModifier _modifier;
        private bool _isUsed;
        private bool _isApplied;
        private bool _isSubscribed;

        public override bool IsStackable { get; set; }
        public override EffectVisualType VisualType => EffectVisualType.NextHitDamageMitigation;
        
        
        public NextHitDamageMitigation(Unit owner, BaseModifier modifier)
        {
            _owner = owner;
            _modifier = modifier;
        }

        public override void OnApply(Unit unit)
        {
            if (_isApplied)
                return;

            if (_owner != null && !_isSubscribed)
            {
                _owner.OnGettingHit += HandleOwnerHit;
                _isSubscribed = true;
            }

            unit.AddOuterModifier(_modifier);
            _isApplied = true;
            AttackProcessor.RunAfterCurrentAttack(unit.ProcessPendingModRecalculation);
        }
        
        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _isUsed;
        }

        public override void OnRemove(Unit unit)
        {
            if (_owner != null)
            {
                _owner.OnGettingHit -= HandleOwnerHit;
                _isSubscribed = false;
            }

            if (_isApplied)
            {
                unit.RemoveOuterModifier(_modifier);
                _isApplied = false;
                AttackProcessor.RunAfterCurrentAttack(unit.ProcessPendingModRecalculation);
            }
        }

        private void HandleOwnerHit(DamageInfo _)
        {
            if (_isUsed)
                return;

            _isUsed = true;
            foreach (ActiveEffect active in _owner.effectController.GetAllEffectsOfType<NextHitDamageMitigation>())
            {
                if (ReferenceEquals(active.Effect, this))
                {
                    _owner.effectController.RemoveEffect(active);
                    break;
                }
            }
        }
    }
}
