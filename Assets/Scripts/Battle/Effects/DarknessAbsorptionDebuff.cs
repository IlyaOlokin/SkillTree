using UnityEngine;

namespace Battle
{
    public class DarknessAbsorptionDebuff : BaseEffect
    {
        private int _stacks;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.DarknessAbsorptionDebuff;
        public int Stacks => _stacks;

        public DarknessAbsorptionDebuff(int stacks)
        {
            _stacks = Mathf.Max(0, stacks);
        }

        public override void OnApply(Unit unit) { }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is DarknessAbsorptionDebuff debuff)
            {
                _stacks = Mathf.Max(0, debuff._stacks);
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit) => false;
        public override void OnRemove(Unit unit) { }
    }
}
