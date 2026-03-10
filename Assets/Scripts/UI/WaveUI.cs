using Battle;
using TMPro;
using UnityEngine;
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
    }
    
    private void UpdateUI()
    {
        waveNumberText.text = $"LVL: {spawner.SelectedLevel}";
        waveProgressText.text = $"{spawner.CurrentClearedWaves}/{spawner.WavesToUnlockNextLevel}";
        nextLevelButton.interactable = spawner.SelectedLevel < spawner.MaxUnlockedLevel;
        previousLevelButton.interactable = spawner.SelectedLevel > 1;
    }

    private void OnDestroy()
    {
        spawner.OnLevelChanged -= UpdateUI;
        spawner.OnWaveCleared -= UpdateUI;
    }
}
