using Battle;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Bleed Chance If Target Has No Bleed", fileName = "New BleedChanceIfTargetHasNoBleed")]
    public class BleedChanceIfTargetHasNoBleed : Modifier
    {
        [SerializeField, Range(0f, 1f)] private float addedBleedChance = 0.5f;

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            Unit targetUnit = damageInfo.Target?.UnitObject;
            if (targetUnit?.effectController == null)
            {
                return;
            }

            if (targetUnit.effectController.GetAllEffectsOfType<Bleed>().Count > 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, StatType.BleedChance, addedBleedChance));
        }

        public override string GetDescription()
        {
            return $"+{addedBleedChance * 100f:0.#}% {{Bleed}} Chance if target has no {{Bleed}}";
        }
    }
}
