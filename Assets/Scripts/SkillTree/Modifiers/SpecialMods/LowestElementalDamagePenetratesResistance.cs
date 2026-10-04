using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Lowest Elemental Damage Penetrates Resistance", fileName = "LowestElementalDamagePenetratesResistance")]
    public class LowestElementalDamagePenetratesResistance : Modifier
    {
        public override bool IsInPriority(ModifierPriority priority) => priority == ModifierPriority.OnAttack;

        public override void ApplyEffect(AttackContext context)
        {
            context?.DamageInfo?.AttackEffectPayload?.EnableLowestElementalDamagePenetration();
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.lowestElementalDamagePenetratesResistance.description",
                "The two lowest-damage elements in each attack fully penetrate their Resistances.");
        }
    }
}
