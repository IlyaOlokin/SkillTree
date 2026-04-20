using System.Collections.Generic;
using Battle;
using UnityEngine;

public class LocationMapPanelController : MonoBehaviour
{
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private LocationFlowController flowController;
    [SerializeField] private List<LocationMapButton> locationNodes = new();
    [SerializeField] private LocationInfoWindow infoWindow;

    private void Awake()
    {
        if (enemySpawner == null)
            enemySpawner = FindFirstObjectByType<EnemySpawner>();

        if (flowController == null)
            flowController = FindFirstObjectByType<LocationFlowController>();

        CacheLocationNodesIfNeeded();
        BindLocationNodes();

        if (infoWindow != null)
        {
            infoWindow.Initialize(enemySpawner);
            infoWindow.OnEnterRequested += HandleEnterRequested;
            infoWindow.OnCloseRequested += HandleInfoWindowClosed;
            infoWindow.Hide();
        }
    }

    private void OnEnable()
    {
        infoWindow?.Hide();
        Refresh();

        if (enemySpawner != null)
        {
            enemySpawner.OnLocationChanged += Refresh;
            enemySpawner.OnLevelChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (enemySpawner != null)
        {
            enemySpawner.OnLocationChanged -= Refresh;
            enemySpawner.OnLevelChanged -= Refresh;
        }
    }

    private void OnDestroy()
    {
        if (infoWindow != null)
        {
            infoWindow.OnEnterRequested -= HandleEnterRequested;
            infoWindow.OnCloseRequested -= HandleInfoWindowClosed;
        }
    }

    private void Refresh()
    {
        if (enemySpawner == null)
            return;

        RefreshNodeSelection();
        if (infoWindow != null && infoWindow.IsOpen)
            infoWindow.RefreshContent();
    }

    private void CacheLocationNodesIfNeeded()
    {
        if (locationNodes.Count > 0)
            return;

        GetComponentsInChildren(true, locationNodes);
    }

    private void BindLocationNodes()
    {
        for (int i = 0; i < locationNodes.Count; i++)
        {
            if (locationNodes[i] == null)
                continue;

            locationNodes[i].Bind(HandleLocationSelected);
        }
    }

    private void RefreshNodeSelection()
    {
        for (int i = 0; i < locationNodes.Count; i++)
        {
            LocationMapButton node = locationNodes[i];
            if (node == null)
                continue;
        }
    }

    private bool IsNodeSelected(LocationMapButton node)
    {
        return node != null &&
               enemySpawner != null &&
               node.LocationId == enemySpawner.SelectedLocationId;
    }

    private void HandleLocationSelected(LocationMapButton locationButton)
    {
        if (locationButton == null || locationButton.Location == null)
            return;

        LocationDefinition location = locationButton.Location;

        if (flowController != null)
            flowController.SelectLocation(location.LocationId);
        else if (enemySpawner != null)
            enemySpawner.SelectLocation(location.LocationId, false);

        infoWindow?.Show(location, locationButton.RectTransform);
        RefreshNodeSelection();
    }

    private void HandleEnterRequested(LocationDefinition location)
    {
        if (location == null)
            return;

        if (flowController != null)
        {
            flowController.SelectLocation(location.LocationId);
            infoWindow?.Hide();
            flowController.EnterSelectedLocation();
            return;
        }

        if (enemySpawner != null)
        {
            enemySpawner.SelectLocation(location.LocationId, false);
            infoWindow?.Hide();
            enemySpawner.EnterBattle();
        }
    }

    private void HandleInfoWindowClosed()
    {
        infoWindow?.Hide();
    }
}
