using System;
using System.Collections.Generic;
using System.Globalization;
using LocalizationSupport;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class AilmentAbsorption : BaseEffect
    {
        private Modifier _sourceModifier;
        private float _cooldown;
        private float _timeLeft;
        private bool _isCharged;
        private bool _isReadyToBeRemoved;

        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.AilmentAbsorption;

        public bool IsCharged => _isCharged;

        public AilmentAbsorption(Modifier sourceModifier, float cooldown)
        {
            _sourceModifier = sourceModifier;
            _cooldown = Mathf.Max(0f, cooldown);
            _timeLeft = _cooldown;
        }

        public override void OnApply(Unit unit)
        {
            if (_cooldown <= 0f)
            {
                Charge();
            }
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is not AilmentAbsorption absorption)
            {
                return;
            }

            _sourceModifier = absorption._sourceModifier;
            _cooldown = absorption._cooldown;

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
            return new[]
            {
                GameLocalization.FormatContent(
                    "effect.ailmentAbsorption.description",
                    "Absorbs the next incoming {ailment|Ailment}, then recharges after [[0]] seconds.",
                    _cooldown.ToString("0.##", CultureInfo.InvariantCulture))
            };
        }

        protected override string GetDescriptionId()
        {
            return "ailmentAbsorption";
        }

        public bool TryAbsorb()
        {
            if (!_isCharged)
            {
                return false;
            }

            _isCharged = false;
            _timeLeft = _cooldown;
            if (_cooldown <= 0f)
            {
                Charge();
            }

            return true;
        }

        public static bool TryAbsorbIncomingAilment(Unit target)
        {
            if (target?.effectController == null)
            {
                return false;
            }

            List<ActiveEffect> absorptions = target.effectController.GetAllEffectsOfType<AilmentAbsorption>();
            for (int i = 0; i < absorptions.Count; i++)
            {
                if (absorptions[i]?.Effect is AilmentAbsorption absorption && absorption.TryAbsorb())
                {
                    return true;
                }
            }

            return false;
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
