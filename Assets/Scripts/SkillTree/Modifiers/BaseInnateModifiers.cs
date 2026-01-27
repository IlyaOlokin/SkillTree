using System.Collections.Generic;
using Battle;
using SkillTree;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Base Innate Modifiers", fileName = "New BaseInnateModifiers")]

    public class BaseInnateModifiers : Modifier
    {
        [SerializeField] public List<AddedBaseModifierContainer> addedBaseModifiers = new List<AddedBaseModifierContainer>();
        [SerializeField] public List<IncreasedBaseModifierContainer> increasedBaseModifiers = new List<IncreasedBaseModifierContainer>();
        [SerializeField] public List<MoreBaseModifierContainer> moreBaseModifiers = new List<MoreBaseModifierContainer>();

        public override void ApplyEffect(BaseUnitModifiers baseUnitModifiers)
        {
            foreach (var modifier in addedBaseModifiers)
            {
                baseUnitModifiers.ChangeAddedValue(modifier.modifierType, modifier.value);
            }
            foreach (var modifier in increasedBaseModifiers)
            {
                baseUnitModifiers.ChangeIncreasedValue(modifier.modifierType, modifier.value);
            }
            foreach (var modifier in moreBaseModifiers)
            {
                baseUnitModifiers.AddMoreStatModifier(modifier.modifierType, modifier.value);
            }
        }
    }
}
