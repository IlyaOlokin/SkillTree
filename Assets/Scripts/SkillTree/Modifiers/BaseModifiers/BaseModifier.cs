using System.Text;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Base Modifier", fileName = "New BaseModifier")]
    public class BaseModifier : Modifier
    {
        [SerializeField] public ModifierContainer modifierContainer;

        public override void ApplyEffect(Unit unit)
        {
            unit.baseUnitModifiers.ChangeModifierValue(modifierContainer);
        }
        
        public override void ApplyEffect(DamageInfo damageInfo)
        {
            damageInfo.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override string GetDescription()
        {
            return modifierContainer.GetDescription();
        }
        
    }
}