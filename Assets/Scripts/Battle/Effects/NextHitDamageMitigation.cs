using SkillTree;
using UnityEngine;

namespace Battle
{
    public class NextHitDamageMitigation : BaseEffect
    {
        private bool _isUsed = false;
        private DamageMitigation _modifier;

        public override bool IsStackable { get; set; }
        public override EffectVisualType VisualType => EffectVisualType.NextHitDamageMitigation;
        
        
        public NextHitDamageMitigation(Unit owner, DamageMitigation modifier)
        {
            owner.OnHit += (damageInstance) =>
            { 
                owner.RemoveOuterModifier(_modifier);
                _isUsed = true;
            };
            _modifier = modifier;
        }

        public override void OnApply(Unit unit)
        {
            unit.AddOuterModifier(_modifier);
        }
        
        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _isUsed;
        }
    }
}
