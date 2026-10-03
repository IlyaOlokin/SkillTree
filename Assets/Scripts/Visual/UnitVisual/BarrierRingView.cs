using Battle;
using DG.Tweening;
using UnityEngine;

namespace Visual
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed class BarrierRingView : MonoBehaviour
    {
        private const string ShaderName = "SkillTree/Barrier Rings SDF";
        private const string GeneratedMeshName = "Barrier Rings Quad";
        private const int MaxSupportedRingCount = 32;

        private static readonly int ActiveCountId = Shader.PropertyToID("_ActiveCount");
        private static readonly int MaxCountId = Shader.PropertyToID("_MaxCount");
        private static readonly int BrokenProgressId = Shader.PropertyToID("_BrokenProgress");
        private static readonly int SegmentDirectionId = Shader.PropertyToID("_SegmentDirection");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
        private static readonly int InactiveColorId = Shader.PropertyToID("_InactiveColor");
        private static readonly int OuterRadiusId = Shader.PropertyToID("_OuterRadius");
        private static readonly int InnerRadiusId = Shader.PropertyToID("_InnerRadius");
        private static readonly int RingThicknessFractionId = Shader.PropertyToID("_RingThicknessFraction");
        private static readonly int RingGapId = Shader.PropertyToID("_RingGap");
        private static readonly int SegmentsPerLayerId = Shader.PropertyToID("_SegmentsPerLayer");
        private static readonly int SegmentGapId = Shader.PropertyToID("_SegmentGap");
        private static readonly int InnerFadeId = Shader.PropertyToID("_InnerFade");
        private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int ActiveAlphaId = Shader.PropertyToID("_ActiveAlpha");
        private static readonly int GlowAlphaId = Shader.PropertyToID("_GlowAlpha");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");

        [SerializeField] private Barrier barrier;
        [SerializeField] private Material material;
        [SerializeField, Min(0.1f)] private float visualSize = 2.05f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder = -1;

        [Header("Preview")]
        [SerializeField] private bool previewInEditMode = true;
        [SerializeField, Min(0)] private int previewActiveCount = 5;
        [SerializeField, Min(0)] private int previewMaxCount = 8;

        [Header("Rings")]
        [SerializeField, Range(0.05f, 0.95f)] private float outerRadius = 0.78f;
        [SerializeField, Range(0f, 0.85f)] private float innerRadius = 0.26f;
        [SerializeField, Range(0.05f, 1.35f)] private float ringThicknessFraction = 0.9f;
        [SerializeField, Range(-0.08f, 0.08f)] private float ringGap = -0.01f;
        [SerializeField, Range(1, 16)] private int segmentsPerLayer = 5;
        [SerializeField, Range(0f, 0.35f)] private float segmentGap = 0.06f;
        [SerializeField, Range(0.001f, 1f)] private float innerFade = 0.32f;
        [SerializeField, Range(0.001f, 0.3f)] private float glowWidth = 0.035f;

        [Header("Colors")]
        [SerializeField, ColorUsage(true, true)] private Color activeColor = new Color(0.35f, 2.25f, 3.1f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color glowColor = new Color(0.2f, 1.55f, 3.4f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color inactiveColor = new Color(0.16f, 0.34f, 0.42f, 0.34f);
        [SerializeField, Range(0f, 1f)] private float activeAlpha = 0.34f;
        [SerializeField, Range(0f, 1f)] private float glowAlpha = 0.18f;

        [Header("Motion")]
        [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
        [SerializeField, Range(0f, 8f)] private float pulseSpeed = 1.1f;
        [SerializeField, Min(0f)] private float segmentRotationDuration = 0.22f;
        [SerializeField] private Ease segmentRotationEase = Ease.OutCubic;
        [SerializeField] private bool reverseSegmentDirection;
        [SerializeField] private float rotation;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Barrier subscribedBarrier;
        private float appliedMeshSize = -1f;
        private int currentActiveCount;
        private int currentMaxCount;
        private float brokenProgress;
        private float targetBrokenProgress;
        private bool hasInitializedBrokenProgress;
        private Tween brokenProgressTween;

        private void Reset()
        {
            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            ResolveBarrier();
            RefreshCounts();
            ApplyProperties();
        }

        private void OnEnable()
        {
            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            ResolveBarrier();
            SubscribeToBarrier();
            RefreshCounts();
            ApplyProperties();
        }

        private void OnDisable()
        {
            KillBrokenProgressTween();
            UnsubscribeFromBarrier();
            hasInitializedBrokenProgress = false;
        }

        private void OnValidate()
        {
            previewMaxCount = Mathf.Max(0, previewMaxCount);
            previewActiveCount = Mathf.Clamp(previewActiveCount, 0, previewMaxCount);
            visualSize = Mathf.Max(0.1f, visualSize);
            outerRadius = Mathf.Clamp(outerRadius, 0.05f, 0.95f);
            innerRadius = Mathf.Clamp(innerRadius, 0f, outerRadius - 0.01f);

            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            RefreshCounts();
            ApplyProperties();
        }

        public void SetBarrier(Barrier nextBarrier)
        {
            if (barrier == nextBarrier)
            {
                return;
            }

            UnsubscribeFromBarrier();
            barrier = nextBarrier;
            SubscribeToBarrier();
            RefreshCounts();
            ApplyProperties();
        }

        public void SetCounts(int activeCount, int maxCount)
        {
            SetCountsWithoutApplying(activeCount, maxCount);
            ApplyProperties();
        }

        private void CacheComponents()
        {
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }

            propertyBlock ??= new MaterialPropertyBlock();
        }

        private void EnsureMaterial()
        {
            if (meshRenderer == null)
            {
                return;
            }

            if (material != null)
            {
                meshRenderer.sharedMaterial = material;
            }
            else if (meshRenderer.sharedMaterial == null)
            {
                Shader shader = Shader.Find(ShaderName);
                if (shader == null)
                {
                    return;
                }

                meshRenderer.sharedMaterial = new Material(shader)
                {
                    name = "M_BarrierRings_Runtime",
                    hideFlags = HideFlags.DontSave
                };
            }

            meshRenderer.sortingLayerName = sortingLayerName;
            meshRenderer.sortingOrder = sortingOrder;
        }

        private void EnsureMesh()
        {
            if (meshFilter == null)
            {
                return;
            }

            Mesh currentMesh = meshFilter.sharedMesh;
            if (currentMesh != null && currentMesh.name == GeneratedMeshName && Mathf.Approximately(appliedMeshSize, visualSize))
            {
                return;
            }

            meshFilter.sharedMesh = CreateQuadMesh(visualSize);
            appliedMeshSize = visualSize;
        }

        private Mesh CreateQuadMesh(float size)
        {
            float halfSize = size * 0.5f;
            Mesh mesh = new Mesh
            {
                name = GeneratedMeshName,
                hideFlags = HideFlags.DontSave
            };

            mesh.vertices = new[]
            {
                new Vector3(-halfSize, -halfSize, 0f),
                new Vector3(-halfSize, halfSize, 0f),
                new Vector3(halfSize, halfSize, 0f),
                new Vector3(halfSize, -halfSize, 0f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(1f, 0f)
            };

            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateBounds();
            return mesh;
        }

        private void ResolveBarrier()
        {
            if (barrier == null)
            {
                barrier = FindBarrierInParents();
            }
        }

        private void SubscribeToBarrier()
        {
            Barrier nextBarrier = barrier != null ? barrier : FindBarrierInParents();
            if (subscribedBarrier == nextBarrier)
            {
                return;
            }

            UnsubscribeFromBarrier();
            subscribedBarrier = nextBarrier;
            if (subscribedBarrier != null)
            {
                subscribedBarrier.OnMaxBarrierChanged += HandleBarrierChanged;
                subscribedBarrier.OnBarrierCountChanged += HandleBarrierChanged;
            }
        }

        private void UnsubscribeFromBarrier()
        {
            if (subscribedBarrier == null)
            {
                return;
            }

            subscribedBarrier.OnMaxBarrierChanged -= HandleBarrierChanged;
            subscribedBarrier.OnBarrierCountChanged -= HandleBarrierChanged;
            subscribedBarrier = null;
        }

        private Barrier FindBarrierInParents()
        {
            Barrier parentBarrier = GetComponentInParent<Barrier>();
            if (parentBarrier != null)
            {
                return parentBarrier;
            }

            Unit parentUnit = GetComponentInParent<Unit>();
            return parentUnit != null ? parentUnit.barrier : null;
        }

        private void HandleBarrierChanged()
        {
            RefreshCounts();
            ApplyProperties();
        }

        private void RefreshCounts()
        {
            if (!Application.isPlaying && previewInEditMode)
            {
                SetCountsWithoutApplying(previewActiveCount, previewMaxCount);
                return;
            }

            if (barrier == null)
            {
                SetCountsWithoutApplying(0, 0);
                return;
            }

            SetCountsWithoutApplying(barrier.BarrierCount, barrier.MaxBarrierCount);
        }

        private void SetCountsWithoutApplying(int activeCount, int maxCount)
        {
            currentMaxCount = Mathf.Clamp(maxCount, 0, MaxSupportedRingCount);
            currentActiveCount = Mathf.Clamp(activeCount, 0, currentMaxCount);
            float nextBrokenProgress = currentMaxCount - currentActiveCount;

            if (!Application.isPlaying || !hasInitializedBrokenProgress)
            {
                KillBrokenProgressTween();
                targetBrokenProgress = nextBrokenProgress;
                brokenProgress = targetBrokenProgress;
                hasInitializedBrokenProgress = true;
                return;
            }

            if (Mathf.Approximately(targetBrokenProgress, nextBrokenProgress))
            {
                return;
            }

            targetBrokenProgress = nextBrokenProgress;
            PlayBrokenProgressTween();
        }

        private void PlayBrokenProgressTween()
        {
            KillBrokenProgressTween();

            if (segmentRotationDuration <= 0f)
            {
                brokenProgress = targetBrokenProgress;
                ApplyProperties();
                return;
            }

            brokenProgressTween = DOTween
                .To(
                    () => brokenProgress,
                    value =>
                    {
                        brokenProgress = value;
                        ApplyProperties();
                    },
                    targetBrokenProgress,
                    segmentRotationDuration)
                .SetEase(segmentRotationEase)
                .SetTarget(this);
        }

        private void KillBrokenProgressTween()
        {
            brokenProgressTween?.Kill();
            brokenProgressTween = null;
        }

        private void ApplyProperties()
        {
            CacheComponents();

            if (meshRenderer == null)
            {
                return;
            }

            propertyBlock.Clear();
            propertyBlock.SetFloat(ActiveCountId, currentActiveCount);
            propertyBlock.SetFloat(MaxCountId, currentMaxCount);
            propertyBlock.SetFloat(BrokenProgressId, brokenProgress);
            propertyBlock.SetFloat(SegmentDirectionId, reverseSegmentDirection ? -1f : 1f);
            propertyBlock.SetColor(ColorId, activeColor);
            propertyBlock.SetColor(GlowColorId, glowColor);
            propertyBlock.SetColor(InactiveColorId, inactiveColor);
            propertyBlock.SetFloat(OuterRadiusId, outerRadius);
            propertyBlock.SetFloat(InnerRadiusId, innerRadius);
            propertyBlock.SetFloat(RingThicknessFractionId, ringThicknessFraction);
            propertyBlock.SetFloat(RingGapId, ringGap);
            propertyBlock.SetFloat(SegmentsPerLayerId, Mathf.Max(1, segmentsPerLayer));
            propertyBlock.SetFloat(SegmentGapId, segmentGap);
            propertyBlock.SetFloat(InnerFadeId, innerFade);
            propertyBlock.SetFloat(GlowWidthId, glowWidth);
            propertyBlock.SetFloat(GlowIntensityId, currentActiveCount > 0 ? 1f : 0f);
            propertyBlock.SetFloat(ActiveAlphaId, activeAlpha);
            propertyBlock.SetFloat(GlowAlphaId, glowAlpha);
            propertyBlock.SetFloat(PulseAmountId, pulseAmount);
            propertyBlock.SetFloat(PulseSpeedId, pulseSpeed);
            propertyBlock.SetFloat(RotationId, rotation);
            meshRenderer.enabled = currentMaxCount > 0;
            meshRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
