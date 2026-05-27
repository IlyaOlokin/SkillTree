using System;
using Battle;
using DropSystem;
using TMPro;
using UnityEngine;
using Zenject;

public class LocationCompleteWindowController : MonoBehaviour
{
    [Inject] private BattleTickSystem _battleTickSystem;

    [Header("Scene references")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LocationFlowController locationFlowController;
    [SerializeField] private EnemyItemDropSpawner itemDropSpawner;

    [Header("UI references")]
    [SerializeField] private GameObject window;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private string defaultTitle = "Location complete";
    [SerializeField] private bool hideOnAwake = true;

    public event Action<LocationDefinition, int> OnWindowOpened;
    public event Action OnWindowClosed;

    public bool IsOpen => window != null && window.activeSelf;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (locationFlowController == null)
            locationFlowController = FindFirstObjectByType<LocationFlowController>();

        if (itemDropSpawner == null)
            itemDropSpawner = FindFirstObjectByType<EnemyItemDropSpawner>();

        if (hideOnAwake)
            HideWindow();
    }

    private void OnEnable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnLocationCompletedFirstTime += HandleLocationCompletedFirstTime;
            enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
            locationFlowController.OnModeChanged += HandleLocationModeChanged;
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnLocationCompletedFirstTime -= HandleLocationCompletedFirstTime;
            enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
            locationFlowController.OnModeChanged -= HandleLocationModeChanged;
    }

    public void CollectAllLoot()
    {
        itemDropSpawner.CollectAllActiveDrops();
    }

    public void ExitToMap()
    {
        HideWindow();

        if (locationFlowController != null)
        {
            locationFlowController.ReturnToMap();
            return;
        }

        _battleTickSystem?.Pause();
        enemySpawner?.ExitBattle();
    }

    public void HideWindow()
    {
        bool wasOpen = IsOpen;

        if (window != null)
            window.SetActive(false);

        if (wasOpen)
            OnWindowClosed?.Invoke();
    }

    private void HandleLocationCompletedFirstTime(LocationDefinition location, int completedLevel)
    {
        _battleTickSystem?.Pause();

        if (titleText != null)
            titleText.text = BuildTitle(location);

        if (window != null)
            window.SetActive(true);

        OnWindowOpened?.Invoke(location, completedLevel);
    }

    private string BuildTitle(LocationDefinition location)
    {
        if (location == null || string.IsNullOrWhiteSpace(location.DisplayName))
            return defaultTitle;

        return $"{location.DisplayName} complete";
    }

    private void HandleBattleActivityChanged(bool isBattleActive)
    {
        if (!isBattleActive)
            HideWindow();
    }

    private void HandleLocationModeChanged(LocationFlowController.FlowMode mode)
    {
        if (mode == LocationFlowController.FlowMode.Map)
            HideWindow();
    }
}
