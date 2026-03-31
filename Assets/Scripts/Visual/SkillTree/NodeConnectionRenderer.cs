using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using SkillTree;
using Unity.Mathematics;

namespace SkillTree
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteAlways]
    public class NodeConnectionRenderer : MonoBehaviour
    {
        [SerializeField] private MainSkillTree skillTree;
        [SerializeField] private Node rootNode;
        [SerializeField] private List<NodeConnectionData> nodeConnections = new();
        [SerializeField] private SplineContainer connectionPrefab;
        [SerializeField] private int resolutionPerSpline = 20;
        [SerializeField] private int maxVerticesPerChunk = 60000;
        [SerializeField] private float allocatedLineWidth = 0.15f;
        [SerializeField] private float defaultLineWidth = 0.08f;
        [SerializeField] private Color allocatedColor;
        [SerializeField] private Color defaultColor;
        
        private Texture2D _stateTexture;
        private Material _material;
        private const string ChunkObjectPrefix = "__ConnectionChunk_";


        private void OnValidate()
        {
            _material = GetComponent<MeshRenderer>().sharedMaterial;
        }

        private void Awake()
        {
            if (_material == null)
                _material = GetComponent<MeshRenderer>().sharedMaterial;

            skillTree.OnAnyNodeChanged += ChangeNodeConnection;
        }

        private void OnDestroy()
        {
            if (skillTree != null)
                skillTree.OnAnyNodeChanged -= ChangeNodeConnection;
        }

        private void Start()
        {
            BuildMesh();
            CreateStateTexture();
        }

#if UNITY_EDITOR
        public void ConstructNodeConnections()
        {
            var pairs = NodeGraphTraversalService.CollectUniquePairs(rootNode);

            foreach (var pair in pairs)
            {
                if (nodeConnections.Exists(x => x.pair.Equals(pair)))
                {
                    NodeConnectionData NodeConnectionData = nodeConnections.Find(x => x.pair.Equals(pair));
                    BezierKnot knot11 = NodeConnectionData.spline.Splines[0][0];
                    BezierKnot knot22 = NodeConnectionData.spline.Splines[0][1];
                    
                    knot11.Position = new float3(pair.A.transform.position);
                    knot22.Position = new float3(pair.B.transform.position);
                    
                    NodeConnectionData.spline.Splines[0][0] = knot11;
                    NodeConnectionData.spline.Splines[0][1] = knot22;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(NodeConnectionData.spline);
                    
                    continue;
                }
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

        public int RemoveEmptyNodeConnections()
        {
            Undo.RecordObject(this, "Remove Empty Node Connections");

            int removedCount = 0;
            for (int i = nodeConnections.Count - 1; i >= 0; i--)
            {
                NodeConnectionData connection = nodeConnections[i];
                bool isEmpty = connection == null ||
                               connection.spline == null ||
                               connection.pair.A == null ||
                               connection.pair.B == null;
                if (!isEmpty)
                    continue;

                if (connection != null && connection.spline != null)
                    Undo.DestroyObjectImmediate(connection.spline.gameObject);

                nodeConnections.RemoveAt(i);
                removedCount++;
            }

            if (removedCount > 0)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                EditorUtility.SetDirty(this);
            }

            return removedCount;
        }
#endif

    public void BuildMesh()
    {
        var rootFilter = GetComponent<MeshFilter>();
        if (rootFilter != null)
            rootFilter.sharedMesh = null;

        if (resolutionPerSpline < 2)
        {
            RemoveUnusedChunkObjects(0);
            return;
        }

        int maxVerts = Mathf.Clamp(maxVerticesPerChunk, 4, 65535);
        var vertices = new List<Vector3>(maxVerts);
        var triangles = new List<int>(maxVerts * 3 / 2);
        var normals = new List<Vector3>(maxVerts);
        var uvs = new List<Vector2>(maxVerts);
        var uv2s = new List<Vector2>(maxVerts);
        var colors = new List<Color>(maxVerts);

        int vertIndex = 0;
        int connectionId = 0;
        int chunkIndex = 0;
        Color baseColor = Color.white;

        void FlushChunk()
        {
            if (vertices.Count == 0)
                return;

            Mesh chunkMesh = GetOrCreateChunkMesh(chunkIndex);
            chunkMesh.Clear();
            chunkMesh.indexFormat = IndexFormat.UInt16;
            chunkMesh.SetVertices(vertices);
            chunkMesh.SetTriangles(triangles, 0);
            chunkMesh.SetUVs(0, uvs);
            chunkMesh.SetUVs(1, uv2s);
            chunkMesh.SetNormals(normals);
            chunkMesh.SetColors(colors);
            chunkMesh.RecalculateBounds();

            chunkIndex++;
            vertIndex = 0;
            vertices.Clear();
            triangles.Clear();
            normals.Clear();
            uvs.Clear();
            uv2s.Clear();
            colors.Clear();
        }

        foreach (var nodeConnection in nodeConnections)
        {
            var spline = nodeConnection.spline;
            if (spline == null || spline.Splines.Count == 0)
            {
                connectionId++;
                continue;
            }

            Vector3 prevPos = spline.EvaluatePosition(0f);

            for (int i = 1; i < resolutionPerSpline; i++)
            {
                if (vertIndex + 4 > maxVerts)
                    FlushChunk();

                float t = i / (float)(resolutionPerSpline - 1);
                Vector3 currPos = spline.EvaluatePosition(t);

                Vector3 dir = (currPos - prevPos).normalized;
                Vector3 normal = Vector3.Cross(dir, Vector3.forward);

                normals.Add(-normal);
                normals.Add(-normal);
                normals.Add(-normal);
                normals.Add(-normal);

                vertices.Add(prevPos);
                vertices.Add(prevPos);
                vertices.Add(currPos);
                vertices.Add(currPos);

                triangles.Add(vertIndex + 0);
                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 1);

                triangles.Add(vertIndex + 2);
                triangles.Add(vertIndex + 3);
                triangles.Add(vertIndex + 1);

                uvs.Add(new Vector2(t, 1));
                uvs.Add(new Vector2(t, -1));
                uvs.Add(new Vector2(t, 1));
                uvs.Add(new Vector2(t, -1));

                uv2s.Add(new Vector2(connectionId, 0));
                uv2s.Add(new Vector2(connectionId, 0));
                uv2s.Add(new Vector2(connectionId, 0));
                uv2s.Add(new Vector2(connectionId, 0));

                colors.Add(baseColor);
                colors.Add(baseColor);
                colors.Add(baseColor);
                colors.Add(baseColor);

                vertIndex += 4;
                prevPos = currPos;
            }

            connectionId++;
        }

        FlushChunk();
        RemoveUnusedChunkObjects(chunkIndex);
    }

    private Mesh GetOrCreateChunkMesh(int chunkIndex)
    {
        if (_material == null)
            _material = GetComponent<MeshRenderer>().sharedMaterial;

        string chunkName = $"{ChunkObjectPrefix}{chunkIndex}";
        Transform chunkTransform = transform.Find(chunkName);
        if (chunkTransform == null)
        {
            var chunkObject = new GameObject(chunkName);
            chunkObject.transform.SetParent(transform, false);
            chunkObject.layer = gameObject.layer;
            chunkTransform = chunkObject.transform;
        }

        var meshFilter = chunkTransform.GetComponent<MeshFilter>();
        if (meshFilter == null)
            meshFilter = chunkTransform.gameObject.AddComponent<MeshFilter>();

        var meshRenderer = chunkTransform.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
            meshRenderer = chunkTransform.gameObject.AddComponent<MeshRenderer>();

        meshRenderer.sharedMaterial = _material;

        if (meshFilter.sharedMesh == null)
        {
            var mesh = new Mesh { name = $"SkillTreeLines_Chunk_{chunkIndex}" };
            meshFilter.sharedMesh = mesh;
        }

        return meshFilter.sharedMesh;
    }

    private void RemoveUnusedChunkObjects(int usedChunks)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (!child.name.StartsWith(ChunkObjectPrefix, StringComparison.Ordinal))
                continue;

            if (!int.TryParse(child.name.Substring(ChunkObjectPrefix.Length), out int index))
                continue;

            if (index < usedChunks)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
#else
            Destroy(child.gameObject);
#endif
        }
    }
        
        private void CreateStateTexture()
        {
            if (_material == null)
                _material = GetComponent<MeshRenderer>().sharedMaterial;

            _stateTexture = new Texture2D(
                nodeConnections.Count,
                1,
                TextureFormat.RGBAFloat,
                false,
                true
            );

            _stateTexture.filterMode = FilterMode.Point;
            _stateTexture.wrapMode = TextureWrapMode.Clamp;

            for (int i = 0; i < nodeConnections.Count; i++)
            {
                _stateTexture.SetPixel(i, 0, new Color(defaultLineWidth, defaultColor.r, defaultColor.g, defaultColor.b));
            }

            _stateTexture.Apply(false);

            _material.SetTexture("_StateTex", _stateTexture);
            _material.SetFloat("_StateTexWidth", nodeConnections.Count);
        }

        private void ChangeNodeConnection(Node node)
        {
            for (var i = 0; i < nodeConnections.Count; i++)
            {
                var connection = nodeConnections[i];
                if (connection.pair.Contains(node))
                {
                    SetConnectionState(i, nodeConnections[i].pair.IsAllocated());
                }
            }
            
            _stateTexture.Apply(false);
        }
        
        public void SetConnectionState(int id, bool isAllocated)
        {
            Color color = isAllocated ? allocatedColor : defaultColor;
            float thicknessMul = isAllocated ? allocatedLineWidth : defaultLineWidth;
            _stateTexture.SetPixel(
                id,
                0,
                new Color(thicknessMul, color.r, color.g, color.b)
            );
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

        public bool Contains(Node node)
        {
            return A == node || B == node;
        }

        public bool IsAllocated()
        {
            return A.IsAllocated && B.IsAllocated;
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
