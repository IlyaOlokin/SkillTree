using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using SkillTree;


[CustomEditor(typeof(NodeConnectionRenderer))]
public class NodeConnectionRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        NodeConnectionRenderer comp = (NodeConnectionRenderer)target;

        if (GUILayout.Button("Construct Splines"))
        {
            comp.ConstructNodeConnections();
        }
        if (GUILayout.Button("Build Spline Mesh"))
        {
            comp.BuildMesh();
        }
        if (GUILayout.Button("Create Mesh Asset"))
        {
            comp.CreateMeshAsset();
        }
    }
}

