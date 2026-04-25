using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container If Target Has Bleed", fileName = "New ModifierContainerIfTargetHasBleed")]
    public class TargetHasBleedModifier : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            Unit targetUnit = damageInfo.Target?.UnitObject;
            if (targetUnit?.effectController == null || modifierContainer == null)
            {
                return;
            }

            if (targetUnit.effectController.GetAllEffectsOfType<Bleed>().Count <= 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.modifierContainerIfTargetHasBleed.description",
                "[[0]] if target has {bleed|Bleed}",
                modifierContainer.GetDescription());
        }
    }
}
