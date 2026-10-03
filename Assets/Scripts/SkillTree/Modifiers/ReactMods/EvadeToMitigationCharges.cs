using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/EvadeToMitigationCharges", fileName = "New Evade To Mitigation Charges")]
    public class EvadeToMitigationCharges : Modifier
    {
        [SerializeField] private BaseModifier modifier;
        
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            void HandleEvade()
            {
                unit.effectController.AddEffect(() => new NextHitDamageMitigation(unit, modifier));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEvade += HandleEvade,
                () => unit.OnEvade -= HandleEvade);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.evadeToMitigationCharges.description",
                "Each Evade grants a charge: [[0]]% added Damage Mitigation per charge. All charges are removed after taking a hit.",
                (modifier != null && modifier.modifierContainer != null ? modifier.modifierContainer.value : 0f) * 100f);
        }
    }
}
