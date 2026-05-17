using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Multi Crit", fileName = "New MultiCritModifier")]
    public class MultiCritModifier : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            if (context?.DamageInfo == null)
            {
                return;
            }

            context.DamageInfo.AllowsMultiCrit = true;
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.multiCrit.description",
                "Crit Chance can exceed 100%. Each full 100% Crit Chance grants another Crit Damage Bonus layer, and the remainder rolls for one additional layer.");
        }
    }
}
