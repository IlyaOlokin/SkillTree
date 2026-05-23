using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Items/Node Targeted/Increase Node Power", fileName = "IncreaseNodePowerItem")]
    public sealed class IncreaseNodePowerItemDefinition : ItemDefinition
    {
        [SerializeField] [Min(0.01f)] private float powerAmount = 1f;

        public override bool CanBeUsedOnNode => true;
        public override bool ConsumeOnUse => true;

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new(base.GetTooltipDescriptions())
            {
                GameLocalization.FormatContent(
                    "item.increaseNodePower.description",
                    "Apply to a node that can change Power to increase its Power by [[0]].",
                    powerAmount)
            };

            descriptions.Add(GameLocalization.GetContent(
                "item.increaseNodePower.maxPermanentPower",
                "This item cannot raise a node's permanent {power|Power} above 100%."));

            return descriptions;
        }

        public override bool TryUseOnNode(ItemUseContext context, Node node)
        {
            if (context?.Player == null || node == null || !node.CanChangePower)
                return false;

            if (node.PermanentPower >= Node.MaxPermanentPower)
                return false;

            float previousPower = node.PermanentPower;
            node.IncreasePermanentPower(powerAmount);
            return !Mathf.Approximately(previousPower, node.PermanentPower);
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            powerAmount = Mathf.Max(0.01f, powerAmount);
        }
    }
}
