using Battle;
using SkillTree;
using UnityEngine;

namespace SkillTree
{
    public class EvadeToMitigationCharges : Modifier
    {
        
        public override void ApplyEffect(Unit unit)
        {
            unit.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }
    }
}

