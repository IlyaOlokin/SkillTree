using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Unblocked Hit Grants Block Power", fileName = "New UnblockedHitGrantsBlockPower")]
    public sealed class UnblockedHitGrantsBlockPower : Modifier
    {
        public const string Description = "When you receive an unblocked hit, gain 1 stack of Block Power Momentum. Each stack grants 10% increased {blockPower|Block Power}. Maximum 10 stacks. Blocking a hit removes all stacks.";

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit == null || unit.effectController == null) return null;
            EffectController controller = unit.effectController;

            void HandleGettingHit(DamageInfo damageInfo)
            {
                if (damageInfo == null || damageInfo.IsBlocked) return;
                // No repeat factory: gained self-buffs do not trigger received-effect repeats.
                controller.AddEffect(new BlockPowerMomentum());
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnGettingHit += HandleGettingHit,
                () => unit.OnGettingHit -= HandleGettingHit);
        }

        public override string GetDescription() => GameLocalization.GetModifier(
            "modifier.unblockedHitGrantsBlockPower.description", Description);
    }
}
