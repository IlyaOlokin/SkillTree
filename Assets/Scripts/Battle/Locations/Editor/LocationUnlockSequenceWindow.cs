using System.Collections.Generic;
using Battle;
using UnityEditor;
using UnityEngine;

public sealed class LocationUnlockSequenceWindow : EditorWindow
{
    private const string CatalogEditorPrefsKey = "Battle.LocationUnlockSequenceWindow.Catalog";

    private LocationCatalog _catalog;
    private Vector2 _scrollPosition;

    [MenuItem("Tools/Battle/Location Unlock Sequence")]
    private static void Open()
    {
        GetWindow<LocationUnlockSequenceWindow>("Location Unlocks");
    }

    private void OnEnable()
    {
        string catalogPath = EditorPrefs.GetString(CatalogEditorPrefsKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(catalogPath))
            _catalog = AssetDatabase.LoadAssetAtPath<LocationCatalog>(catalogPath);

        if (_catalog == null)
            _catalog = FindFirstCatalogAsset();
    }

    private void OnDisable()
    {
        string catalogPath = _catalog != null ? AssetDatabase.GetAssetPath(_catalog) : string.Empty;
        EditorPrefs.SetString(CatalogEditorPrefsKey, catalogPath);
    }

    private void OnGUI()
    {
        DrawCatalogSelector();

        if (_catalog == null)
        {
            EditorGUILayout.HelpBox("Assign a Location Catalog to edit unlock dependencies.", MessageType.Info);
            return;
        }

        DrawLocations();
    }

    private void DrawCatalogSelector()
    {
        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            _catalog = (LocationCatalog)EditorGUILayout.ObjectField(
                "Location Catalog",
                _catalog,
                typeof(LocationCatalog),
                false);

            if (GUILayout.Button("Use Selected", GUILayout.Width(110f)))
                TryUseSelectedCatalog();
        }
    }

    private void TryUseSelectedCatalog()
    {
        if (Selection.activeObject is LocationCatalog selectedCatalog)
            _catalog = selectedCatalog;
    }

    private static LocationCatalog FindFirstCatalogAsset()
    {
        string[] guids = AssetDatabase.FindAssets("t:LocationCatalog");
        if (guids == null || guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<LocationCatalog>(path);
    }

    private void DrawLocations()
    {
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "A location with no prerequisites is available immediately. If prerequisites are set, completing any one of them unlocks this location.",
            MessageType.None);

        _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

        IReadOnlyList<LocationDefinition> locations = _catalog.Locations;
        for (int i = 0; i < locations.Count; i++)
        {
            LocationDefinition location = locations[i];
            if (location == null)
                continue;

            DrawLocation(location);
        }

        EditorGUILayout.EndScrollView();
    }

    private static void DrawLocation(LocationDefinition location)
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(location.DisplayName, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Location ID", location.LocationId);

            SerializedObject serializedLocation = new(location);
            SerializedProperty prerequisites = serializedLocation.FindProperty("unlockPrerequisites");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(prerequisites, new GUIContent("Unlock after any completed"), true);

            if (EditorGUI.EndChangeCheck())
            {
                serializedLocation.ApplyModifiedProperties();
                EditorUtility.SetDirty(location);
            }
        }
    }
}
