using System;
using System.Collections.Generic;
using UnityEngine;

namespace SkillTree
{
    public class SkillTreeFogOfWarController : MonoBehaviour
    {
        private static readonly int RevealTexId = Shader.PropertyToID("_RevealTex");
        private static readonly int WorldMinId = Shader.PropertyToID("_WorldMin");
        private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");

        [SerializeField] private MainSkillTree skillTree;
        [SerializeField] private Node rootNode;
        [SerializeField] private Renderer fogOverlayRenderer;
        [SerializeField] private bool alwaysRevealRootNode = true;
        [SerializeField] private Vector2Int maskResolution = new(256, 256);
        [SerializeField] [Min(0.01f)] private float defaultRevealRadius = 3f;
        [SerializeField] [Min(0.01f)] private float revealPropagationDuration = 0.35f;
        [SerializeField] [Min(0.01f)] private float revealCellFadeDuration = 0.22f;
        [SerializeField] [Range(0f, 1f)] private float visibilityThreshold = 0.4f;
        [SerializeField] private FogRevealShape defaultRevealShape = FogRevealShape.Square;
        [SerializeField] private Vector2 boundsPadding = new(1f, 1f);

        private readonly HashSet<Node> _discoveredNodes = new();
        private readonly List<Node> _cachedNodes = new();
        private readonly Dictionary<Node, NodePresentationState> _presentationStates = new();

        private MainSkillTree _subscribedSkillTree;
        private Texture2D _revealTexture;
        private Material _runtimeMaterial;
        private bool[] _revealedCells = Array.Empty<bool>();
        private float[] _displayedRevealValues = Array.Empty<float>();
        private float[] _revealStartTimes = Array.Empty<float>();
        private float[] _revealDelays = Array.Empty<float>();
        private Color32[] _maskPixels = Array.Empty<Color32>();
        private Bounds _worldBounds;

        public IReadOnlyCollection<Node> GetDiscoveredNodes()
        {
            return _discoveredNodes;
        }

        public void Bind(MainSkillTree targetSkillTree, Node targetRootNode)
        {
            if (targetSkillTree != null)
                skillTree = targetSkillTree;

            if (targetRootNode != null)
                rootNode = targetRootNode;

            if (!ReferenceEquals(_subscribedSkillTree, skillTree))
            {
                if (_subscribedSkillTree != null)
                    _subscribedSkillTree.OnAnyNodeChanged -= HandleNodeChanged;

                _subscribedSkillTree = skillTree;

                if (_subscribedSkillTree != null)
                    _subscribedSkillTree.OnAnyNodeChanged += HandleNodeChanged;
            }

            CacheNodes();
            RecalculateWorldBounds();
            EnsureMaskStorage();
            EnsureRuntimeMaterial();
            RebuildMaskFromDiscoveredNodes(true);
            RefreshNodeVisibility();
            UpdateOverlayMaterialProperties();
        }

        public void SetDiscoveredNodes(IEnumerable<Node> nodes)
        {
            _discoveredNodes.Clear();

            if (nodes != null)
            {
                foreach (Node node in nodes)
                {
                    if (node != null)
                        _discoveredNodes.Add(node);
                }
            }

            RebuildMaskFromDiscoveredNodes(true);
            RefreshNodeVisibility();
        }

        public bool IsNodeDiscovered(Node node)
        {
            return node != null && IsWorldPositionDiscovered(node.transform.position);
        }

        public bool IsWorldPositionDiscovered(Vector3 worldPosition)
        {
            if (_revealedCells.Length == 0)
                return false;

            Vector2 normalized = WorldToNormalized(worldPosition);
            if (normalized.x < 0f || normalized.x > 1f || normalized.y < 0f || normalized.y > 1f)
                return false;

            int width = Mathf.Max(1, maskResolution.x);
            int height = Mathf.Max(1, maskResolution.y);
            int x = Mathf.Clamp(Mathf.FloorToInt(normalized.x * width), 0, width - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(normalized.y * height), 0, height - 1);
            int index = y * width + x;

            return index >= 0
                   && index < _displayedRevealValues.Length
                   && _displayedRevealValues[index] >= visibilityThreshold;
        }

        private void Awake()
        {
            Bind(skillTree, rootNode);
            UpdateFogOverlayVisibility();
        }

        private void Update()
        {
            UpdateFogOverlayVisibility();

            if (!Application.isPlaying || !UpdateRevealAnimation())
                return;

            UploadRevealTexture();
            RefreshNodeVisibility();
        }

        private void OnDestroy()
        {
            if (_subscribedSkillTree != null)
                _subscribedSkillTree.OnAnyNodeChanged -= HandleNodeChanged;

            ReleaseTexture();
            ReleaseRuntimeMaterial();
        }

        private void OnValidate()
        {
            maskResolution.x = Mathf.Max(1, maskResolution.x);
            maskResolution.y = Mathf.Max(1, maskResolution.y);
            defaultRevealRadius = Mathf.Max(0.01f, defaultRevealRadius);
            revealPropagationDuration = Mathf.Max(0.01f, revealPropagationDuration);
            revealCellFadeDuration = Mathf.Max(0.01f, revealCellFadeDuration);
            visibilityThreshold = Mathf.Clamp01(visibilityThreshold);
            boundsPadding.x = Mathf.Max(0f, boundsPadding.x);
            boundsPadding.y = Mathf.Max(0f, boundsPadding.y);
            UpdateFogOverlayVisibility();
        }

        private void HandleNodeChanged(Node node)
        {
            if (node == null || !node.IsActive)
                return;

            if (!_discoveredNodes.Add(node))
                return;

            StampRevealSource(node, true);
            UploadRevealTexture();
            RefreshNodeVisibility();
        }

        private void CacheNodes()
        {
            _cachedNodes.Clear();

            if (rootNode == null)
                return;

            HashSet<Node> visitedNodes = new();

            NodeGraphTraversalService.Traverse(rootNode, node =>
            {
                if (node == null)
                    return;

                _cachedNodes.Add(node);
                visitedNodes.Add(node);

                if (!_presentationStates.ContainsKey(node))
                    _presentationStates[node] = new NodePresentationState(node.gameObject);
            });

            List<Node> obsoleteNodes = null;
            foreach (Node node in _presentationStates.Keys)
            {
                if (visitedNodes.Contains(node))
                    continue;

                obsoleteNodes ??= new List<Node>();
                obsoleteNodes.Add(node);
            }

            if (obsoleteNodes == null)
                return;

            for (int i = 0; i < obsoleteNodes.Count; i++)
                _presentationStates.Remove(obsoleteNodes[i]);
        }

        private void RecalculateWorldBounds()
        {
            if (_cachedNodes.Count == 0)
            {
                _worldBounds = new Bounds(transform.position, Vector3.one);
                return;
            }

            Vector3 min = _cachedNodes[0].transform.position;
            Vector3 max = min;
            float maxRevealRadius = defaultRevealRadius;

            for (int i = 0; i < _cachedNodes.Count; i++)
            {
                Node node = _cachedNodes[i];
                Vector3 position = node.transform.position;
                min = Vector3.Min(min, position);
                max = Vector3.Max(max, position);

                NodeFogRevealSource revealSource = node.GetComponent<NodeFogRevealSource>();
                if (revealSource != null)
                    maxRevealRadius = Mathf.Max(maxRevealRadius, revealSource.GetRevealRadius(defaultRevealRadius));
            }

            Vector2 totalPadding = boundsPadding + Vector2.one * maxRevealRadius;
            min -= new Vector3(totalPadding.x, totalPadding.y, 0f);
            max += new Vector3(totalPadding.x, totalPadding.y, 0f);

            Vector3 size = max - min;
            size.x = Mathf.Max(size.x, 0.01f);
            size.y = Mathf.Max(size.y, 0.01f);
            size.z = 0.01f;

            _worldBounds = new Bounds(min + size * 0.5f, size);
        }

        private void EnsureMaskStorage()
        {
            int width = Mathf.Max(1, maskResolution.x);
            int height = Mathf.Max(1, maskResolution.y);
            int cellCount = width * height;

            if (_revealedCells.Length != cellCount)
                _revealedCells = new bool[cellCount];

            if (_displayedRevealValues.Length != cellCount)
                _displayedRevealValues = new float[cellCount];

            if (_revealStartTimes.Length != cellCount)
                _revealStartTimes = new float[cellCount];

            if (_revealDelays.Length != cellCount)
                _revealDelays = new float[cellCount];

            if (_maskPixels.Length != cellCount)
                _maskPixels = new Color32[cellCount];

            if (_revealTexture != null && _revealTexture.width == width && _revealTexture.height == height)
                return;

            ReleaseTexture();

            _revealTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                name = "SkillTreeFogRevealMask"
            };
        }

        private void EnsureRuntimeMaterial()
        {
            if (fogOverlayRenderer == null || _runtimeMaterial != null)
                return;

            Material sharedMaterial = fogOverlayRenderer.sharedMaterial;
            if (sharedMaterial == null)
                return;

            _runtimeMaterial = new Material(sharedMaterial)
            {
                name = $"{sharedMaterial.name} (Runtime Fog)"
            };

            fogOverlayRenderer.sharedMaterial = _runtimeMaterial;
        }

        private void ReleaseTexture()
        {
            if (_revealTexture == null)
                return;

            if (Application.isPlaying)
                Destroy(_revealTexture);
            else
                DestroyImmediate(_revealTexture);

            _revealTexture = null;
        }

        private void ReleaseRuntimeMaterial()
        {
            if (_runtimeMaterial == null)
                return;

            if (Application.isPlaying)
                Destroy(_runtimeMaterial);
            else
                DestroyImmediate(_runtimeMaterial);

            _runtimeMaterial = null;
        }

        private void RebuildMaskFromDiscoveredNodes(bool instant)
        {
            EnsureMaskStorage();

            if (_revealedCells.Length == 0)
                return;

            Array.Clear(_revealedCells, 0, _revealedCells.Length);
            Array.Clear(_displayedRevealValues, 0, _displayedRevealValues.Length);
            Array.Clear(_revealStartTimes, 0, _revealStartTimes.Length);
            Array.Clear(_revealDelays, 0, _revealDelays.Length);

            foreach (Node node in EnumerateRevealSources())
                StampRevealSource(node, !instant);

            UploadRevealTexture();
        }

        private IEnumerable<Node> EnumerateRevealSources()
        {
            if (alwaysRevealRootNode && rootNode != null)
                yield return rootNode;

            foreach (Node node in _discoveredNodes)
            {
                if (node != null)
                    yield return node;
            }
        }

        private void StampRevealSource(Node node, bool animate)
        {
            if (node == null || _revealedCells.Length == 0)
                return;

            float radius = defaultRevealRadius;
            FogRevealShape shape = defaultRevealShape;

            NodeFogRevealSource revealSource = node.GetComponent<NodeFogRevealSource>();
            if (revealSource != null)
            {
                radius = revealSource.GetRevealRadius(defaultRevealRadius);
                shape = revealSource.RevealShape;
            }

            int width = Mathf.Max(1, maskResolution.x);
            int height = Mathf.Max(1, maskResolution.y);
            Vector3 position = node.transform.position;

            Vector2 minNormalized = WorldToNormalized(position - new Vector3(radius, radius, 0f));
            Vector2 maxNormalized = WorldToNormalized(position + new Vector3(radius, radius, 0f));

            int minX = Mathf.Clamp(Mathf.FloorToInt(minNormalized.x * width), 0, width - 1);
            int maxX = Mathf.Clamp(Mathf.CeilToInt(maxNormalized.x * width), 0, width - 1);
            int minY = Mathf.Clamp(Mathf.FloorToInt(minNormalized.y * height), 0, height - 1);
            int maxY = Mathf.Clamp(Mathf.CeilToInt(maxNormalized.y * height), 0, height - 1);

            float radiusSqr = radius * radius;
            float currentTime = Application.isPlaying ? Time.time : 0f;
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 cellCenter = GetCellWorldCenter(x, y);
                    Vector2 delta = cellCenter - (Vector2)position;

                    bool shouldReveal = shape == FogRevealShape.Square
                        ? Mathf.Abs(delta.x) <= radius && Mathf.Abs(delta.y) <= radius
                        : delta.sqrMagnitude <= radiusSqr;

                    if (!shouldReveal)
                        continue;

                    int index = y * width + x;
                    if (_revealedCells[index])
                        continue;

                    _revealedCells[index] = true;

                    if (!animate || !Application.isPlaying)
                    {
                        _displayedRevealValues[index] = 1f;
                        _revealStartTimes[index] = currentTime;
                        _revealDelays[index] = 0f;
                        continue;
                    }

                    float normalizedDistance = shape == FogRevealShape.Square
                        ? Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) / Mathf.Max(radius, 0.001f)
                        : Mathf.Sqrt(delta.sqrMagnitude) / Mathf.Max(radius, 0.001f);

                    _revealStartTimes[index] = currentTime;
                    _revealDelays[index] = Mathf.Clamp01(normalizedDistance) * revealPropagationDuration;
                }
            }
        }

        private void UploadRevealTexture()
        {
            if (_revealTexture == null || _maskPixels.Length != _displayedRevealValues.Length)
                return;

            for (int i = 0; i < _displayedRevealValues.Length; i++)
            {
                byte intensity = (byte)Mathf.RoundToInt(Mathf.Clamp01(_displayedRevealValues[i]) * 255f);
                _maskPixels[i] = new Color32(intensity, intensity, intensity, 255);
            }

            _revealTexture.SetPixels32(_maskPixels);
            _revealTexture.Apply(false, false);
            UpdateOverlayMaterialProperties();
        }

        private bool UpdateRevealAnimation()
        {
            if (_displayedRevealValues.Length == 0)
                return false;

            bool hasChanges = false;
            float currentTime = Time.time;

            for (int i = 0; i < _displayedRevealValues.Length; i++)
            {
                if (!_revealedCells[i] || _displayedRevealValues[i] >= 1f)
                    continue;

                float revealStartTime = _revealStartTimes[i] + _revealDelays[i];
                if (currentTime <= revealStartTime)
                    continue;

                float t = Mathf.Clamp01((currentTime - revealStartTime) / revealCellFadeDuration);
                float smoothedT = t * t * (3f - 2f * t);
                if (smoothedT <= _displayedRevealValues[i])
                    continue;

                _displayedRevealValues[i] = smoothedT;
                hasChanges = true;
            }

            return hasChanges;
        }

        private void UpdateOverlayMaterialProperties()
        {
            if (_runtimeMaterial == null)
                return;

            if (_runtimeMaterial.HasProperty(RevealTexId))
                _runtimeMaterial.SetTexture(RevealTexId, _revealTexture);

            if (_runtimeMaterial.HasProperty(WorldMinId))
                _runtimeMaterial.SetVector(WorldMinId, new Vector4(_worldBounds.min.x, _worldBounds.min.y, 0f, 0f));

            if (_runtimeMaterial.HasProperty(WorldSizeId))
                _runtimeMaterial.SetVector(
                    WorldSizeId,
                    new Vector4(
                        Mathf.Max(_worldBounds.size.x, 0.01f),
                        Mathf.Max(_worldBounds.size.y, 0.01f),
                        0f,
                        0f));
        }

        private void RefreshNodeVisibility()
        {
            for (int i = 0; i < _cachedNodes.Count; i++)
            {
                Node node = _cachedNodes[i];
                if (node == null || !_presentationStates.TryGetValue(node, out NodePresentationState presentationState))
                    continue;

                presentationState.SetVisible(IsNodeDiscovered(node));
            }
        }

        private Vector2 WorldToNormalized(Vector3 worldPosition)
        {
            float sizeX = Mathf.Max(_worldBounds.size.x, 0.01f);
            float sizeY = Mathf.Max(_worldBounds.size.y, 0.01f);

            return new Vector2(
                (worldPosition.x - _worldBounds.min.x) / sizeX,
                (worldPosition.y - _worldBounds.min.y) / sizeY);
        }

        private Vector2 GetCellWorldCenter(int x, int y)
        {
            float width = Mathf.Max(1, maskResolution.x);
            float height = Mathf.Max(1, maskResolution.y);

            return new Vector2(
                _worldBounds.min.x + ((x + 0.5f) / width) * _worldBounds.size.x,
                _worldBounds.min.y + ((y + 0.5f) / height) * _worldBounds.size.y);
        }

        private void UpdateFogOverlayVisibility()
        {
            if (fogOverlayRenderer != null)
                fogOverlayRenderer.enabled = Application.isPlaying;
        }

        private sealed class NodePresentationState
        {
            private readonly Renderer[] _renderers;
            private readonly bool[] _rendererEnabledStates;
            private readonly Collider[] _colliders;
            private readonly bool[] _colliderEnabledStates;
            private readonly Collider2D[] _colliders2D;
            private readonly bool[] _collider2DEnabledStates;

            public NodePresentationState(GameObject nodeObject)
            {
                _renderers = nodeObject.GetComponentsInChildren<Renderer>(true);
                _rendererEnabledStates = new bool[_renderers.Length];
                for (int i = 0; i < _renderers.Length; i++)
                    _rendererEnabledStates[i] = _renderers[i] != null && _renderers[i].enabled;

                _colliders = nodeObject.GetComponentsInChildren<Collider>(true);
                _colliderEnabledStates = new bool[_colliders.Length];
                for (int i = 0; i < _colliders.Length; i++)
                    _colliderEnabledStates[i] = _colliders[i] != null && _colliders[i].enabled;

                _colliders2D = nodeObject.GetComponentsInChildren<Collider2D>(true);
                _collider2DEnabledStates = new bool[_colliders2D.Length];
                for (int i = 0; i < _colliders2D.Length; i++)
                    _collider2DEnabledStates[i] = _colliders2D[i] != null && _colliders2D[i].enabled;
            }

            public void SetVisible(bool isVisible)
            {
                for (int i = 0; i < _renderers.Length; i++)
                {
                    if (_renderers[i] != null)
                        _renderers[i].enabled = isVisible && _rendererEnabledStates[i];
                }

                for (int i = 0; i < _colliders.Length; i++)
                {
                    if (_colliders[i] != null)
                        _colliders[i].enabled = isVisible && _colliderEnabledStates[i];
                }

                for (int i = 0; i < _colliders2D.Length; i++)
                {
                    if (_colliders2D[i] != null)
                        _colliders2D[i].enabled = isVisible && _collider2DEnabledStates[i];
                }
            }
        }
    }
}
