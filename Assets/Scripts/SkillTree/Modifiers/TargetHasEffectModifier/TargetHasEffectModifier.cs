using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Modifier Container If Target Has Effect", fileName = "New ModifierContainerIfTargetHasEffect")]
    public class TargetHasEffectModifier : Modifier
    {
        [SerializeField] private ModifierContainer modifierContainer;
        [SerializeField] private EffectVisualType targetEffect = EffectVisualType.Bleed;

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            Unit targetUnit = damageInfo?.Target?.UnitObject;
            if (targetUnit?.effectController == null || modifierContainer == null)
            {
                return;
            }

            if (!targetUnit.effectController.HasEffectOfVisualType(targetEffect))
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(modifierContainer);
        }

        public override void ApplyEffect(DamageInfo damageInfo, ModifierPowerContext powerContext)
        {
            Unit targetUnit = damageInfo?.Target?.UnitObject;
            if (targetUnit?.effectController == null || modifierContainer == null)
            {
                return;
            }

            if (!targetUnit.effectController.HasEffectOfVisualType(targetEffect))
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(powerContext.Scale(modifierContainer));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.modifierContainerIfTargetHasEffect.noModifier",
                    "Applies modifier if target has effect");
            }

            string plainEffectName = GameLocalization.GetContent(
                $"effect.{targetEffect}.name",
                GameLocalization.HumanizeIdentifier(targetEffect.ToString()));
            string effectName = GameLocalization.GetModifier(
                $"effect.{targetEffect}.linkedName",
                plainEffectName);

            return GameLocalization.FormatModifier(
                "modifier.modifierContainerIfTargetHasEffect.description",
                "[[0]] if target has [[1]]",
                powerContext.Scale(modifierContainer).GetDescription(),
                effectName);
        }
    }
}
