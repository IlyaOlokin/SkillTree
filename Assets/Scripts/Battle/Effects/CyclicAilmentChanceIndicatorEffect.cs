using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using SkillTree;

namespace Battle
{
    public class CyclicAilmentChanceIndicatorEffect : BaseEffect
    {
        private Modifier _sourceModifier;
        private float _chanceBonus;
        private int _currentIndex;
        private bool _isReadyToBeRemoved;
        private bool _isSubscribed;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => CurrentVisualType;
        public StatType CurrentChanceStat => _currentIndex switch
        {
            0 => StatType.IgniteChance,
            1 => StatType.ChillChance,
            2 => StatType.OverchargeChance,
            _ => StatType.IgniteChance
        };

        public CyclicAilmentChanceIndicatorEffect(
            Modifier sourceModifier,
            float chanceBonus)
        {
            _sourceModifier = sourceModifier;
            _chanceBonus = chanceBonus;
        }

        public override void OnApply(Unit unit)
        {
            Subscribe(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is not CyclicAilmentChanceIndicatorEffect indicator)
            {
                return;
            }

            _sourceModifier = indicator._sourceModifier;
            _chanceBonus = indicator._chanceBonus;
            _isReadyToBeRemoved = false;
        }

        public override void OnTick(Unit unit, float deltaTime)
        {
            if (!IsSourceStillCollected(unit))
            {
                _isReadyToBeRemoved = true;
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _isReadyToBeRemoved;
        }

        public override void OnRemove(Unit unit)
        {
            Unsubscribe(unit);
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return $"+{(_chanceBonus * 100f).ToString("0.#", CultureInfo.InvariantCulture)}%";
        }

        public override float GetIconTimerProgress(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return 1f;
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new[]
            {
                GameLocalization.FormatContent(
                    "effect.cyclicAilmentChance.description",
                    "Next attack has [[0]]% increased chance to apply [[1]].",
                    _chanceBonus * 100f,
                    GetCurrentAilmentName())
            };
        }

        protected override string GetDescriptionId()
        {
            return "cyclicAilmentChance";
        }

        private EffectVisualType CurrentVisualType => _currentIndex switch
        {
            0 => EffectVisualType.Ignite,
            1 => EffectVisualType.Chill,
            2 => EffectVisualType.Overcharge,
            _ => EffectVisualType.None
        };

        private string GetCurrentAilmentName()
        {
            return CurrentVisualType switch
            {
                EffectVisualType.Ignite => "{ignite|Ignite}",
                EffectVisualType.Chill => "{chill|Chill}",
                EffectVisualType.Overcharge => "{overcharge|Overcharge}",
                _ => GameLocalization.GetContent("effect.cyclicAilmentChance.name", "Cyclic Ailment")
            };
        }

        private void Advance()
        {
            _currentIndex = (_currentIndex + 1) % 3;
        }

        private void Subscribe(Unit unit)
        {
            if (unit == null || _isSubscribed)
            {
                return;
            }

            unit.OnAttackCompleted += HandleAttackCompleted;
            _isSubscribed = true;
        }

        private void Unsubscribe(Unit unit)
        {
            if (unit == null || !_isSubscribed)
            {
                return;
            }

            unit.OnAttackCompleted -= HandleAttackCompleted;
            _isSubscribed = false;
        }

        private void HandleAttackCompleted(ITarget _)
        {
            Advance();
        }

        private bool IsSourceStillCollected(Unit unit)
        {
            if (unit == null || _sourceModifier == null)
            {
                return false;
            }

            List<CollectedModifier> modifiers = unit.GetAllModifiers();
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (ReferenceEquals(modifiers[i].Modifier, _sourceModifier))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
