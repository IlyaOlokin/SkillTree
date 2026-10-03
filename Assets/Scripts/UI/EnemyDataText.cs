using System;
using Battle;
using LocalizationSupport;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class EnemyDataText : MonoBehaviour
{
    [SerializeField] private EnemyUnit unit;
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        unit.OnInitialized += UpdateText;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }

    private void OnDestroy()
    {
        if (unit != null)
            unit.OnInitialized -= UpdateText;

        if (LocalizationSettings.HasSettings)
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
    }

    private void UpdateText()
    {
        if (unit == null || unit.SpawnData == null)
        {
            text.text = string.Empty;
            return;
        }

        text.text = $"{Math.Round(unit.SpawnData.Power)} ({Math.Round(unit.SpawnData.ExperienceReward)})\n{unit.SpawnData.Rarity}\nBase: {GetBaseModuleName(unit.SpawnData.Definition)}";
    }

    private void HandleLocaleChanged(Locale _)
    {
        UpdateText();
    }

    private static string GetBaseModuleName(GeneratedEnemyDefinition definition)
    {
        return definition?.CoreProfile != null ? definition.CoreProfile.name : "-";
    }
}
