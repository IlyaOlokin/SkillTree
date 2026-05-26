using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Items/Node Targeted/Independent Node Allocation", fileName = "IndependentNodeAllocationItem")]
    public sealed class IndependentNodeAllocationItemDefinition : ItemDefinition
    {
        public override bool CanBeUsedOnNode => true;
        public override bool ConsumeOnUse => true;

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            List<string> descriptions = new(base.GetTooltipDescriptions())
            {
                GameLocalization.GetContent(
                    "item.independentNodeAllocation.description",
                    "Apply to an unallocated node to allocate it permanently without a root connection.")
            };

            return descriptions;
        }

        public override bool CanUseOnNode(ItemUseContext context, Node node)
        {
            if (context?.Player == null || node == null)
                return false;

            return node.CanBeIndependentlyAllocated();
        }

        public override bool TryUseOnNode(ItemUseContext context, Node node)
        {
            if (!CanUseOnNode(context, node))
                return false;

            return node.TryAllocateIndependently();
        }
    }
}
