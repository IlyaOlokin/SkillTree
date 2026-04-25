using UnityEngine;

namespace Items
{
    public abstract class ConsumableItemDefinition : ItemDefinition
    {
        public sealed override bool CanBeUsed => true;
        public sealed override bool ConsumeOnUse => true;

        public sealed override bool TryUse(ItemUseContext context)
        {
            if (context?.Player == null)
                return false;

            return TryConsume(context);
        }

        protected abstract bool TryConsume(ItemUseContext context);
    }
}
