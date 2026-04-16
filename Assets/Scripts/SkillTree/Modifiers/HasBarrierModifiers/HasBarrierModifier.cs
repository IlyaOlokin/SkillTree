using Battle;
using LocalizationSupport;
using UnityEngine;


namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/HasBarrierModifier", fileName = "New HasBarrierModifier")]
    public class HasBarrierModifier : Modifier
    {
            [SerializeField] public ModifierContainer modifierContainer;

            public override bool IsApplicable(Unit unit) => unit.barrier.HasBarrier;
            
            public override void ApplyEffect(Unit unit)
            {
                unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
            }

            public override string GetDescription()
            {
                if (modifierContainer == null)
                {
                    return GameLocalization.Get(
                        "modifier.hasBarrier.noModifier",
                        "While Barrier is active, applies modifier");
                }

                return GameLocalization.Format(
                    "modifier.hasBarrier.withModifier",
                    "While Barrier is active, [[0]]",
                    modifierContainer.GetDescription());
            }

        
    }
}
