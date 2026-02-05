using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Attributes : MonoBehaviour
    {
        [Header("Strength")]
        [SerializeField] public List<ModifierContainer> baseModifiersStrength = new List<ModifierContainer>();
        
        
        [Header("Dexterity")]
        [SerializeField] public List<ModifierContainer> baseModifiersDexterity= new List<ModifierContainer>();

        
        [Header("Intelligence")]
        [SerializeField] public List<ModifierContainer> baseModifiersIntelligence = new List<ModifierContainer>();
    }
}

