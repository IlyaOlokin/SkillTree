using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/LowLifeModifier", fileName = "New LowLifeModifier")]
    public class LowLifeModifier : SpecialModifier
    {
        [SerializeField] public StatModifierIncreasedType increasedModifierType;
        [SerializeField] public float increasedValue;
        
        public override bool IsApplicable(Unit unit) => unit.IsOnLowLife();
        
        public override void ApplyEffect(DamageInfo damageInfo)
        {
            damageInfo.BaseStatModifier.ChangeIncreasedValue(increasedModifierType, increasedValue);
        }
    }
}

