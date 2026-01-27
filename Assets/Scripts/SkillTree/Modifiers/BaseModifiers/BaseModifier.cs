using System.Text;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Base Modifier", fileName = "New BaseModifier")]
    public class BaseModifier : Modifier
    {
        [SerializeField] public AddedBaseModifierContainer addedModifier;
        [SerializeField] public IncreasedBaseModifierContainer increasedModifier;
        [SerializeField] public MoreBaseModifierContainer moreModifier;

        public override void ApplyEffect(Unit unit)
        {
            unit.baseUnitModifiers.ChangeAddedValue(addedModifier.modifierType, addedModifier.value);
            unit.baseUnitModifiers.ChangeIncreasedValue(increasedModifier.modifierType, increasedModifier.value);
            unit.baseUnitModifiers.AddMoreStatModifier(moreModifier.modifierType, moreModifier.value);
        }

        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            if (addedModifier.value != 0) builder.Append("+")
                .Append(addedModifier.value)
                .Append(" to ")
                .Append(addedModifier.modifierType.ToPrettyString().Replace("Added", ""));
            if (increasedModifier.value != 0) builder.Append("+")
                .Append(increasedModifier.value * 100f)
                .Append("% ")
                .Append(increasedModifier.modifierType.ToPrettyString());
            if (moreModifier.value != 0) builder.Append("+")
                .Append(moreModifier.value * 100f)
                .Append("% ")
                .Append(moreModifier.modifierType.ToPrettyString());
            
            return builder.ToString();
        }
        
    }
}