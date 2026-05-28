using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Timed Next Attack Modifier Container", fileName = "New TimedNextAttackModifierContainer")]
    public class TimedNextAttackModifierContainer : Modifier
    {
        [SerializeField, Min(0f)] private float cooldown = 5f;
        [SerializeField] private ModifierContainer modifierContainer;

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

            float scaledCooldown = cooldown;
            ModifierContainer scaledModifierContainer = powerContext.Scale(modifierContainer);

            void AddEffect()
            {
                effectController.AddRepeatedEffect(() => new TimedNextAttackModifierEffect(
                    this,
                    scaledCooldown,
                    scaledModifierContainer));
            }

            return new DelegateModifierRuntimeBinding(
                () =>
                {
                    AddEffect();
                    unit.OnCombatStateReset += AddEffect;
                },
                () => unit.OnCombatStateReset -= AddEffect);
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
            if (context?.Attacker?.effectController == null)
            {
                return;
            }

            foreach (ActiveEffect activeEffect in context.Attacker.effectController.GetAllEffectsOfType<TimedNextAttackModifierEffect>())
            {
                if (activeEffect.Effect is not TimedNextAttackModifierEffect timedEffect || !timedEffect.IsCharged)
                {
                    continue;
                }

                timedEffect.ApplyAttackBonus(context.DamageInfo);
            }
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            if (modifierContainer == null)
            {
                return GameLocalization.GetModifier(
                    "modifier.timedNextAttackModifierContainer.noModifier",
                    "Every few seconds, your next attack gains an unconfigured modifier");
            }

            return GameLocalization.FormatModifier(
                "modifier.timedNextAttackModifierContainer.description",
                "Every [[0]] seconds, your next attack gains [[1]].",
                cooldown,
                powerContext.Scale(modifierContainer).GetDescription());
        }
    }
}
