using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Block Restores Barrier", fileName = "New BlockRestoresBarrier")]
    public class BlockRestoresBarrier : Modifier
    {
        private const int BarrierRestoreAmount = 1;
        [SerializeField] private Sprite procIcon;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit?.barrier == null)
            {
                return null;
            }

            void HandleBlock()
            {
                if (unit.barrier.Restore(BarrierRestoreAmount) > 0)
                    unit.NotifyModifierProc(procIcon);
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnBlock += HandleBlock,
                () => unit.OnBlock -= HandleBlock);
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.blockRestoresBarrier.description",
                "After {block|Block}: restore 1 {barrier|Barrier}");
        }
    }
}
