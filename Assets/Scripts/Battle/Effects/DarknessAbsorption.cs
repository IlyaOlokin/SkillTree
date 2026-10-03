using SkillTree;

namespace Battle
{
    public class DarknessAbsorption : AbsorptionEffect<DarknessAbsorption>
    {
        public override EffectVisualType VisualType => EffectVisualType.DarknessAbsorptionDebuff;

        public DarknessAbsorption(int stacks) : base(stacks, StatType.Damage)
        {
        }
    }
}
