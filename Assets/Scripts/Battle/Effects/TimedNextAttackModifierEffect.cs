using System;
using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class TimedNextAttackModifierEffect : BaseEffect
    {
        private readonly Modifier _sourceModifier;
        private float _cooldown;
        private ModifierContainer _modifierContainer;
        private float _timeLeft;
        private bool _isCharged;
        private bool _isReadyToBeRemoved;
        private bool _isSubscribed;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.TimedNextAttackModifier;
        public bool IsCharged => _isCharged;

        public TimedNextAttackModifierEffect(
            Modifier sourceModifier,
            float cooldown,
            ModifierContainer modifierContainer)
        {
            _sourceModifier = sourceModifier;
            _cooldown = Mathf.Max(0f, cooldown);
            _modifierContainer = modifierContainer;
            _timeLeft = _cooldown;
        }

        public override void OnApply(Unit unit)
        {
            Subscribe(unit);
            if (_cooldown <= 0f)
            {
                Charge();
            }
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is not TimedNextAttackModifierEffect timedEffect)
            {
                return;
            }

            _cooldown = timedEffect._cooldown;
            _modifierContainer = timedEffect._modifierContainer;

            if (_isCharged)
            {
                return;
            }

            _timeLeft = Mathf.Min(_timeLeft, _cooldown);
            if (_cooldown <= 0f)
            {
                Charge();
            }
        }

        public override void OnTick(Unit unit, float deltaTime)
        {
            if (!IsSourceStillCollected(unit))
            {
                _isReadyToBeRemoved = true;
                return;
            }

            if (_isCharged)
            {
                return;
            }

            _timeLeft = Mathf.Max(0f, _timeLeft - deltaTime);
            if (_timeLeft <= 0f)
            {
                Charge();
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
            if (_isCharged)
            {
                return string.Empty;
            }

            return Math.Max(0f, _timeLeft).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public override float GetIconTimerProgress(IReadOnlyList<ActiveEffect> activeEffects)
        {
            if (_isCharged || _cooldown <= 0f)
            {
                return 1f;
            }

            return Mathf.Clamp01(_timeLeft / _cooldown);
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            string modifierDescription = _modifierContainer != null
                ? _modifierContainer.GetDescription()
                : GameLocalization.GetModifier(
                    "modifier.timedNextAttackModifierContainer.noModifier",
                    "an unconfigured modifier");

            return new[]
            {
                GameLocalization.FormatContent(
                    "effect.timedNextAttackModifier.description",
                    "Charges every [[0]] seconds. When charged, your next attack has [[1]].",
                    _cooldown.ToString("0.##", CultureInfo.InvariantCulture),
                    modifierDescription)
            };
        }

        protected override string GetDescriptionId()
        {
            return "timedNextAttackModifier";
        }

        public void ApplyAttackBonus(DamageInfo damageInfo)
        {
            if (!_isCharged || _modifierContainer == null || damageInfo == null)
            {
                return;
            }

            damageInfo.BaseUnitModifiers.ChangeModifierValue(_modifierContainer);
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
            if (!_isCharged)
            {
                return;
            }

            _isCharged = false;
            _timeLeft = _cooldown;
            if (_cooldown <= 0f)
            {
                Charge();
            }
        }

        private void Charge()
        {
            _isCharged = true;
            _timeLeft = 0f;
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
