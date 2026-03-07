using System.Collections.Generic;
using System.Linq;
using SkillTree;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

[EditorTool("Skill Tree Tool")]
public class SkillTreeTool : EditorTool
{
    private const float SnapThreshold = 0.5f;

    public SkillTreeToolConfig config;

    private Node selectedNode;
    
    private bool cacheDirty = true;
    private readonly List<(Vector3 a, Vector3 b)> connectionLines
        = new();
    private readonly HashSet<Node> corruptedNodes
        = new();
    private readonly List<(Vector3 a, Vector3 b)> asymmetricLines = new();
    private readonly HashSet<Node> asymmetricNodes = new();
    
    private void OnEnable()
    {
        config = FindConfig();
    }
    
    public override void OnActivated()
    {
        base.OnActivated();
        MarkCacheDirty();
        SceneView.RepaintAll();
    }

    private SkillTreeToolConfig FindConfig()
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

        DrawPrefabSwitch();
    }

    private void HandleInput()
    {
        Event e = Event.current;

        if (HandlePrefabSwitchInput(e))
        {
            e.Use();
            return;
        }

        if (e.type == EventType.KeyDown &&
            (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace))
        {
            if (DeleteSelectedNode())
                e.Use();
            return;
        }

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
            bool snapToSelected = e.alt;
            CreateNode(worldPos, clone, snapToSelected);
        }

        e.Use();
    }

    private bool DeleteSelectedNode()
    {
        if (selectedNode == null)
            return false;

        Node nodeToDelete = selectedNode;
        selectedNode = null;

        Node[] nodes = FindObjectsByType<Node>(FindObjectsSortMode.None);
        foreach (Node node in nodes)
        {
            if (node == null || node == nodeToDelete)
                continue;

            Disconnect(node, nodeToDelete);
        }

        SerializedObject so = new SerializedObject(nodeToDelete);
        SerializedProperty list = so.FindProperty("connectedNodes");
        if (list != null && list.arraySize > 0)
        {
            Undo.RecordObject(nodeToDelete, "Clear Node Connections");
            list.ClearArray();
            so.ApplyModifiedProperties();
        }

        Undo.DestroyObjectImmediate(nodeToDelete.gameObject);

        MarkCacheDirty();
        SceneView.RepaintAll();
        return true;
    }
    
    private Vector3 GetMouseWorldPosition(Vector2 mousePosition)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePosition);
        
        Plane plane = new Plane(Vector3.forward, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
    
    private Node GetNodeUnderMouse(Vector2 mousePosition)
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
    
    private void CreateNode(Vector3 position, bool copyFromSelected, bool snapToSelected)
    {
        if (snapToSelected)
            position = SnapToSelectedNode(position);

        Node newNode = CreateNodeFromPrefab(position);

        if (copyFromSelected && selectedNode != null)
            CopyNodeData(selectedNode, newNode);

        if (selectedNode != null)
            Connect(selectedNode, newNode);

        selectedNode = newNode;
        MarkCacheDirty();
    }

    private Vector3 SnapToSelectedNode(Vector3 position)
    {
        if (selectedNode == null)
            return position;

        Vector2 selected = selectedNode.transform.position;
        Vector2 point = position;
        Vector2 delta = point - selected;

        var candidates = new List<(Vector2 position, float score)>
        {
            (point, float.MaxValue)
        };

        float axisDeltaX = Mathf.Abs(delta.x);
        if (axisDeltaX <= SnapThreshold)
            candidates.Add((new Vector2(selected.x, point.y), axisDeltaX));

        float axisDeltaY = Mathf.Abs(delta.y);
        if (axisDeltaY <= SnapThreshold)
            candidates.Add((new Vector2(point.x, selected.y), axisDeltaY));

        if (axisDeltaX <= SnapThreshold && axisDeltaY <= SnapThreshold)
            candidates.Add((selected, Mathf.Max(axisDeltaX, axisDeltaY)));

        Vector2 diagonalA = new Vector2(1f, 1f).normalized;
        Vector2 projectedA = selected + Vector2.Dot(delta, diagonalA) * diagonalA;
        float distanceToDiagonalA = Vector2.Distance(point, projectedA);
        if (distanceToDiagonalA <= SnapThreshold)
            candidates.Add((projectedA, distanceToDiagonalA));

        Vector2 diagonalB = new Vector2(1f, -1f).normalized;
        Vector2 projectedB = selected + Vector2.Dot(delta, diagonalB) * diagonalB;
        float distanceToDiagonalB = Vector2.Distance(point, projectedB);
        if (distanceToDiagonalB <= SnapThreshold)
            candidates.Add((projectedB, distanceToDiagonalB));

        (Vector2 position, float score) best = candidates[0];
        for (int i = 1; i < candidates.Count; i++)
        {
            if (candidates[i].score < best.score)
                best = candidates[i];
        }

        return new Vector3(best.position.x, best.position.y, position.z);
    }
    
    private void CopyNodeData(Node source, Node target)
    {
        CopyModifiers(source, target);
    }
    
    private void CopyModifiers(Node source, Node target)
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
    
    private Node CreateNodeFromPrefab(Vector3 position)
    {
        GameObject prefab = config.nodePrefabs[config.currentPrefabIndex];
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        Undo.RegisterCreatedObjectUndo(go, "Create Node");
        Transform nodeParent = FindNodeParent();
        if (nodeParent != null)
        {
            Undo.SetTransformParent(go.transform, nodeParent, "Parent Node");
        }
        else
        {
            Debug.LogWarning("SkillTreeTool: Parent object \"Nodes\" was not found. Node created without parent.");
        }
        go.transform.position = position;

        return go.GetComponent<Node>();
    }

    private Transform FindNodeParent()
    {
        Transform[] transforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in transforms)
        {
            if (t == null || t.name != "Nodes")
                continue;
            if (!t.gameObject.scene.IsValid())
                continue;
            if (EditorUtility.IsPersistent(t))
                continue;
            return t;
        }

        return null;
    }

    private void HandleNodeClick(Node node)
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

    private void Connect(Node a, Node b)
    {
        AddConnection(a, b);
        AddConnection(b, a);
    }
    
    private bool AreConnected(Node a, Node b)
    {
        return a.ConnectedNodes.Contains(b)
               || b.ConnectedNodes.Contains(a);
    }

    private void AddConnection(Node from, Node to)
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
    
    private void Disconnect(Node a, Node b)
    {
        RemoveConnection(a, b);
        RemoveConnection(b, a);
    }

    private void RemoveConnection(Node from, Node to)
    {
        SerializedObject so = new SerializedObject(from);
        SerializedProperty list = so.FindProperty("connectedNodes");
        bool removed = false;

        for (int i = list.arraySize - 1; i >= 0; i--)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == to)
            {
                Undo.RecordObject(from, "Disconnect Nodes");
                list.DeleteArrayElementAtIndex(i);
                if (i < list.arraySize &&
                    list.GetArrayElementAtIndex(i).objectReferenceValue == null)
                {
                    list.DeleteArrayElementAtIndex(i);
                }
                removed = true;
            }
        }

        if (removed)
        {
            so.ApplyModifiedProperties();
            MarkCacheDirty();
        }
    }
    private bool HandlePrefabSwitchInput(Event e)
    {
        if (e.type != EventType.KeyDown || config == null || config.nodePrefabs.Count == 0)
            return false;

        if (e.keyCode == KeyCode.LeftArrow)
        {
            config.currentPrefabIndex =
                (config.currentPrefabIndex - 1 + config.nodePrefabs.Count) % config.nodePrefabs.Count;
            SceneView.RepaintAll();
            return true;
        }

        if (e.keyCode == KeyCode.RightArrow)
        {
            config.currentPrefabIndex =
                (config.currentPrefabIndex + 1) % config.nodePrefabs.Count;
            SceneView.RepaintAll();
            return true;
        }

        return false;
    }

    private void DrawPrefabSwitch()
    {
        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(10, 10, 260, 40), GUI.skin.box);
        GUILayout.Label(config.nodePrefabs[config.currentPrefabIndex].name);
        GUILayout.EndArea();
        Handles.EndGUI();
    }
    
    private void DrawSelectedNodeHighlight()
    {
        if (selectedNode == null) return;

        Handles.color = Color.yellow;
        float radius = 0.7f;
        Vector3 pos = selectedNode.transform.position;
        Handles.DrawWireDisc(pos, Vector3.forward, radius);
    }
    
    private void DrawConnections()
    {
        Handles.color = Color.white;

        foreach (var line in connectionLines)
            Handles.DrawLine(line.a, line.b);
    }
    
    private void DrawCorruptedNodeHighlights()
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
    
    private void DrawAsymmetricConnections()
    {
        Handles.color = new Color(1f, 0.5f, 0f); // оранжевый

        foreach (var line in asymmetricLines)
            Handles.DrawDottedLine(line.a, line.b, 5f);
    }
    
    private void DrawAsymmetricNodeHighlights()
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
    
    private void RebuildCache()
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
    
    private void MarkCacheDirty()
    {
        cacheDirty = true;
    }
}

