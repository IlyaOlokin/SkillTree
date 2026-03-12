using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/EvadeToMitigationCharges", fileName = "New Evade To Mitigation Charges")]
    public class EvadeToMitigationCharges : Modifier
    {
        [SerializeField] private DamageMitigation modifier;
        
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            void HandleEvade()
            {
                unit.effectController.AddEffect(new NextHitDamageMitigation(unit, modifier));
            }

            return new DelegateModifierRuntimeBinding(
                () => unit.OnEvade += HandleEvade,
                () => unit.OnEvade -= HandleEvade);
        }

        public override string GetDescription()
        {
            return "Each Evade grants a charge: take 10% less damage from the next hit";
        }
    }
}
