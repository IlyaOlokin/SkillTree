using MenuTree;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MenuConnectionRenderer))]
public class MenuConnectionRendererEditor : Editor
{
    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    public override void OnInspectorGUI()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorGUILayout.LabelField("Inspector disabled during Play Mode");
            return;
        }

        DrawDefaultInspector();

        MenuConnectionRenderer component = (MenuConnectionRenderer)target;

        if (GUILayout.Button("Construct Splines"))
        {
            component.ConstructNodeConnections();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Build Spline Mesh"))
        {
            component.BuildMesh();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Empty Connections"))
        {
            int removed = component.RemoveEmptyNodeConnections();
            Debug.Log($"Removed {removed} empty menu node connections.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Duplicate Connections"))
        {
            int removed = component.RemoveDuplicateNodeConnections();
            Debug.Log($"Removed {removed} duplicate menu node connections.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Unreferenced Children"))
        {
            int removed = component.RemoveUnreferencedConnectionChildren();
            Debug.Log($"Removed {removed} unreferenced child connection objects.");
            GUIUtility.ExitGUI();
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            Selection.activeObject = null;
    }
}
