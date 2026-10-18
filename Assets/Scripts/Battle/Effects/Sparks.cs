using SkillTree;

namespace Battle
{
    public sealed class Sparks : ElementalChargeEffect
    {
        public const string Description = "Each charge grants +10 Lightning Damage. Attacks consume 1 charge.";

        public Sparks(int stacks = 1) : base(StatType.LightningDamage, stacks) { }

        public override EffectVisualType VisualType => EffectVisualType.Sparks;
        protected override string GetDescriptionId() => "sparks";
        protected override string GetDescriptionFallback() => Description;
    }
}
