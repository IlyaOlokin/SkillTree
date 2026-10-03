using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Cyclic Ailment Chance", fileName = "New CyclicAilmentChance")]
    public class CyclicAilmentChanceModifier : Modifier
    {
        [SerializeField, Min(0f)] private float chanceBonus = 1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit)
        {
            return CreateRuntimeBinding(unit, ModifierPowerContext.None);
        }

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            if (unit == null || unit.effectController == null)
            {
                return null;
            }

            float scaledChanceBonus = powerContext.Scale(chanceBonus);

            void AddIndicator()
            {
                unit.effectController.AddRepeatedEffect(() => new CyclicAilmentChanceIndicatorEffect(
                    this,
                    scaledChanceBonus));
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    AddIndicator();
                    unit.OnCombatStateReset += AddIndicator;
                },
                () =>
                {
                    unit.OnCombatStateReset -= AddIndicator;
                });
        }

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.OnAttack;
        }

        public override void ApplyEffect(AttackContext context)
        {
            ApplyEffect(context, ModifierPowerContext.None);
        }

        public override void ApplyEffect(AttackContext context, ModifierPowerContext powerContext)
        {
            if (context?.Attacker == null || context.DamageInfo == null)
            {
                return;
            }

            CyclicAilmentChanceIndicatorEffect indicator = GetIndicator(context.Attacker);
            if (indicator == null)
            {
                return;
            }

            context.DamageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(
                    ModifierType.Added,
                    indicator.CurrentChanceStat,
                    powerContext.Scale(chanceBonus)));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.cyclicAilmentChance.description",
                "Attacks cycle between +[[0]]% to {ignite|Ignite}, {chill|Chill}, and {overcharge|Overcharge} Chance.",
                powerContext.HighlightValue(powerContext.Scale(chanceBonus) * 100f));
        }

        private static CyclicAilmentChanceIndicatorEffect GetIndicator(Unit unit)
        {
            if (unit?.effectController == null)
            {
                return null;
            }

            foreach (ActiveEffect activeEffect in unit.effectController.GetAllEffectsOfType<CyclicAilmentChanceIndicatorEffect>())
            {
                if (activeEffect?.Effect is CyclicAilmentChanceIndicatorEffect indicator)
                {
                    return indicator;
                }
            }

            return null;
        }
    }
}
