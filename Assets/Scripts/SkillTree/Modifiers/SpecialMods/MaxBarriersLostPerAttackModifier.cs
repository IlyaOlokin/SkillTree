using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Max Barriers Lost Per Attack", fileName = "New MaxBarriersLostPerAttackModifier")]
    public class MaxBarriersLostPerAttackModifier : Modifier
    {
        [SerializeField, Min(1)] private int maxBarriersLostPerAttack = 1;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.IncomingPreMitigation;
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            if (damageInfo == null)
                return;

            // Multiple copies use the strictest limit. Node power does not scale this cap.
            damageInfo.MaxBarriersLostPerAttack = Mathf.Min(
                damageInfo.MaxBarriersLostPerAttack, Mathf.Max(1, maxBarriersLostPerAttack));
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.maxBarriersLostPerAttack.description",
                "A single attack against you can destroy at most [[0]] {barrier|Barriers}. Remaining damage passes through.",
                Mathf.Max(1, maxBarriersLostPerAttack));
        }
    }
}
