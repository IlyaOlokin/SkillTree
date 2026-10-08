using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class CriticalCharge : BaseEffect
    {
        public const int MaxStacks = 3;
        public const float BuffDuration = 4f;
        public const float CritDamageBonusPerStack = 0.15f;
        public const string Description = "Critical hits consume all charges, gaining +15% Critical Damage Bonus per charge. Lasts 4 seconds. Maximum 3 charges. Gaining a charge refreshes the duration.";

        public int Stacks { get; private set; } = 1;
        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.CriticalCharge;

        public CriticalCharge() { Duration = BuffDuration; }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            Stacks = Mathf.Min(MaxStacks, Stacks + 1);
            existing.TimeLeft = BuffDuration;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects) => Stacks.ToString();
        protected override string GetDescriptionId() => "criticalCharge";
        protected override string GetDescriptionFallback() => Description;

        public static void ApplyCriticalCharge(AttackContext context)
        {
            if (context?.DamageInfo == null || !context.DamageInfo.IsCritical || context.Attacker?.effectController == null)
                return;

            EffectController controller = context.Attacker.effectController;
            foreach (ActiveEffect active in controller.GetAllEffectsOfType<CriticalCharge>())
            {
                if (active.TimeLeft > 0f && active.Effect is CriticalCharge charge)
                {
                    context.DamageInfo.BaseUnitModifiers.ChangeModifierValue(
                        new ModifierContainer(ModifierType.Added, StatType.CritDamageBonus,
                            CritDamageBonusPerStack * charge.Stacks));
                }
                // Remove immediately so nested attacks cannot spend the same charges twice.
                controller.RemoveEffect(active);
            }
        }
    }
}
