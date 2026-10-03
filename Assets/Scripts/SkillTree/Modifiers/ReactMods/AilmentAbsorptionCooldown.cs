using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Reactive/Ailment Absorption Cooldown", fileName = "New AilmentAbsorptionCooldown")]
    public class AilmentAbsorptionCooldown : Modifier
    {
        [SerializeField, Min(0f)] private float cooldown = 5f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EffectController effectController = unit?.effectController;
            if (effectController == null)
            {
                return null;
            }

            void AddEffect()
            {
                effectController.AddRepeatedEffect(() => new AilmentAbsorption(this, cooldown));
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    AddEffect();
                    unit.OnCombatStateReset += AddEffect;
                },
                () => unit.OnCombatStateReset -= AddEffect);
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.ailmentAbsorptionCooldown.description",
                "Every [[0]] seconds, {ailmentAbsorption|Ailment Absorption} charges. While charged, it absorbs the next incoming {ailment|Ailment}.",
                cooldown);
        }
    }
}
