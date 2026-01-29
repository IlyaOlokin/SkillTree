using System.Collections.Generic;
using System.Linq;
using SkillTree;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Skill Tree Tool")]
public class SkillTreeTool : EditorTool
{
    public SkillTreeToolConfig config;

    private Node selectedNode;
    
    private bool cacheDirty = true;
    private readonly List<(Vector3 a, Vector3 b)> connectionLines
        = new();
    private readonly HashSet<Node> corruptedNodes
        = new();
    private readonly List<(Vector3 a, Vector3 b)> asymmetricLines = new();
    private readonly HashSet<Node> asymmetricNodes = new();
    
    void OnEnable()
    {
        config = FindConfig();
    }
    
    public override void OnActivated()
    {
        base.OnActivated();
        MarkCacheDirty();
        SceneView.RepaintAll();
    }

    SkillTreeToolConfig FindConfig()
    {
        string[] guids = AssetDatabase.FindAssets("t:SkillTreeToolConfig");
        if (guids.Length == 0)
            return null;

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<SkillTreeToolConfig>(path);
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (config == null || config.nodePrefabs.Count == 0)
            return;
        
        if (cacheDirty)
            RebuildCache();

        HandleInput();

        DrawConnections();                 
        DrawAsymmetricConnections();

        DrawCorruptedNodeHighlights();     // red
        DrawAsymmetricNodeHighlights();    // orange
        DrawSelectedNodeHighlight();       // yellow

        DrawPrefabSwitcher();
    }

    void HandleInput()
    {
        Event e = Event.current;
        if (e.type != EventType.MouseDown || e.button != 0)
            return;

        Vector3 worldPos = GetMouseWorldPosition(e.mousePosition);
        Node node = GetNodeUnderMouse(e.mousePosition);

        if (node != null)
        {
            HandleNodeClick(node);
        }
        else
        {
            bool clone = e.control || e.command;
            CreateNode(worldPos, clone);
        }

        e.Use();
    }
    
    Vector3 GetMouseWorldPosition(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        
        Plane plane = new Plane(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
    
    Node GetNodeUnderMouse(Vector2 mousePosition)
    {
        Vector3 worldPos = GetMouseWorldPosition(mousePosition);

        
        Node[] nodes = FindObjectsByType<Node>(FindObjectsSortMode.None);

        float pickRadius = 0.5f; 
        foreach (Node node in nodes)
        {
            if (Vector2.Distance(worldPos, node.transform.position) <= pickRadius)
                return node;
        }

        return null;
    }
    
    void CreateNode(Vector3 position, bool copyFromSelected)
    {
        Node newNode = CreateNodeFromPrefab(position);

        if (copyFromSelected && selectedNode != null)
            CopyNodeData(selectedNode, newNode);

        if (selectedNode != null)
            Connect(selectedNode, newNode);

        selectedNode = newNode;
        MarkCacheDirty();
    }
    
    void CopyNodeData(Node source, Node target)
    {
        CopyModifiers(source, target);
    }
    
    void CopyModifiers(Node source, Node target)
    {
        Undo.RecordObject(target, "Copy Modifiers");

        target.Modifiers.Clear();

        foreach (Modifier modifier in source.Modifiers)
        {
            if (modifier == null) continue;
            Modifier copy = modifier;
            target.Modifiers.Add(copy);
        }

        EditorUtility.SetDirty(target);
    }
    
    Node CreateNodeFromPrefab(Vector3 position)
    {
        GameObject prefab = config.nodePrefabs[config.currentPrefabIndex];
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        Undo.RegisterCreatedObjectUndo(go, "Create Node");
        go.transform.position = position;

        return go.GetComponent<Node>();
    }

    void HandleNodeClick(Node node)
    {
        if (selectedNode == null)
            selectedNode = node;
        else if (selectedNode != node)
        {
            if (AreConnected(selectedNode, node)) 
                Disconnect(selectedNode, node);
            else
                Connect(selectedNode, node);
            
            selectedNode = null;
        }
        else
        {
            selectedNode = null;
        }
        
        MarkCacheDirty();
    }

    void Connect(Node a, Node b)
    {
        AddConnection(a, b);
        AddConnection(b, a);
    }
    
    bool AreConnected(Node a, Node b)
    {
        return a.ConnectedNodes.Contains(b)
               || b.ConnectedNodes.Contains(a);
    }

    void AddConnection(Node from, Node to)
    {
        SerializedObject so = new SerializedObject(from);
        SerializedProperty list = so.FindProperty("connectedNodes");

        for (int i = 0; i < list.arraySize; i++)
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == to)
                return;

        Undo.RecordObject(from, "Connect Nodes");
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = to;
        so.ApplyModifiedProperties();
        
        MarkCacheDirty();
    }
    
    void Disconnect(Node a, Node b)
    {
        RemoveConnection(a, b);
        RemoveConnection(b, a);
    }

    void RemoveConnection(Node from, Node to)
    {
        SerializedObject so = new SerializedObject(from);
        SerializedProperty list = so.FindProperty("connectedNodes");

        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == to)
            {
                Undo.RecordObject(from, "Disconnect Nodes");
                list.DeleteArrayElementAtIndex(i);
                so.ApplyModifiedProperties();
                break;
            }
        }

        MarkCacheDirty();
    }

    void DrawPrefabSwitcher()
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 220, 40), GUI.skin.box);
        GUILayout.BeginHorizontal();

        if (GUILayout.Button("◀", GUILayout.Width(30)))
            config.currentPrefabIndex =
                (config.currentPrefabIndex - 1 + config.nodePrefabs.Count) % config.nodePrefabs.Count;

        GUILayout.Label(config.nodePrefabs[config.currentPrefabIndex].name);

        if (GUILayout.Button("▶", GUILayout.Width(30)))
            config.currentPrefabIndex =
                (config.currentPrefabIndex + 1) % config.nodePrefabs.Count;

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
        Handles.EndGUI();
    }
    
    void DrawSelectedNodeHighlight()
    {
        if (selectedNode == null) return;

        Handles.color = Color.yellow;
        float radius = 0.7f;
        Vector3 pos = selectedNode.transform.position;
        Handles.DrawWireDisc(pos, Vector3.forward, radius);
    }
    
    void DrawConnections()
    {
        Handles.color = Color.white;

        foreach (var line in connectionLines)
            Handles.DrawLine(line.a, line.b);
    }
    
    void DrawCorruptedNodeHighlights()
    {
        Handles.color = Color.red;
        float radius = 0.8f;

        foreach (Node node in corruptedNodes)
        {
            if (node == null) continue;
            Handles.DrawWireDisc(
                node.transform.position,
                Vector3.forward,
                radius
            );
        }
    }
    
    void DrawAsymmetricConnections()
    {
        Handles.color = new Color(1f, 0.5f, 0f); // оранжевый

        foreach (var line in asymmetricLines)
            Handles.DrawDottedLine(line.a, line.b, 5f);
    }
    
    void DrawAsymmetricNodeHighlights()
    {
        Handles.color = new Color(1f, 0.5f, 0f);
        float radius = 0.65f;

        foreach (Node node in asymmetricNodes)
        {
            if (node == null) continue;
            Handles.DrawWireDisc(
                node.transform.position,
                Vector3.forward,
                radius
            );
        }
    }
    
    void RebuildCache()
    {
        connectionLines.Clear();
        asymmetricLines.Clear();
        corruptedNodes.Clear();
        asymmetricNodes.Clear();

        Node[] nodes = FindObjectsByType<Node>(FindObjectsSortMode.None);

        foreach (Node node in nodes)
        {
            if (node == null) continue;

            var seen = new HashSet<Node>();

            foreach (Node connected in node.ConnectedNodes)
            {
                if (connected == null)
                {
                    corruptedNodes.Add(node);
                    continue;
                }
                
                bool symmetric = connected.ConnectedNodes.Contains(node);

                if (node.GetInstanceID() < connected.GetInstanceID())
                {
                    if (symmetric)
                    {
                        connectionLines.Add((
                            node.transform.position,
                            connected.transform.position
                        ));
                    }
                    else
                    {
                        asymmetricLines.Add((
                            node.transform.position,
                            connected.transform.position
                        ));
                    }
                }
                if (!symmetric)
                {
                    asymmetricNodes.Add(node);
                    asymmetricNodes.Add(connected);
                }
                
                if (!seen.Add(connected))
                    corruptedNodes.Add(node);
            }
        }

        cacheDirty = false;
    }
    
    void MarkCacheDirty()
    {
        cacheDirty = true;
    }
}
