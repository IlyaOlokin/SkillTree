using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/LowLifeModifier", fileName = "New LowLifeModifier")]
    public class LowLifeModifier : SpecialModifier
    {
        [SerializeField] public ModifierContainer modifierContainer;
        
        public override bool IsApplicable(Unit unit) => unit.IsOnLowLife();
        

        public override void ApplyEffect(Unit unit)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

    }
}

