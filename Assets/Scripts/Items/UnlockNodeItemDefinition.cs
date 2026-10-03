using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Items/Node Targeted/Unlock Node", fileName = "UnlockNodeItem")]
    public sealed class UnlockNodeItemDefinition : ItemDefinition
    {
        public override bool CanBeUsedOnNode => true;
        public override bool ConsumeOnUse => true;

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new List<string>(base.GetTooltipDescriptions())
            {
                GameLocalization.GetContent("item.unlockNode.description",
                    "Apply to a locked node to unlock it. The node must still be learned separately.")
            };
        }

        public override bool CanUseOnNode(ItemUseContext context, Node node)
        {
            return context?.Player != null && node != null && node.IsLocked;
        }

        public override bool TryUseOnNode(ItemUseContext context, Node node)
        {
            return CanUseOnNode(context, node) && node.TryUnlock();
        }
    }
}
