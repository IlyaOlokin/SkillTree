using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;

namespace Battle
{
    public sealed class BlockPowerMomentum : BaseEffect
    {
        public const int MaxStacks = 10;
        public const float BlockPowerIncreasePerStack = 0.1f;
        public const string Description = "Each stack grants 10% increased {blockPower|Block Power}. Maximum 10 stacks. Blocking a hit removes all stacks. No time limit.";

        private Unit _owner;
        private BaseModifier _blockPowerModifier;
        public int Stacks { get; private set; } = 1;
        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.BlockPowerMomentum;

        public override void OnApply(Unit unit)
        {
            _owner = unit;
            _blockPowerModifier = CreateRuntimeModifier<BaseModifier>();
            _blockPowerModifier.modifierContainer = new ModifierContainer(
                ModifierType.Increased, StatType.BlockPower, BlockPowerIncreasePerStack);
            unit.AddOuterModifier(_blockPowerModifier);
            unit.OnBlock += RemoveStacks;
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (!(newEffect is BlockPowerMomentum) || Stacks >= MaxStacks) return;
            unit.RemoveOuterModifier(_blockPowerModifier);
            Stacks++;
            _blockPowerModifier.modifierContainer.value = BlockPowerIncreasePerStack * Stacks;
            unit.AddOuterModifier(_blockPowerModifier);
        }

        private void RemoveStacks()
        {
            // Block has already captured/applied its power before this callback.
            _owner.effectController.RemoveEffectsOfType<BlockPowerMomentum>();
        }

        public override void OnRemove(Unit unit)
        {
            unit.OnBlock -= RemoveStacks;
            unit.RemoveOuterModifier(_blockPowerModifier);
            _blockPowerModifier = null;
            _owner = null;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects) => Stacks.ToString();
        protected override string GetDescriptionId() => "blockPowerMomentum";
        protected override string GetDescriptionFallback() => Description;
        protected override string GetDisplayName() => GameLocalization.GetContent(
            "effect.blockPowerMomentum.name", "Block Power Momentum");
    }
}
