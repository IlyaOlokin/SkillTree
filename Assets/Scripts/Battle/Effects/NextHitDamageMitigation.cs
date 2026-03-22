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
        private readonly System.Action<DamageInstance> _onHitHandler;

        public override bool IsStackable { get; set; }
        public override EffectVisualType VisualType => EffectVisualType.NextHitDamageMitigation;
        
        
        public NextHitDamageMitigation(Unit owner, DamageMitigation modifier)
        {
            _owner = owner;
            _modifier = modifier;
            _onHitHandler = HandleOwnerHit;
            _owner.OnGettingHit += _onHitHandler;
        }

        public override void OnApply(Unit unit)
        {
            if (_isApplied)
                return;

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
                _owner.OnGettingHit -= _onHitHandler;

            if (_isApplied)
            {
                unit.RemoveOuterModifier(_modifier);
                _isApplied = false;
            }
        }

        private void HandleOwnerHit(DamageInstance _)
        {
            _isUsed = true;
        }
    }
}
