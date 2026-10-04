using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;

namespace Battle
{
    public sealed class FortificationEffect : BaseEffect
    {
        public const int MaxStacks = 10;
        public const float AddedPhysicalDamageMitigation = 0.05f;
        private BaseModifier _modifier;
        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.Fortification;
        public FortificationEffect()
        {
            Duration = 6f;
        }
        public override void OnApply(Unit unit)
        {
            _modifier = CreateRuntimeModifier<BaseModifier>();
            _modifier.modifierContainer = new ModifierContainer(
                ModifierType.Added, StatType.PhysicalDamageMitigation, AddedPhysicalDamageMitigation);
            unit.AddOuterModifier(_modifier);
        }
        public override void OnRemove(Unit unit)
        {
            if (_modifier != null) unit.RemoveOuterModifier(_modifier);
        }
        // BaseEffect's grouped timer selects the nearest expiry; all stacks last six seconds.
        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
            => (activeEffects?.Count ?? 0).ToString();
        protected override string GetDescriptionId() => "fortification";
        protected override string GetDisplayName()
            => GameLocalization.GetContent("effect.fortification.name", "Fortification");
        protected override string GetDescriptionFallback()
            => "Each stack grants 5% added Physical Damage Mitigation for 6 seconds. Maximum 10 stacks, each expiring independently. At maximum stacks, new restoration does not refresh them. The icon shows stack count; its border tracks the next expiry.";
    }
}
