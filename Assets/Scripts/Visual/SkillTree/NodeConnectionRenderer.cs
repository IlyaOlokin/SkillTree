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
        private readonly Dictionary<Node, List<int>> _connectionIdsByNode = new();
        private readonly List<int> _activeConnections = new();
        private readonly HashSet<int> _activeConnectionIds = new();
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
            if (!Application.isPlaying || _activeConnections.Count == 0 || _progressTexture == null)
                return;

            float speed = Mathf.Max(0.0001f, connectionGrowSpeed);
            bool hasChanges = false;

            for (int activeIndex = _activeConnections.Count - 1; activeIndex >= 0; activeIndex--)
            {
                int i = _activeConnections[activeIndex];
                ref ConnectionVisualState state = ref _connectionStates[i];
                float connectionLength = i < _connectionLengths.Length ? Mathf.Max(_connectionLengths[i], 0.0001f) : 0.0001f;
                float step = (speed * Time.deltaTime) / connectionLength;
                if (step <= 0f)
                    continue;
                state.progress = Mathf.MoveTowards(state.progress, state.targetProgress, step);
                SetConnectionProgress(i, state.progress, state.reverse, true);
                if (Mathf.Approximately(state.progress, state.targetProgress))
                {
                    state.progress = state.targetProgress;
                    FinalizeConnectionVisualState(i, ref state);
                    _activeConnectionIds.Remove(i);
                    int lastIndex = _activeConnections.Count - 1;
                    _activeConnections[activeIndex] = _activeConnections[lastIndex];
                    _activeConnections.RemoveAt(lastIndex);
                }
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
            if (rootNode == null)
            {
                Debug.LogWarning("Cannot synchronize connections without a root node.", this);
                return;
            }

            var pairs = NodeGraphTraversalService.CollectUniquePairs(rootNode);
            var currentPairs = new HashSet<NodePair>(pairs);
            Undo.RecordObject(this, "Synchronize Node Connections");

            // Cached geometry must not outlive the graph edge it represents.
            // Retain the existing data for surviving edges to preserve their curves.
            for (int i = nodeConnections.Count - 1; i >= 0; i--)
            {
                NodeConnectionData connection = nodeConnections[i];
                if (connection != null && connection.pair.A != null && connection.pair.B != null &&
                    currentPairs.Contains(connection.pair))
                    continue;

                if (connection?.spline != null)
                    Undo.DestroyObjectImmediate(connection.spline.gameObject);
                nodeConnections.RemoveAt(i);
            }

            foreach (var pair in pairs)
            {
                if (nodeConnections.Exists(x => x.pair.Equals(pair)))
                {
                    NodeConnectionData existingConnection = nodeConnections.Find(x => x.pair.Equals(pair));
                    if (existingConnection.spline != null)
                    {
                        SyncSplineToPair(existingConnection);
                        existingConnection.ClearBakedPolyline();
                        PrefabUtility.RecordPrefabInstancePropertyModifications(existingConnection.spline);
                    }

                    continue;
                }
                SplineContainer spline = null;
                if (connectionPrefab != null)
                {
                    spline = (SplineContainer)PrefabUtility.InstantiatePrefab(connectionPrefab, transform);
                    Undo.RegisterCreatedObjectUndo(spline.gameObject, "Create Node Connection");
                    spline.transform.localPosition = Vector3.zero;
                    spline.transform.localRotation = Quaternion.identity;
                    spline.transform.localScale = Vector3.one;
                }

                var connectionData = new NodeConnectionData
                {
                    pair = pair,
                    spline = spline
                };
                if (spline != null)
                {
                    SyncSplineToPair(connectionData, true);
                    connectionData.ClearBakedPolyline();
                }
                nodeConnections.Add(connectionData);
            }

            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
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

        public int StripSplineConnectionObjects()
        {
            Undo.RecordObject(this, "Strip Skill Tree Spline Connections");

            int removedCount = 0;
            foreach (NodeConnectionData connection in nodeConnections)
            {
                if (connection?.spline == null)
                    continue;

                SyncSplineToPair(connection);
                BakeConnectionPolyline(connection);
                Undo.DestroyObjectImmediate(connection.spline.gameObject);
                connection.spline = null;
                removedCount++;
            }

            if (removedCount > 0)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                EditorUtility.SetDirty(this);
                BuildMesh();
            }

            return removedCount;
        }

        public int ForceCreateSplineConnectionObjects()
        {
            if (connectionPrefab == null)
            {
                Debug.LogWarning($"{nameof(NodeConnectionRenderer)} requires a connection prefab to recreate spline objects.", this);
                return 0;
            }

            Undo.RecordObject(this, "Force Create Skill Tree Spline Connections");

            int createdCount = 0;
            foreach (NodeConnectionData connection in nodeConnections)
            {
                if (connection == null || connection.pair.A == null || connection.pair.B == null)
                    continue;

                if (connection.spline != null)
                {
                    SyncSplineToPair(connection);
                    BakeConnectionPolyline(connection);
                    Undo.DestroyObjectImmediate(connection.spline.gameObject);
                }

                SplineContainer spline = (SplineContainer)PrefabUtility.InstantiatePrefab(connectionPrefab, transform);
                Undo.RegisterCreatedObjectUndo(spline.gameObject, "Create Skill Tree Spline Connection");
                spline.transform.localPosition = Vector3.zero;
                spline.transform.localRotation = Quaternion.identity;
                spline.transform.localScale = Vector3.one;

                connection.spline = spline;
                RestoreSplineShape(connection);
                connection.ClearBakedPolyline();
                createdCount++;
            }

            if (createdCount > 0)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(this);
                EditorUtility.SetDirty(this);
                BuildMesh();
            }

            return createdCount;
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
            if (!TryGetConnectionPosition(nodeConnection, 0f, out Vector3 prevPos))
            {
                connectionId++;
                continue;
            }

            _connectionLengths[connectionId] = EstimateConnectionLength(nodeConnection);
            int segmentCount = GetSegmentCountForLength(_connectionLengths[connectionId]);

            for (int i = 1; i < segmentCount; i++)
            {
                if (vertIndex + 4 > maxVerts)
                    FlushChunk();

                float t = i / (float)(segmentCount - 1);
                if (!TryGetConnectionPosition(nodeConnection, t, out Vector3 currPos))
                    break;

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

    private float EstimateConnectionLength(NodeConnectionData connection)
    {
        if (connection?.spline != null && connection.spline.Splines.Count > 0)
            return EstimateSplineLength(connection.spline);

        if (connection?.HasBakedPolyline == true)
            return EstimatePolylineLength(connection.bakedPolyline);

        if (connection?.pair.A == null || connection.pair.B == null)
            return 0f;

        return Vector3.Distance(connection.pair.A.transform.position, connection.pair.B.transform.position);
    }

    private float EstimatePolylineLength(IReadOnlyList<Vector3> polyline)
    {
        float length = 0f;
        for (int i = 1; i < polyline.Count; i++)
            length += Vector3.Distance(polyline[i - 1], polyline[i]);

        return length;
    }

    private bool TryGetConnectionPosition(NodeConnectionData connection, float t, out Vector3 position)
    {
        position = default;
        if (connection == null)
            return false;

        if (connection.spline != null && connection.spline.Splines.Count > 0)
        {
            position = connection.spline.EvaluatePosition(t);
            return true;
        }

        if (connection.HasBakedPolyline)
        {
            position = EvaluatePolyline(connection.bakedPolyline, t);
            return true;
        }

        if (connection.pair.A == null || connection.pair.B == null)
            return false;

        position = Vector3.Lerp(
            connection.pair.A.transform.position,
            connection.pair.B.transform.position,
            t);
        return true;
    }

    private Vector3 EvaluatePolyline(IReadOnlyList<Vector3> polyline, float t)
    {
        if (polyline.Count == 1)
            return polyline[0];

        float totalLength = EstimatePolylineLength(polyline);
        if (totalLength <= 0f)
            return polyline[0];

        float targetDistance = Mathf.Clamp01(t) * totalLength;
        float distance = 0f;
        for (int i = 1; i < polyline.Count; i++)
        {
            Vector3 from = polyline[i - 1];
            Vector3 to = polyline[i];
            float segmentLength = Vector3.Distance(from, to);
            if (segmentLength <= 0f)
                continue;

            if (distance + segmentLength >= targetDistance)
                return Vector3.Lerp(from, to, (targetDistance - distance) / segmentLength);

            distance += segmentLength;
        }

        return polyline[^1];
    }

#if UNITY_EDITOR
    private void BakeConnectionPolyline(NodeConnectionData connection)
    {
        if (connection?.spline == null || connection.spline.Splines.Count == 0)
            return;

        float length = EstimateSplineLength(connection.spline);
        int sampleCount = Mathf.Max(2, GetSegmentCountForLength(length));
        connection.bakedPolyline.Clear();

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            connection.bakedPolyline.Add(connection.spline.EvaluatePosition(t));
        }
    }

    private void RestoreSplineShape(NodeConnectionData connection)
    {
        if (connection?.spline == null)
            return;

        Spline spline = connection.spline.Spline;
        if (spline == null)
            return;

        spline.Clear();

        if (connection.HasBakedPolyline)
        {
            foreach (Vector3 worldPosition in connection.bakedPolyline)
                spline.Add(new BezierKnot((float3)connection.spline.transform.InverseTransformPoint(worldPosition)));
            return;
        }

        if (connection.pair.A == null || connection.pair.B == null)
            return;

        spline.Add(new BezierKnot((float3)connection.spline.transform.InverseTransformPoint(connection.pair.A.transform.position)));
        spline.Add(new BezierKnot((float3)connection.spline.transform.InverseTransformPoint(connection.pair.B.transform.position)));
    }
#endif

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
            if (connection == null || connection.pair.A == null || connection.pair.B == null)
                return;

            if (connection.spline == null)
            {
                if (connection.HasBakedPolyline)
                    MovePolylineEndpoints(connection.bakedPolyline,
                        connection.pair.A.transform.position, connection.pair.B.transform.position);
                return;
            }

            Spline spline = connection.spline.Spline;
            if (spline == null)
                return;

            if (resetSpline)
                spline.Clear();

            while (spline.Count < 2)
                spline.Add(new BezierKnot(float3.zero));

            var positions = new List<Vector3>(spline.Count);
            for (int i = 0; i < spline.Count; i++)
                positions.Add(connection.spline.transform.TransformPoint((Vector3)spline[i].Position));

            if (!MovePolylineEndpoints(positions, connection.pair.A.transform.position, connection.pair.B.transform.position))
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                Undo.RecordObject(connection.spline, "Move Connection Spline");
#endif
            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot knot = spline[i];
                knot.Position = (float3)connection.spline.transform.InverseTransformPoint(positions[i]);
                spline[i] = knot;
            }
        }

        private bool MovePolylineEndpoints(List<Vector3> positions, Vector3 targetStart, Vector3 targetEnd)
        {
            Vector3 startOffset = targetStart - positions[0];
            Vector3 endOffset = targetEnd - positions[^1];
            if (startOffset.sqrMagnitude < 1e-12f && endOffset.sqrMagnitude < 1e-12f)
                return false;

            // Blend endpoint movement over the original shape, retaining its bends.
            float totalLength = EstimatePolylineLength(positions);
            float distance = 0f;
            Vector3 previous = positions[0];
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 original = positions[i];
                distance += Vector3.Distance(previous, original);
                float t = totalLength > 0f ? distance / totalLength : i / (float)(positions.Count - 1);
                positions[i] = original + Vector3.Lerp(startOffset, endOffset, t);
                previous = original;
            }

            positions[0] = targetStart;
            positions[^1] = targetEnd;
            return true;
        }
        
        private void CreateStateTexture()
        {
            RebuildConnectionIndex();
            _activeConnections.Clear();
            _activeConnectionIds.Clear();
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
                _connectionLengths[i] = EstimateConnectionLength(nodeConnections[i]);

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
            if (node == null || !_connectionIdsByNode.TryGetValue(node, out List<int> connectionIds))
                return;

            bool wasAllocated = _nodeAllocationStates.TryGetValue(node, out bool previousAllocated) && previousAllocated;
            bool isAllocated = node.IsAllocated;
            bool progressChanged = false;

            for (int connectionIndex = 0; connectionIndex < connectionIds.Count; connectionIndex++)
            {
                int i = connectionIds[connectionIndex];
                var connection = nodeConnections[i];
                if (connection.pair.Contains(node))
                {
                    bool connectionChanged = false;
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
                        connectionChanged = true;
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
                        connectionChanged = true;
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
                            connectionChanged = true;
                        }
                    }

                    if (connectionChanged)
                        ScheduleConnectionTransition(i, ref state);
                }
            }
            
            if (progressChanged)
            {
                _stateTexture.Apply(false);
                _progressTexture.Apply(false);
            }

            _nodeAllocationStates[node] = isAllocated;
        }

        private void ScheduleConnectionTransition(int id, ref ConnectionVisualState state)
        {
            if (Mathf.Approximately(state.progress, state.targetProgress))
            {
                state.progress = state.targetProgress;
                FinalizeConnectionVisualState(id, ref state);
                if (_activeConnectionIds.Remove(id))
                    _activeConnections.Remove(id);
            }
            else if (_activeConnectionIds.Add(id))
            {
                _activeConnections.Add(id);
            }
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

            _activeConnections.Clear();
            _activeConnectionIds.Clear();
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

        private void RebuildConnectionIndex()
        {
            _connectionIdsByNode.Clear();
            for (int i = 0; i < nodeConnections.Count; i++)
            {
                var pair = nodeConnections[i].pair;
                AddConnectionIndex(pair.A, i);
                if (pair.B != pair.A)
                    AddConnectionIndex(pair.B, i);
            }
        }

        private void AddConnectionIndex(Node node, int id)
        {
            if (node == null)
                return;
            if (!_connectionIdsByNode.TryGetValue(node, out List<int> ids))
            {
                ids = new List<int>();
                _connectionIdsByNode.Add(node, ids);
            }
            ids.Add(id);
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
            
            if (n1.GetEntityId().CompareTo(n2.GetEntityId()) < 0)
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
                hash = hash * 31 + A.GetEntityId().GetHashCode();
                hash = hash * 31 + B.GetEntityId().GetHashCode();
                return hash;
            }
        }
    }
    
    [Serializable]
    public class NodeConnectionData
    {
        [SerializeField] public NodePair pair;
        [SerializeField] public SplineContainer spline;
        [SerializeField] public List<Vector3> bakedPolyline = new();

        public bool HasBakedPolyline => bakedPolyline != null && bakedPolyline.Count >= 2;

        public void ClearBakedPolyline()
        {
            bakedPolyline?.Clear();
        }
    }
}
