using System.Collections.Generic;
using LocalizationSupport;
using TMPro;
using TooltipSystem;
using UnityEngine;

namespace SkillTree
{
    // Use ordinary BaseModifier assets with additive scalar containers.
    public class InfiniteNode : Node
    {
        [SerializeField] private TMP_Text skillPointsText;
        public override bool IsInfinite => true;

        private void OnEnable()
        {
            OnNodeChanged += UpdateCounter;
            UpdateCounter(this);
        }

        private void OnDisable() => OnNodeChanged -= UpdateCounter;

        private void UpdateCounter(Node node)
        {
            if (skillPointsText != null)
                skillPointsText.text = InvestedSkillPoints.ToString();
        }

        public override IReadOnlyList<TooltipDescriptionLine> GetTooltipDescriptionLines()
        {
            var lines = new List<TooltipDescriptionLine>(base.GetTooltipDescriptionLines());
            lines.Add(TooltipDescriptionLine.Required(GameLocalization.Get(
                "node.infinite.description",
                "You can invest unlimited skill points in this node.")));
            return lines;
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            if (Modifiers == null) return;
            foreach (Modifier modifier in Modifiers)
                if (!Supports(modifier))
                    Debug.LogError("Infinite nodes require BaseModifier with Added or Increased scalar values (no type masks).", this);
        }

        public static bool Supports(Modifier modifier)
        {
            if (modifier == null || modifier.GetType() != typeof(BaseModifier)) return false;
            ModifierContainer container = ((BaseModifier)modifier).modifierContainer;
            return container != null
                && (container.modifierType == ModifierType.Added || container.modifierType == ModifierType.Increased)
                && container.statType != StatType.BarrierDamageTypeMask
                && container.statType != StatType.LifeStealTypeMask;
        }
    }
}
