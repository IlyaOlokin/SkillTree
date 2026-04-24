using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Splines;
using Unity.Mathematics;
using SkillTree;
using ConnectionRendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

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
        [SerializeField] private float segmentsPerUnit = 4f;
        [SerializeField] private int minSegmentsPerSpline = 6;
        [SerializeField] private int maxSegmentsPerSpline = 120;
        [SerializeField] private int maxVerticesPerChunk = 60000;
        [SerializeField] private float allocatedLineWidth = 0.15f;
        [SerializeField] private float defaultLineWidth = 0.08f;
        [SerializeField] private Color allocatedColor;
        [SerializeField] private Color defaultColor;
        [SerializeField] private float connectionGrowSpeed = 6f;
        [SerializeField] [Range(0.001f, 0.25f)] private float frontWidth = 0.04f;
        [SerializeField] [Range(0f, 2f)] private float frontThicknessBoost = 0.35f;
        [SerializeField] [Range(0.001f, 0.25f)] private float frontThicknessWidth = 0.08f;
        [SerializeField] [ColorUsage(true, true)] private Color frontGlowColor = Color.white;
        [SerializeField] [Range(0.001f, 0.25f)] private float frontGlowWidth = 0.06f;
        [SerializeField] [Min(0f)] private float frontGlowIntensity = 1f;
        [SerializeField] [Min(0f)] private float baseWidth;
        
        private Texture2D _stateTexture;
        private Texture2D _progressTexture;
        private Material _material;
        private ConnectionVisualState[] _connectionStates = Array.Empty<ConnectionVisualState>();
        private float[] _connectionLengths = Array.Empty<float>();
        private Dictionary<Node, bool> _nodeAllocationStates = new();
        private const string ChunkObjectPrefix = "__ConnectionChunk_";


        private void OnValidate()
        {
            CacheMaterialReference();
            ApplyMaterialProperties();
        }

        private void Awake()
        {
            CacheMaterialReference();

            CacheNodeAllocationStates();
            if (skillTree != null)
                skillTree.OnAnyNodeChanged += ChangeNodeConnection;
        }

        private void OnDestroy()
        {
            if (skillTree != null)
                skillTree.OnAnyNodeChanged -= ChangeNodeConnection;

            ConnectionRendererUtility.ReleaseTexture(_stateTexture);
            ConnectionRendererUtility.ReleaseTexture(_progressTexture);
        }

        private void Start()
        {
            BuildMesh();
            CreateStateTexture();
            SyncAllConnectionStates();
        }

        private void Update()
        {
            if (!Application.isPlaying || _connectionStates.Length == 0 || _progressTexture == null)
                return;

            float speed = Mathf.Max(0.0001f, connectionGrowSpeed);
            bool hasChanges = false;

            for (int i = 0; i < _connectionStates.Length; i++)
            {
                ref ConnectionVisualState state = ref _connectionStates[i];
                if (Mathf.Approximately(state.progress, state.targetProgress))
                {
                    FinalizeConnectionVisualState(i, ref state);
                    continue;
                }

                float connectionLength = i < _connectionLengths.Length ? Mathf.Max(_connectionLengths[i], 0.0001f) : 0.0001f;
                float step = (speed * Time.deltaTime) / connectionLength;
                state.progress = Mathf.MoveTowards(state.progress, state.targetProgress, step);
                SetConnectionProgress(i, state.progress, state.reverse, true);
                FinalizeConnectionVisualState(i, ref state);
                hasChanges = true;
            }

            if (hasChanges)
            {
                _stateTexture.Apply(false);
                _progressTexture.Apply(false);
            }
        }

#if UNITY_EDITOR
        public void ConstructNodeConnections()
        {
            var pairs = NodeGraphTraversalService.CollectUniquePairs(rootNode);

            foreach (var pair in pairs)
            {
                if (nodeConnections.Exists(x => x.pair.Equals(pair)))
                {
                    NodeConnectionData existingConnection = nodeConnections.Find(x => x.pair.Equals(pair));
                    SyncSplineToPair(existingConnection);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(existingConnection.spline);

                    continue;
                }
                SplineContainer spline =
                    (SplineContainer)PrefabUtility.InstantiatePrefab(
                        connectionPrefab, transform);


                spline.transform.localPosition = Vector3.zero;
                spline.transform.localRotation = Quaternion.identity;
                spline.transform.localScale = Vector3.one;

                var connectionData = new NodeConnectionData
                {
                    pair = pair,
                    spline = spline
                };
                SyncSplineToPair(connectionData, true);
                nodeConnections.Add(connectionData);
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

        public int RemoveDuplicateNodeConnections()
        {
            Undo.RecordObject(this, "Remove Duplicate Node Connections");

            int removedCount = 0;
            var uniquePairs = new HashSet<NodePair>();

            for (int i = 0; i < nodeConnections.Count; i++)
            {
                NodeConnectionData connection = nodeConnections[i];
                bool isInvalid = connection == null ||
                                 connection.pair.A == null ||
                                 connection.pair.B == null;
                if (isInvalid)
                    continue;

                if (uniquePairs.Add(connection.pair))
                    continue;

                if (connection.spline != null)
                    Undo.DestroyObjectImmediate(connection.spline.gameObject);

                nodeConnections.RemoveAt(i);
                removedCount++;
                i--;
            }

            if (removedCount > 0)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                EditorUtility.SetDirty(this);
            }

            return removedCount;
        }

        public int RemoveUnreferencedConnectionChildren()
        {
            int removedCount = 0;
            var referencedChildren = new HashSet<GameObject>();

            foreach (NodeConnectionData connection in nodeConnections)
            {
                if (connection?.spline == null)
                    continue;

                referencedChildren.Add(connection.spline.gameObject);
            }

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (referencedChildren.Contains(child.gameObject))
                    continue;

                Undo.DestroyObjectImmediate(child.gameObject);
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
        SyncAllSplinePositionsToNodes();

        var rootFilter = GetComponent<MeshFilter>();
        if (rootFilter != null)
            rootFilter.sharedMesh = null;

        if (segmentsPerUnit <= 0f || maxSegmentsPerSpline < 2)
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
        _connectionLengths = new float[nodeConnections.Count];

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

            _connectionLengths[connectionId] = EstimateSplineLength(spline);
            int segmentCount = GetSegmentCountForLength(_connectionLengths[connectionId]);
            Vector3 prevPos = spline.EvaluatePosition(0f);

            for (int i = 1; i < segmentCount; i++)
            {
                if (vertIndex + 4 > maxVerts)
                    FlushChunk();

                float t = i / (float)(segmentCount - 1);
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

    private int GetSegmentCountForLength(float length)
    {
        return ConnectionRendererUtility.GetSegmentCountForLength(length, segmentsPerUnit, minSegmentsPerSpline, maxSegmentsPerSpline);
    }

    private float EstimateSplineLength(SplineContainer spline)
    {
        return ConnectionRendererUtility.EstimateSplineLength(spline);
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

        private void SyncAllSplinePositionsToNodes()
        {
            foreach (NodeConnectionData connection in nodeConnections)
                SyncSplineToPair(connection);
        }

        private void SyncSplineToPair(NodeConnectionData connection, bool resetSpline = false)
        {
            if (connection?.spline == null || connection.pair.A == null || connection.pair.B == null)
                return;

            Spline spline = connection.spline.Spline;
            if (spline == null)
                return;

            if (resetSpline)
                spline.Clear();

            while (spline.Count < 2)
                spline.Add(new BezierKnot(float3.zero));

            int lastKnotIndex = spline.Count - 1;
            BezierKnot startKnot = spline[0];
            BezierKnot endKnot = spline[lastKnotIndex];

            startKnot.Position = (float3)connection.spline.transform.InverseTransformPoint(connection.pair.A.transform.position);
            endKnot.Position = (float3)connection.spline.transform.InverseTransformPoint(connection.pair.B.transform.position);

            spline[0] = startKnot;
            spline[lastKnotIndex] = endKnot;
        }
        
        private void CreateStateTexture()
        {
            CacheMaterialReference();
            ConnectionRendererUtility.ReleaseTexture(_stateTexture);
            ConnectionRendererUtility.ReleaseTexture(_progressTexture);

            int textureWidth = Mathf.Max(1, nodeConnections.Count);

            _stateTexture = ConnectionRendererUtility.CreateRuntimeTexture(textureWidth);
            _progressTexture = ConnectionRendererUtility.CreateRuntimeTexture(textureWidth);

            _connectionStates = new ConnectionVisualState[nodeConnections.Count];
            if (_connectionLengths.Length != nodeConnections.Count)
                _connectionLengths = new float[nodeConnections.Count];

            for (int i = 0; i < nodeConnections.Count; i++)
            {
                bool isAllocated = nodeConnections[i].pair.IsAllocated();
                float progress = isAllocated ? 1f : 0f;
                Color color = ConnectionRendererUtility.GetShaderColor(isAllocated ? allocatedColor : defaultColor);
                float thickness = isAllocated ? allocatedLineWidth : defaultLineWidth;
                _connectionLengths[i] = EstimateSplineLength(nodeConnections[i].spline);

                _stateTexture.SetPixel(i, 0, new Color(thickness, color.r, color.g, color.b));
                _progressTexture.SetPixel(i, 0, new Color(progress, 0f, 0f, 0f));
                _connectionStates[i] = new ConnectionVisualState
                {
                    progress = progress,
                    targetProgress = progress,
                    reverse = false
                };
            }

            _stateTexture.Apply(false);
            _progressTexture.Apply(false);

            ConnectionRendererUtility.BindTextures(_material, _stateTexture, _progressTexture, textureWidth);
            ApplyMaterialProperties();
        }

        private void ChangeNodeConnection(Node node)
        {
            if (_stateTexture == null || _progressTexture == null || _connectionStates.Length != nodeConnections.Count)
                return;

            bool wasAllocated = _nodeAllocationStates.TryGetValue(node, out bool previousAllocated) && previousAllocated;
            bool isAllocated = node.IsAllocated;
            bool progressChanged = false;

            for (var i = 0; i < nodeConnections.Count; i++)
            {
                var connection = nodeConnections[i];
                if (connection.pair.Contains(node))
                {
                    bool pairAllocated = connection.pair.IsAllocated();
                    ref ConnectionVisualState state = ref _connectionStates[i];

                    if (pairAllocated && isAllocated && !wasAllocated)
                    {
                        Node sourceNode = connection.pair.A == node ? connection.pair.B : connection.pair.A;
                        state.reverse = ReferenceEquals(sourceNode, connection.pair.B);
                        state.targetProgress = 1f;
                        SetConnectionState(i, true);
                        SetConnectionProgress(i, state.progress, state.reverse, true);
                        progressChanged = true;
                    }
                    else if (!pairAllocated && !isAllocated && wasAllocated)
                    {
                        Node sourceNode = connection.pair.A == node ? connection.pair.B : connection.pair.A;
                        if (sourceNode != null && sourceNode.IsAllocated)
                        {
                            state.reverse = ReferenceEquals(sourceNode, connection.pair.B);
                            state.targetProgress = 0f;
                            SetConnectionState(i, true);
                            SetConnectionProgress(i, state.progress, state.reverse, true);
                        }
                        else
                        {
                            state.reverse = false;
                            state.progress = 0f;
                            state.targetProgress = 0f;
                            SetConnectionState(i, false);
                            SetConnectionProgress(i, 0f, false, false);
                        }
                        progressChanged = true;
                    }
                    else
                    {
                        float targetProgress = pairAllocated ? 1f : 0f;
                        if (!Mathf.Approximately(state.targetProgress, targetProgress) || !Mathf.Approximately(state.progress, targetProgress))
                        {
                            state.targetProgress = targetProgress;
                            if (pairAllocated)
                                SetConnectionState(i, true);
                            else
                                SetConnectionState(i, false);
                            SetConnectionProgress(i, state.progress, state.reverse, false);
                            progressChanged = true;
                        }
                    }
                }
            }
            
            _stateTexture.Apply(false);
            if (progressChanged)
                _progressTexture.Apply(false);

            _nodeAllocationStates[node] = isAllocated;
        }
        
        public void SetConnectionState(int id, bool isAllocated)
        {
            Color color = ConnectionRendererUtility.GetShaderColor(isAllocated ? allocatedColor : defaultColor);
            float thicknessMul = isAllocated ? allocatedLineWidth : defaultLineWidth;
            _stateTexture.SetPixel(
                id,
                0,
                new Color(thicknessMul, color.r, color.g, color.b)
            );
        }

        private void SetConnectionProgress(int id, float progress, bool reverse, bool isFrontActive)
        {
            _progressTexture.SetPixel(
                id,
                0,
                new Color(progress, reverse ? 1f : 0f, isFrontActive ? 1f : 0f, 0f)
            );
        }

        private void SyncAllConnectionStates()
        {
            if (_connectionStates.Length != nodeConnections.Count || _progressTexture == null)
                return;

            for (int i = 0; i < nodeConnections.Count; i++)
            {
                bool isAllocated = nodeConnections[i].pair.IsAllocated();
                _connectionStates[i].progress = isAllocated ? 1f : 0f;
                _connectionStates[i].targetProgress = _connectionStates[i].progress;
                _connectionStates[i].reverse = false;
                SetConnectionState(i, isAllocated);
                SetConnectionProgress(i, _connectionStates[i].progress, false, false);
            }

            _stateTexture.Apply(false);
            _progressTexture.Apply(false);
            CacheNodeAllocationStates();
        }

        private void CacheNodeAllocationStates()
        {
            _nodeAllocationStates.Clear();

            foreach (NodeConnectionData connection in nodeConnections)
            {
                if (connection?.pair.A != null && !_nodeAllocationStates.ContainsKey(connection.pair.A))
                    _nodeAllocationStates.Add(connection.pair.A, connection.pair.A.IsAllocated);

                if (connection?.pair.B != null && !_nodeAllocationStates.ContainsKey(connection.pair.B))
                    _nodeAllocationStates.Add(connection.pair.B, connection.pair.B.IsAllocated);
            }
        }

        private void ApplyMaterialProperties()
        {
            if (_material == null)
                return;

            ConnectionRendererUtility.ApplySharedMaterialProperties(
                _material,
                baseWidth,
                defaultColor,
                defaultLineWidth,
                allocatedLineWidth,
                frontWidth,
                frontThicknessBoost,
                frontThicknessWidth,
                frontGlowColor,
                frontGlowWidth,
                frontGlowIntensity);
        }

        private void FinalizeConnectionVisualState(int id, ref ConnectionVisualState state)
        {
            if (!Mathf.Approximately(state.progress, state.targetProgress))
                return;

            if (Mathf.Approximately(state.targetProgress, 0f))
            {
                SetConnectionState(id, false);
                SetConnectionProgress(id, 0f, state.reverse, false);
            }
            else if (Mathf.Approximately(state.targetProgress, 1f))
            {
                SetConnectionState(id, true);
                SetConnectionProgress(id, 1f, state.reverse, false);
            }
        }

        private void CacheMaterialReference()
        {
            var meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                _material = meshRenderer.sharedMaterial;
        }

    }

    internal struct ConnectionVisualState
    {
        public float progress;
        public float targetProgress;
        public bool reverse;
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
