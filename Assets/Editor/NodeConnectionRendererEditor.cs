using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using SkillTree;


[CustomEditor(typeof(NodeConnectionRenderer))]
public class NodeConnectionRendererEditor : Editor
{
    void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    void OnDisable()
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

        NodeConnectionRenderer comp = (NodeConnectionRenderer)target;

        if (GUILayout.Button("Construct Splines"))
        {
            comp.ConstructNodeConnections();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Build Spline Mesh"))
        {
            comp.BuildMesh();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Create Mesh Asset"))
        {
            comp.CreateMeshAsset();
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Empty Connections"))
        {
            int removed = comp.RemoveEmptyNodeConnections();
            Debug.Log($"Removed {removed} empty node connections.");
            GUIUtility.ExitGUI();
        }
    }
    
    void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Selection.activeObject = null; // 💥 ключевая строка
        }
    }
}

