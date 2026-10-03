using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Critical Hits Deal Double Barrier Damage", fileName = "New CriticalHitsDealDoubleBarrierDamageModifier")]
    public class CriticalHitsDealDoubleBarrierDamageModifier : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.AfterCriticalHit;
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            if (damageInfo == null || !damageInfo.IsCritical)
            {
                return;
            }

            damageInfo.DealsDoubleDamageToBarrier = true;
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.criticalHitsDealDoubleBarrierDamage.description",
                "Critical hits deal double damage to {barrier|Barrier}.");
        }
    }
}
