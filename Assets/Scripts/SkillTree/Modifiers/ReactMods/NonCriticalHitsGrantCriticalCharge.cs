using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Non-Critical Hits Grant Critical Charge", fileName = "NonCriticalHitsGrantCriticalCharge")]
    public class NonCriticalHitsGrantCriticalCharge : Modifier
    {
        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            if (unit.effectController == null) return null;
            void HandleNonCrit(ITarget target)
            {
                unit.effectController.AddEffect(() => new CriticalCharge());
            }
            return new DelegateModifierRuntimeBinding(
                () => unit.OnNonCrit += HandleNonCrit,
                () => unit.OnNonCrit -= HandleNonCrit);
        }

        public override string GetDescription() => GameLocalization.GetModifier(
            "modifier.nonCriticalHitsGrantCriticalCharge.description",
            "Non-critical hits grant a charge of {criticalCharge|Critical Charge}.");
    }
}
