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
        text.text = Math.Round(unit.SpawnData.Power) + "\n" + GameLocalization.LocalizeEnum(unit.SpawnData.Rarity);
    }

    private void HandleLocaleChanged(Locale _)
    {
        UpdateText();
    }
}
