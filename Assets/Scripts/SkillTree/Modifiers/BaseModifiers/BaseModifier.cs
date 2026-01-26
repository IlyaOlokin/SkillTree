using System.Text;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Base Modifier", fileName = "New BaseModifier")]
    public class BaseModifier : Modifier
    {
        [SerializeField] public StatModifierAddedType addedModifierType;
        [SerializeField] public float addedValue;
        [SerializeField] public StatModifierIncreasedType increasedModifierType;
        [SerializeField] public float increasedValue;
        [SerializeField] public StatModifierMoreType moreModifierType;
        [SerializeField] public float moreValue;

        public override void ApplyEffect(Unit unit)
        {
            unit.baseUnitModifiers.ChangeIncreasedValue(increasedModifierType, increasedValue);
            unit.baseUnitModifiers.ChangeAddedValue(addedModifierType, addedValue);
            unit.baseUnitModifiers.AddMoreStatModifier(moreModifierType, moreValue);
        }

        public override string GetDescription()
        {
            StringBuilder builder = new StringBuilder();
            if (addedValue != 0) builder.Append("+")
                .Append(addedValue)
                .Append(" to ")
                .Append(addedModifierType.ToPrettyString().Replace("Added", ""));
            if (increasedValue != 0) builder.Append("+")
                .Append(increasedValue * 100f)
                .Append("% ")
                .Append(increasedModifierType.ToPrettyString());
            if (moreValue != 0) builder.Append("+")
                .Append(moreValue * 100f)
                .Append("% ")
                .Append(moreModifierType.ToPrettyString());
            
            return builder.ToString();
        }
        
    }
}