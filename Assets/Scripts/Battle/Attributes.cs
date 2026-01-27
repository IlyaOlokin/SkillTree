using UnityEngine;

namespace Battle
{
    public class Attributes : MonoBehaviour
    {
        [SerializeField] private BaseAttributeModifiers baseAttributeModifiers;
        
        public BaseUnitModifiers StrengthModifiers;
        public BaseUnitModifiers DexterityModifiers;
        public BaseUnitModifiers IntelligenceModifiers;

        public void Awake()
        {
            StrengthModifiers = new BaseUnitModifiers();
            foreach (var mod in baseAttributeModifiers.increasedBaseModifiersStrength)
            {
                StrengthModifiers.ChangeIncreasedValue(mod.modifierType, mod.value);
            }
            foreach (var mod in baseAttributeModifiers.addedBaseModifiersStrength)
            {
                StrengthModifiers.ChangeAddedValue(mod.modifierType, mod.value);
            }
            
            DexterityModifiers = new BaseUnitModifiers();
            foreach (var mod in baseAttributeModifiers.increasedBaseModifiersDexterity)
            {
                DexterityModifiers.ChangeIncreasedValue(mod.modifierType, mod.value);
            }
            foreach (var mod in baseAttributeModifiers.addedBaseModifiersStrength)
            {
                DexterityModifiers.ChangeAddedValue(mod.modifierType, mod.value);
            }
            
            IntelligenceModifiers = new BaseUnitModifiers();
            foreach (var mod in baseAttributeModifiers.increasedBaseModifiersIntelligence)
            {
                IntelligenceModifiers.ChangeIncreasedValue(mod.modifierType, mod.value);
            }
            foreach (var mod in baseAttributeModifiers.addedBaseModifiersIntelligence)
            {
                IntelligenceModifiers.ChangeAddedValue(mod.modifierType, mod.value);
            }
        }
    }
}

