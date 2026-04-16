using Battle;
using LocalizationSupport;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/LowLifeModifier", fileName = "New LowLifeModifier")]
    public class LowLifeModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;
        
        public override bool IsApplicable(Unit unit) => unit.IsOnLowLife();
        

        public override void ApplyEffect(Unit unit)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override string GetDescription()
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.lowLife.noModifier",
                    "While on Low Life, applies modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.lowLife.withModifier",
                "While on Low Life, [[0]]",
                modifierContainer.GetDescription());
        }

    }
}

