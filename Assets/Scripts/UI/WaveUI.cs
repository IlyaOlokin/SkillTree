using System;
using Battle;
using LocalizationSupport;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    
    [SerializeField] private TMP_Text waveNumberText;
    [SerializeField] private TMP_Text waveProgressText;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button previousLevelButton;

    private void Start()
    {
        UpdateUI();
        spawner.OnLevelChanged += UpdateUI;
        spawner.OnWaveCleared += UpdateUI;
        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
    }
    
    private void UpdateUI()
    {
        int wavesInLevel = Mathf.Max(1, spawner.WavesToUnlockNextLevel);
        int currentWave = Mathf.Clamp(spawner.CurrentClearedWaves + 1, 1, wavesInLevel);

        waveNumberText.text = GameLocalization.Format(
            "ui.wave.level",
            "LVL: [[0]]",
            spawner.SelectedLevel);
        waveProgressText.text = $"{currentWave}/{wavesInLevel}";
        nextLevelButton.interactable = spawner.SelectedLevel < spawner.MaxUnlockedLevel;
        previousLevelButton.interactable = spawner.SelectedLevel > spawner.CurrentLocationStartingLevel;
    }

    private void OnDisable()
    {
        if (LocalizationSettings.HasSettings)
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }
    }

    private void OnDestroy()
    {
        spawner.OnLevelChanged -= UpdateUI;
        spawner.OnWaveCleared -= UpdateUI;
    }

    private void HandleLocaleChanged(Locale _)
    {
        UpdateUI();
    }
}
