using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using SkillTree;


[CustomEditor(typeof(NodeConnectionRenderer))]
public class NodeConnectionRendererEditor : Editor
{
    void OnEnable()
    {
        // Paired with OnDisable below; the domain-reload analyzer misses this Editor lifecycle.
#pragma warning disable UDR0004
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
#pragma warning restore UDR0004
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

        if (GUILayout.Button("Rebuild Connections Full Pipeline"))
        {
            RebuildConnectionsFullPipeline(comp);
            GUIUtility.ExitGUI();
        }

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

        if (GUILayout.Button("Remove Empty Connections"))
        {
            int removed = comp.RemoveEmptyNodeConnections();
            Debug.Log($"Removed {removed} empty node connections.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Duplicate Connections"))
        {
            int removed = comp.RemoveDuplicateNodeConnections();
            Debug.Log($"Removed {removed} duplicate node connections.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Remove Unreferenced Children"))
        {
            int removed = comp.RemoveUnreferencedConnectionChildren();
            Debug.Log($"Removed {removed} unreferenced child connection objects.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Strip Spline Connection Objects"))
        {
            int removed = comp.StripSplineConnectionObjects();
            Debug.Log($"Stripped {removed} spline connection objects. Connections now render directly from node pairs.");
            GUIUtility.ExitGUI();
        }

        if (GUILayout.Button("Force Create Spline Connection Objects"))
        {
            int created = comp.ForceCreateSplineConnectionObjects();
            Debug.Log($"Created {created} spline connection objects from connection data.");
            GUIUtility.ExitGUI();
        }
    }

    private static void RebuildConnectionsFullPipeline(NodeConnectionRenderer comp)
    {
        int created = comp.ForceCreateSplineConnectionObjects();
        comp.ConstructNodeConnections();
        int removedEmpty = comp.RemoveEmptyNodeConnections();
        int removedDuplicates = comp.RemoveDuplicateNodeConnections();
        comp.BuildMesh();
        int stripped = comp.StripSplineConnectionObjects();

        Debug.Log(
            $"Rebuilt node connections full pipeline. Created: {created}, removed empty: {removedEmpty}, removed duplicates: {removedDuplicates}, stripped: {stripped}.",
            comp);
    }
    
    void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            Selection.activeObject = null;
        }
    }

    [MenuItem("Tools/Skill Tree/Strip Spline Connection Objects In Open Scenes")]
    public static void StripSplineConnectionObjectsInOpenScenes()
    {
        int removed = StripLoadedSceneRenderers();
        Debug.Log($"Stripped {removed} spline connection objects in loaded scenes.");
    }

    [MenuItem("Tools/Skill Tree/Force Create Spline Connection Objects In Open Scenes")]
    public static void ForceCreateSplineConnectionObjectsInOpenScenes()
    {
        int created = ForceCreateLoadedSceneRendererSplines();
        Debug.Log($"Created {created} spline connection objects in loaded scenes.");
    }

    public static void StripMainSceneSplineConnectionObjects()
    {
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
        int removed = StripLoadedSceneRenderers();

        if (removed > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        bool saved = EditorSceneManager.SaveScene(scene);
        Debug.Log($"Stripped {removed} spline connection objects from MainScene. Saved: {saved}");
    }

    private static int StripLoadedSceneRenderers()
    {
        int removed = 0;
        foreach (NodeConnectionRenderer renderer in Resources.FindObjectsOfTypeAll<NodeConnectionRenderer>())
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid() || EditorUtility.IsPersistent(renderer))
                continue;

            removed += renderer.StripSplineConnectionObjects();
        }

        return removed;
    }

    private static int ForceCreateLoadedSceneRendererSplines()
    {
        int created = 0;
        foreach (NodeConnectionRenderer renderer in Resources.FindObjectsOfTypeAll<NodeConnectionRenderer>())
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid() || EditorUtility.IsPersistent(renderer))
                continue;

            created += renderer.ForceCreateSplineConnectionObjects();
        }

        return created;
    }
}

