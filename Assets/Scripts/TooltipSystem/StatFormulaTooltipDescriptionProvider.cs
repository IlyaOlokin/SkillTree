using System;
using System.Collections.Generic;
using System.Globalization;
using Battle;
using UnityEngine;
using Zenject;

namespace TooltipSystem
{
    public class StatFormulaTooltipDescriptionProvider : MonoBehaviour, ITooltipDescriptionProvider
    {
        [Inject] private PlayerUnit _player;
        [SerializeField] private StatType statType;

        public string GetTooltipTitle()
        {
            return LocalizationSupport.GameLocalization.LocalizeEnum(statType);
        }

        public bool ShouldShowTooltipTitle()
        {
            return true;
        }

        public void SetStatType(StatType value)
        {
            statType = value;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            if (_player == null)
            {
                return Array.Empty<string>();
            }

            StatModifier modifier = _player.BaseUnitModifiers.GetModifier(statType);
            List<string> parts = new()
            {
                FormatValue(modifier.Added.Value)
            };

            if (!Mathf.Approximately(modifier.Increased.Value, 0f))
            {
                parts.Add(FormatFactor(1f + modifier.Increased.Value));
            }

            if (modifier.More != null)
            {
                for (int i = 0; i < modifier.More.Count; i++)
                {
                    parts.Add(FormatFactor(1f + modifier.More[i]));
                }
            }

            float finalValue = _player.BaseUnitModifiers.GetStatValue(statType);
            return new[] { $"{string.Join(" * ", parts)} = {FormatValue(finalValue)}" };
        }

        private string FormatValue(float value)
        {
            bool isPercent = StatTypeDisplayRules.IsPercentStat(statType);
            float displayValue = isPercent ? value * 100f : value;
            bool isDoubleDigit = Mathf.Abs(displayValue) >= 10f;
            float roundedValue = isDoubleDigit
                ? Mathf.Round(displayValue)
                : Mathf.Round(displayValue * 10f) / 10f;

            string text = roundedValue.ToString(
                isDoubleDigit ? "0" : "0.#",
                CultureInfo.InvariantCulture);

            return isPercent ? $"{text}%" : text;
        }

        private static string FormatFactor(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
