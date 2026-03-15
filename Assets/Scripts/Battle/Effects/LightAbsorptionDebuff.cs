using UnityEngine;

namespace Battle
{
    public class LightAbsorptionDebuff : BaseEffect
    {
        private int _stacks;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.LightAbsorptionDebuff;
        public int Stacks => _stacks;

        public LightAbsorptionDebuff(int stacks)
        {
            _stacks = Mathf.Max(0, stacks);
        }

        public override void OnApply(Unit unit) { }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is LightAbsorptionDebuff debuff)
            {
                _stacks = Mathf.Max(0, debuff._stacks);
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit) => false;
        public override void OnRemove(Unit unit) { }
    }
}
