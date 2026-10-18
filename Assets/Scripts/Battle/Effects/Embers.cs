using SkillTree;

namespace Battle
{
    public sealed class Embers : ElementalChargeEffect
    {
        public const string Description = "Each charge grants +10 Fire Damage. Attacks consume 1 charge.";

        public Embers(int stacks = 1) : base(StatType.FireDamage, stacks) { }

        public override EffectVisualType VisualType => EffectVisualType.Embers;
        protected override string GetDescriptionId() => "embers";
        protected override string GetDescriptionFallback() => Description;
    }
}
