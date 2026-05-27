using SkillTree;
using UnityEngine;

namespace Battle
{
    public class NextHitDamageMitigation : BaseEffect
    {
        private readonly Unit _owner;
        private readonly DamageMitigation _modifier;
        private bool _isUsed;
        private bool _isApplied;
        private bool _isSubscribed;

        public override bool IsStackable { get; set; }
        public override EffectVisualType VisualType => EffectVisualType.NextHitDamageMitigation;
        
        
        public NextHitDamageMitigation(Unit owner, DamageMitigation modifier)
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
            }
        }

        private void HandleOwnerHit(DamageInfo _)
        {
            _isUsed = true;
        }
    }
}
