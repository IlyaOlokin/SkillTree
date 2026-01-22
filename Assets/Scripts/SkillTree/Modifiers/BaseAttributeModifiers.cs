using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Modifiers/Base Attribute Modifier", fileName = "New BaseAttributeModifier")]
    public class BaseAttributeModifiers : Modifier
    {   
        [Header("Strength")]
        [SerializeField] public List<AddedBaseModifierContainer> addedBaseModifiersStrength = new List<AddedBaseModifierContainer>();
        [SerializeField] public List<IncreasedBaseModifierContainer> increasedBaseModifiersStrength = new List<IncreasedBaseModifierContainer>();
        
        [Header("Dexterity")]
        [SerializeField] public List<AddedBaseModifierContainer> addedBaseModifiersDexterity = new List<AddedBaseModifierContainer>();
        [SerializeField] public List<IncreasedBaseModifierContainer> increasedBaseModifiersDexterity = new List<IncreasedBaseModifierContainer>();
        
        [Header("Intelligence")]
        [SerializeField] public List<AddedBaseModifierContainer> addedBaseModifiersIntelligence = new List<AddedBaseModifierContainer>();
        [SerializeField] public List<IncreasedBaseModifierContainer> increasedBaseModifiersIntelligence = new List<IncreasedBaseModifierContainer>();
    }
}
