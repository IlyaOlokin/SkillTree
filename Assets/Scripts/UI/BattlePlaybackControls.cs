using Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattlePlaybackControls : MonoBehaviour
{
    [SerializeField] private BattleTickSystem battleTickSystem;
    [SerializeField] private Button speedButton;
    [SerializeField] private TMP_Text speedLabel;
    [SerializeField] private Button pauseButton;
    [SerializeField] private BattlePlaybackIcon pauseIcon;

    private void OnEnable()
    {
        speedButton.onClick.AddListener(CycleSpeed);
        pauseButton.onClick.AddListener(TogglePause);
        Refresh();
    }

    private void OnDisable()
    {
        speedButton.onClick.RemoveListener(CycleSpeed);
        pauseButton.onClick.RemoveListener(TogglePause);
    }

    private void Update() => Refresh();

    public void CycleSpeed()
    {
        float speed = battleTickSystem.SpeedMultiplier;
        battleTickSystem.SpeedMultiplier = speed < 2f ? 2f : speed < 4f ? 4f : 1f;
        Refresh();
    }

    public void TogglePause()
    {
        if (battleTickSystem.HasPauseOwners) return;
        if (battleTickSystem.IsPaused) battleTickSystem.Resume();
        else battleTickSystem.Pause();
        Refresh();
    }

    private void Refresh()
    {
        if (battleTickSystem == null) return;
        string label = battleTickSystem.SpeedMultiplier.ToString("0") + "x";
        if (speedLabel.text != label) speedLabel.text = label;
        pauseButton.interactable = !battleTickSystem.HasPauseOwners;
        pauseIcon.ShowPlay = battleTickSystem.IsPaused;
    }
}
