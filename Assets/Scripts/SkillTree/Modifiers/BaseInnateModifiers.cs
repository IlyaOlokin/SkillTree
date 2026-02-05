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
                unit.baseUnitModifiers.SetModifierValue(modifier);
            }
        }
    }
}
