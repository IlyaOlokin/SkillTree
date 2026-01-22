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

        public override void ApplyEffect(Unit unit)
        {
            unit.baseUnitModifiers.ChangeIncreasedValue(increasedModifierType, increasedValue);
            unit.baseUnitModifiers.ChangeAddedValue(addedModifierType, addedValue);
            //unit.baseUnitModifiers.addedStatModifiers[addedModifierType].ChangeValue(addedValue);
            //unit.baseUnitModifiers.increasedStatModifiers[increasedModifierType].ChangeValue(increasedValue);
        }
    }
}