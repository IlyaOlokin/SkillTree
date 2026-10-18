using SkillTree;

namespace Battle
{
    public sealed class Rime : ElementalChargeEffect
    {
        public const string Description = "Each charge grants +10 Cold Damage. Attacks consume 1 charge.";

        public Rime(int stacks = 1) : base(StatType.ColdDamage, stacks) { }

        public override EffectVisualType VisualType => EffectVisualType.Rime;
        protected override string GetDescriptionId() => "rime";
        protected override string GetDescriptionFallback() => Description;
    }
}
