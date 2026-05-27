using System.Collections.Generic;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class Vengeance : BaseEffect
    {
        private const float MoreDamagePerStack = 0.05f;
        private const float AddedAilmentChancePerStack = 0.1f;

        private int _stacks;
        private bool _isUsed;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Vengeance;
        public int Stacks => _stacks;

        public Vengeance(int stacks)
        {
            _stacks = Mathf.Max(0, stacks);
        }

        public override void OnApply(Unit unit)
        {
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is Vengeance vengeance)
            {
                _stacks += Mathf.Max(0, vengeance._stacks);
            }
        }

        public override void OnRemove(Unit unit)
        {
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _isUsed;
        }

        public override void Consume(Unit unit)
        {
            _isUsed = true;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return _stacks > 1 ? _stacks.ToString() : string.Empty;
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new[]
            {
                GameLocalization.FormatContent(
                    "effect.vengeance.description",
                    "Your next attack consumes all stacks. Each stack grants [[0]]% more Damage and +[[1]]% Ailment Chance.",
                    MoreDamagePerStack * 100f,
                    AddedAilmentChancePerStack * 100f)
            };
        }

        protected override string GetDescriptionId()
        {
            return "vengeance";
        }

        public void ApplyAttackBonus(DamageInfo damageInfo)
        {
            if (_isUsed || damageInfo == null || _stacks <= 0)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.More, StatType.Damage, MoreDamagePerStack * _stacks));
            damageInfo.BaseUnitModifiers.ChangeModifierValue(
                new ModifierContainer(ModifierType.Added, StatType.AilmentChance, AddedAilmentChancePerStack * _stacks));
        }

        public static void ApplyVengeanceEffect(AttackContext context)
        {
            if (context?.Attacker?.effectController == null)
            {
                return;
            }

            foreach (ActiveEffect activeEffect in context.Attacker.effectController.GetAllEffectsOfType<Vengeance>())
            {
                if (activeEffect.Effect is not Vengeance vengeance)
                {
                    continue;
                }

                vengeance.ApplyAttackBonus(context.DamageInfo);
                context.QueueEffectConsumption(context.Attacker, activeEffect);
            }
        }
    }
}
