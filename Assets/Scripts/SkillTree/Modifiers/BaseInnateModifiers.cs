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
        [SerializeField] public List<ModifierContainer> baseModifiers = new List<ModifierContainer>();

        public override void ApplyEffect(Unit unit)
        {
            foreach (var modifier in baseModifiers)
            {
                unit.BaseUnitModifiers.ChangeModifierValue(modifier);
            }
        }
        
        public void Clear()
        {
            baseModifiers.Clear();
        }

        public void AddModifier(ModifierContainer modifier)
        {
            baseModifiers.Add(new ModifierContainer(
                modifier.modifierType,
                modifier.statType,
                modifier.value));
        }

        public void AddRange(IEnumerable<ModifierContainer> modifiers)
        {
            foreach (var mod in modifiers)
                AddModifier(mod);
        }
    }
}
