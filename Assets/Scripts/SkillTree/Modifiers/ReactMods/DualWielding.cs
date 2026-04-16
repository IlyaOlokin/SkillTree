using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Dual Wielding", fileName = "New Dual Wielding")]
    public class DualWielding : Modifier
    {
        [SerializeField, Range(0f, 0.99f)] private float triggerThreshold = 0.8f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            Attacker attacker = unit.attacker;
            if (!attacker)
            {
                return null;
            }

            return new DelegateModifierRuntimeBinding(
                () => attacker.AddExtraAttackMoment(triggerThreshold),
                () => attacker.RemoveExtraAttackMoment(triggerThreshold));
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.dualWielding.description",
                "Dual Wielding: when attack progress reaches [[0]]%, perform an extra attack.",
                triggerThreshold * 100f);
        }
    }
}
