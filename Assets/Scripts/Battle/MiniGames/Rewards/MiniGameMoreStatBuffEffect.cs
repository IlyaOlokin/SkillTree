using System.Globalization;
using SkillTree;
using UnityEngine;

namespace Battle.MiniGames
{
    public abstract class MiniGameMoreStatBuffEffect : BaseEffect
    {
        private readonly StatType _statType;
        private float _moreValue;
        private BaseModifier _modifier;

        public override bool IsStackable { get; set; } = true;

        protected MiniGameMoreStatBuffEffect(float duration, StatType statType, float moreValue)
        {
            Duration = Mathf.Max(0f, duration);
            _statType = statType;
            _moreValue = Mathf.Max(0f, moreValue);
        }

        public override void OnApply(Unit unit)
        {
            if (unit == null || _moreValue <= 0f)
            {
                return;
            }

            ApplyModifier(unit);
        }

        public override void OnStack(Unit unit, BaseEffect newEffect, ActiveEffect existing)
        {
            if (newEffect == null || newEffect.GetType() != GetType() || existing == null)
            {
                return;
            }

            if (newEffect is not MiniGameMoreStatBuffEffect statBuffEffect)
            {
                return;
            }

            Duration = statBuffEffect.Duration;
            existing.TimeLeft = Duration;

            if (Mathf.Approximately(_moreValue, statBuffEffect._moreValue))
            {
                return;
            }

            ReplaceModifier(unit, statBuffEffect._moreValue);
        }

        public override void OnRemove(Unit unit)
        {
            RemoveModifier(unit);
        }

        protected override object[] GetDescriptionArguments()
        {
            return new object[]
            {
                FormatNumber(_moreValue * 100f),
                FormatNumber(Duration)
            };
        }

        private void ApplyModifier(Unit unit)
        {
            _modifier = CreateRuntimeModifier<BaseModifier>();
            _modifier.modifierContainer = new ModifierContainer(ModifierType.More, _statType, _moreValue);
            unit.AddOuterModifier(_modifier);
        }

        private void ReplaceModifier(Unit unit, float moreValue)
        {
            if (unit == null)
            {
                _moreValue = moreValue;
                return;
            }

            RemoveModifier(unit);
            _moreValue = Mathf.Max(0f, moreValue);

            if (_moreValue > 0f)
            {
                ApplyModifier(unit);
            }
        }

        private void RemoveModifier(Unit unit)
        {
            if (unit == null || _modifier == null)
            {
                return;
            }

            unit.RemoveOuterModifier(_modifier);
            ReleaseRuntimeModifier(_modifier);
            _modifier = null;
        }

        private static string FormatNumber(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
