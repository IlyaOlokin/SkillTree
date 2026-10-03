using SkillTree;

namespace Battle
{
    public class LightAbsorption : AbsorptionEffect<LightAbsorption>
    {
        public override EffectVisualType VisualType => EffectVisualType.LightAbsorptionDebuff;

        public LightAbsorption(int stacks) : base(stacks, StatType.Accuracy)
        {
        }
    }
}
