using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Attack Progress Cannot Be Modified", fileName = "New AttackProgressCannotBeModified")]
    public class AttackProgressCannotBeModified : Modifier
    {
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            Attacker attacker = unit.attacker;
            if (!attacker)
            {
                return null;
            }

            return new DelegateModifierRuntimeBinding(
                attacker.AddExternalAttackProgressLock,
                attacker.RemoveExternalAttackProgressLock);
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.attackProgressCannotBeModified.description",
                "You cannot gain or lose extra Attack Progress.");
        }
    }
}
