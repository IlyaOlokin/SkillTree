using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

namespace SkillTree
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))] [ExecuteAlways]
    public class NodeConnectionRenderer : MonoBehaviour
    {
        
        [SerializeField] private Node rootNode;
        [SerializeField] private List<NodeConnectionData> nodeConnections = new();
        [SerializeField] private SplineContainer connectionPrefab;
        [SerializeField] private int resolutionPerSpline = 20;
        [SerializeField] private float lineWidth = 0.1f;
        [SerializeField] private Color allocatedColor;
        [SerializeField] private Color defaultColor;

        [SerializeField] private Mesh generatedMesh;

#if UNITY_EDITOR
        public void ConstructNodeConnections()
        {
            var pairs = CollectPairs(rootNode);

            foreach (var pair in pairs)
            {
                if (nodeConnections.Exists(x => x.pair.Equals(pair)))
                    continue;
                SplineContainer spline =
                    (SplineContainer)PrefabUtility.InstantiatePrefab(
                        connectionPrefab, transform);


                spline.transform.position = Vector3.zero;

                BezierKnot knot1 = new BezierKnot(pair.A.transform.position);
                BezierKnot knot2 = new BezierKnot(pair.B.transform.position);

                spline.Splines[0].Add(knot1);
                spline.Splines[0].Add(knot2);

                nodeConnections.Add(new NodeConnectionData
                {
                    pair = pair,
                    spline = spline
                });
            }
            EditorUtility.SetDirty(this);
        }
        
        public void CreateMeshAsset()
        {
            if (generatedMesh != null)
            {
                Debug.LogWarning("Mesh asset already exists");
                return;
            }

            generatedMesh = new Mesh();
            generatedMesh.name = "SkillTreeLines_Mesh";

            string folder = "Assets/Generated";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder("Assets", "Generated");
            }

            string path = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/SkillTreeLines_Mesh.asset");

            AssetDatabase.CreateAsset(generatedMesh, path);
            AssetDatabase.SaveAssets();

            GetComponent<MeshFilter>().sharedMesh = generatedMesh;

            EditorUtility.SetDirty(this);
            Debug.Log($"Mesh asset created at {path}");
        }
#endif

        private static List<NodePair> CollectPairs(Node root)
        {
            var result = new List<NodePair>();
            var visited = new HashSet<Node>();

            DFS(root, visited, result);
            return result;
        }

        //Depth First Search
        private static void DFS(
            Node node,
            HashSet<Node> visited,
            List<NodePair> pairs)
        {
            if (!visited.Add(node))
                return;

            foreach (var linked in node.ConnectedNodes)
            {
                if (linked is null || linked == node)
                    continue;

                var pair = new NodePair(node, linked);

                if (!pairs.Contains(pair))
                {
                    pairs.Add(pair);
                }

                DFS(linked, visited, pairs);
            }
        }

        public void BuildMesh()
        {
            if (generatedMesh == null)
            {
                Debug.LogError("Mesh asset not created");
                return;
            }
            
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();

            int vertIndex = 0;

            foreach (var nodeConnection in nodeConnections)
            {
                var spline = nodeConnection.spline;
                
                bool isAllocated = nodeConnection.pair.A.IsAllocated && nodeConnection.pair.B.IsAllocated ;

                Color lineColor = isAllocated ? allocatedColor : defaultColor;

                Vector3 prevPos = spline.EvaluatePosition(0f);

                for (int i = 1; i < resolutionPerSpline; i++)
                {
                    float t = i / (float)(resolutionPerSpline - 1);
                    Vector3 currPos = spline.EvaluatePosition(t);

                    Vector3 dir = (currPos - prevPos).normalized;
                    Vector3 normal = Vector3.Cross(dir, Vector3.forward);

                    Vector3 offset = normal * (lineWidth * 0.5f);

                    Vector3 v0 = prevPos + offset;
                    Vector3 v1 = prevPos - offset;
                    Vector3 v2 = currPos + offset;
                    Vector3 v3 = currPos - offset;

                    vertices.Add(v0);
                    vertices.Add(v1);
                    vertices.Add(v2);
                    vertices.Add(v3);

                    colors.Add(lineColor);
                    colors.Add(lineColor);
                    colors.Add(lineColor);
                    colors.Add(lineColor);

                    triangles.Add(vertIndex + 0);
                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 1);

                    triangles.Add(vertIndex + 2);
                    triangles.Add(vertIndex + 3);
                    triangles.Add(vertIndex + 1);

                    uvs.Add(Vector2.zero);
                    uvs.Add(Vector2.right);
                    uvs.Add(Vector2.up);
                    uvs.Add(Vector2.one);

                    vertIndex += 4;
                    prevPos = currPos;
                }
            }

            generatedMesh.Clear();
            generatedMesh.SetVertices(vertices);
            generatedMesh.SetTriangles(triangles, 0);
            generatedMesh.SetUVs(0, uvs);
            generatedMesh.SetColors(colors);

            generatedMesh.RecalculateBounds();
            generatedMesh.RecalculateNormals();
            
#if UNITY_EDITOR
            EditorUtility.SetDirty(generatedMesh);
            AssetDatabase.SaveAssets();
#endif
        }
    }

    [Serializable]
    public struct NodePair : IEquatable<NodePair>
    {
        public Node A;
        public Node B;

        public NodePair(Node n1, Node n2)
        {
            if (ReferenceEquals(n1, n2))
                throw new ArgumentException("Pair cannot contain the same node");
            
            if (n1.GetInstanceID() < n2.GetInstanceID())
            {
                A = n1;
                B = n2;
            }
            else
            {
                A = n2;
                B = n1;
            }
        }

        public bool Equals(NodePair other)
        {
            return ReferenceEquals(A, other.A) && ReferenceEquals(B, other.B);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is NodePair other))
                return false;

            return A == other.A && B == other.B;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + A.GetInstanceID();
                hash = hash * 31 + B.GetInstanceID();
                return hash;
            }
        }
    }
    
    [Serializable]
    public class NodeConnectionData
    {
        [SerializeField] public NodePair pair;
        [SerializeField] public SplineContainer spline;
    }
}
