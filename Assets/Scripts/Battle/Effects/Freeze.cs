using System;
using System.Collections.Generic;
using System.Globalization;
using SkillTree;
using LocalizationSupport;
using UnityEngine;

namespace Battle
{
    public class Freeze : BaseEffect
    {
        public const float BASE_DURATION = 2f;
        public override bool IsStackable { get; set; } = true;
        public override EffectVisualType VisualType => EffectVisualType.Freeze;

        private Func<BaseEffect> _chillAfterFreezeFactory;
        private BaseModifier _attackSpeedModifier;
        private float _timeLeft = BASE_DURATION;
        private bool _expiredNaturally;

        internal Freeze(Func<BaseEffect> chillAfterFreezeFactory)
        {
            Duration = -1f;
            _chillAfterFreezeFactory = chillAfterFreezeFactory;
        }

        public override void OnApply(Unit unit)
        {
            ApplyModifier(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect is Freeze freeze)
            {
                _timeLeft = Mathf.Max(_timeLeft, freeze._timeLeft);
                _chillAfterFreezeFactory = freeze._chillAfterFreezeFactory;
            }
        }

        public override void OnTick(Unit unit, float deltaTime)
        {
            if (_expiredNaturally)
            {
                return;
            }

            _timeLeft -= deltaTime;
            if (_timeLeft > 0f)
            {
                return;
            }

            _expiredNaturally = true;
            RemoveModifier(unit);
            if (_chillAfterFreezeFactory != null)
            {
                unit.effectController.AddEffect(_chillAfterFreezeFactory);
            }
        }

        public override bool IsReadyToBeRemoved(Unit unit)
        {
            return _expiredNaturally;
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifier(unit);
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return Mathf.Max(0f, _timeLeft).ToString("0.0", CultureInfo.InvariantCulture);
        }

        public override float GetIconTimerProgress(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return Mathf.Clamp01(_timeLeft / BASE_DURATION);
        }

        public override IReadOnlyList<string> GetTooltipDescriptions()
        {
            return new[]
            {
                GameLocalization.GetDescription(
                    "description.freeze1",
                    "Freeze replaces {chill|Chill} when a hit that applies {chill|Chill} also deals more than 40% of the target's Maximum Health"),
                GameLocalization.GetDescription(
                    "description.freeze2",
                    "Frozen units cannot attack for 2 seconds, then receive {chill|Chill}"),
            };
        }

        private void ApplyModifier(Unit unit)
        {
            if (_attackSpeedModifier != null)
            {
                return;
            }

            _attackSpeedModifier = ScriptableObject.CreateInstance<BaseModifier>();
            _attackSpeedModifier.modifierContainer = new ModifierContainer(
                ModifierType.More,
                StatType.AttackSpeed,
                -1f);
            unit.AddOuterModifier(_attackSpeedModifier);
        }

        private void RemoveModifier(Unit unit)
        {
            if (_attackSpeedModifier == null)
            {
                return;
            }

            unit.RemoveOuterModifier(_attackSpeedModifier);
            _attackSpeedModifier = null;
        }
    }
}
