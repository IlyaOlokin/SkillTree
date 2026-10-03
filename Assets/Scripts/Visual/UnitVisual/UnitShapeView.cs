using System;
using System.Collections.Generic;
using Battle;
using UnityEngine;

namespace Visual
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public sealed partial class UnitShapeView : MonoBehaviour
    {
        private const string ShaderName = "SkillTree/Unit Shape SDF";
        private const string GeneratedMeshName = "Unit Shape Quad";
        private const float DefaultArmorMarkCountMin = 4f;
        private const float DefaultArmorMarkCountMax = 32f;
        private const float DefaultCritSpikeCountMin = 4f;
        private const float DefaultCritSpikeCountMax = 12f;
        private const float DefaultMaximumHealthMin = 100f;
        private const float DefaultMaximumHealthMax = 400f;
        private const float DefaultCenterDotRadiusMin = 0.035f;
        private const float DefaultCenterDotRadiusMax = 0.09f;
        private const float DefaultArmorMarkWidth = 0.035f;
        private const float DefaultArmorMarkHeight = 0.065f;
        private const float DefaultCritSpikeWidth = 0.075f;
        private const float DefaultCritSpikeTipWidth = 0.012f;
        private const float DefaultCritSpikeHeight = 0.2f;
        private const float MaxCenterDotRadius = 0.9f;
        private const int CurrentStatVisualBindingsVersion = 2;

        private static readonly int ShapeTypeId = Shader.PropertyToID("_ShapeType");
        private static readonly int TriangleSizeId = Shader.PropertyToID("_TriangleSize");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");
        private static readonly int FillColorId = Shader.PropertyToID("_FillColor");
        private static readonly int CenterDotRadiusId = Shader.PropertyToID("_CenterDotRadius");
        private static readonly int CenterGlowRadiusId = Shader.PropertyToID("_CenterGlowRadius");
        private static readonly int ArmorId = Shader.PropertyToID("_Armor");
        private static readonly int CritId = Shader.PropertyToID("_Crit");
        private static readonly int MarkCountId = Shader.PropertyToID("_MarkCount");
        private static readonly int MarkWidthReferenceCountId = Shader.PropertyToID("_MarkWidthReferenceCount");
        private static readonly int MarkWidthId = Shader.PropertyToID("_MarkWidth");
        private static readonly int MarkLengthId = Shader.PropertyToID("_MarkLength");
        private static readonly int SpikeCountId = Shader.PropertyToID("_SpikeCount");
        private static readonly int SpikeWidthReferenceCountId = Shader.PropertyToID("_SpikeWidthReferenceCount");
        private static readonly int SpikeWidthId = Shader.PropertyToID("_SpikeWidth");
        private static readonly int SpikeTipWidthId = Shader.PropertyToID("_SpikeTipWidth");
        private static readonly int SpikeLengthId = Shader.PropertyToID("_SpikeLength");
        private static readonly int RotationId = Shader.PropertyToID("_Rotation");
        private static readonly int MirrorXId = Shader.PropertyToID("_MirrorX");
        private static readonly int MirrorYId = Shader.PropertyToID("_MirrorY");
        private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int ContourSpinSpeedId = Shader.PropertyToID("_ContourSpinSpeed");

        [SerializeField] private UnitShapeType shapeType;
        [SerializeField] private Material material;
        [SerializeField, Min(0.1f)] private float visualSize = 1.6f;
        [SerializeField, Range(0.45f, 1.2f)] private float triangleSize = 0.72f;
        [SerializeField] private string sortingLayerName = "Default";
        [SerializeField] private int sortingOrder;

        [Header("Stat Source")]
        [SerializeField] private Unit unit;

        [Header("Stat Visual Bindings")]
        [SerializeField] private bool previewBindingsInEditMode = true;
        [SerializeField] private List<UnitStatVisualBinding> statVisualBindings = CreateDefaultStatVisualBindings();
        [SerializeField, HideInInspector] private int statVisualBindingsVersion;

        [Header("Armor Marks")]
        [SerializeField, Range(0f, 48f)] private float armorMarkCount = DefaultArmorMarkCountMin;
        [SerializeField, Range(0.001f, 0.25f)] private float armorMarkWidth = DefaultArmorMarkWidth;
        [SerializeField, Range(0.001f, 0.2f)] private float armorMarkHeight = DefaultArmorMarkHeight;

        [Header("Crit Spikes")]
        [SerializeField, Range(0f, 48f)] private float critSpikeCount = DefaultCritSpikeCountMin;
        [SerializeField, Range(0.001f, 0.25f)] private float critSpikeWidth = DefaultCritSpikeWidth;
        [SerializeField, Range(0.001f, 0.12f)] private float critSpikeTipWidth = DefaultCritSpikeTipWidth;
        [SerializeField, Range(0.001f, 0.45f)] private float critSpikeHeight = DefaultCritSpikeHeight;

        [Header("Colors")]
        [SerializeField, ColorUsage(true, true)] private Color glowColor = new Color(3.2f, 1.35f, 0.12f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color coreColor = new Color(1f, 0.92f, 0.82f, 1f);
        [SerializeField, ColorUsage(false, true)] private Color fillColor = new Color(0.03f, 0.024f, 0.012f, 0.78f);

        [Header("Motion")]
        [SerializeField] private bool mirrorX;
        [SerializeField] private bool mirrorY;
        [SerializeField] private float rotation;
        [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.08f;
        [SerializeField, Range(0f, 8f)] private float pulseSpeed = 1.35f;
        [SerializeField] private float contourSpinSpeed = 0.08f;

        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Unit subscribedUnit;
        private float appliedMeshSize = -1f;
        private float armor;
        private float crit;
        private float armorMarkWidthReferenceCount = DefaultArmorMarkCountMin;
        private float critSpikeWidthReferenceCount = DefaultCritSpikeCountMin;
        private float centerDotRadius = DefaultCenterDotRadiusMin;
        private float centerGlowRadius;
        private bool hasCenterGlowRadiusOverride;
        private float appliedArmorMarkCount;
        private float appliedArmorMarkWidth;
        private float appliedArmorMarkHeight;
        private float appliedCritSpikeCount;
        private float appliedCritSpikeWidth;
        private float appliedCritSpikeTipWidth;
        private float appliedCritSpikeHeight;

        public UnitShapeType ShapeType => shapeType;
        public float Armor => armor;
        public float Crit => crit;
        public Color GlowColor => glowColor;
        public Color CoreColor => coreColor;
        public Color FillColor => fillColor;

        private void Reset()
        {
            EnsureWispMotionSettings();
            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            ResetAppliedShapeControls();
            ApplyProperties();
        }

        private void OnEnable()
        {
            EnsureWispMotionSettings();
            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            EnsureStatVisualBindings();
            ResolveUnit();
            SubscribeToUnit();
            ResetAppliedShapeControls();
            ApplyStatVisualBindingsFromUnit();
            ApplyProperties();
            RefreshWisps();
        }

        private void OnDisable()
        {
            UnsubscribeFromUnit();
            DestroyWisps();
        }

        private void OnValidate()
        {
            EnsureWispMotionSettings();
            armor = Mathf.Clamp01(armor);
            crit = Mathf.Clamp01(crit);
            armorMarkCount = Mathf.Max(0f, armorMarkCount);
            armorMarkWidth = Mathf.Clamp(armorMarkWidth, 0.001f, 0.25f);
            armorMarkHeight = Mathf.Clamp(armorMarkHeight, 0.001f, 0.2f);
            armorMarkWidthReferenceCount = Mathf.Max(1f, armorMarkWidthReferenceCount);
            critSpikeCount = Mathf.Max(0f, critSpikeCount);
            critSpikeWidth = Mathf.Clamp(critSpikeWidth, 0.001f, 0.25f);
            critSpikeTipWidth = Mathf.Clamp(critSpikeTipWidth, 0.001f, 0.12f);
            critSpikeHeight = Mathf.Clamp(critSpikeHeight, 0.001f, 0.45f);
            critSpikeWidthReferenceCount = Mathf.Max(1f, critSpikeWidthReferenceCount);
            centerDotRadius = Mathf.Clamp(centerDotRadius, 0f, MaxCenterDotRadius);
            visualSize = Mathf.Max(0.1f, visualSize);
            triangleSize = Mathf.Clamp(triangleSize, 0.45f, 1.2f);

            CacheComponents();
            EnsureMaterial();
            EnsureMesh();
            EnsureStatVisualBindings();
            ResetAppliedShapeControls();
            ApplyStatVisualBindingsFromUnit();
            ApplyProperties();
        }

        public void SetShape(UnitShapeType nextShape)
        {
            shapeType = nextShape;
            ApplyProperties();
        }

        public void SetStats(float nextArmor, float nextCrit)
        {
            ResetAppliedShapeControls();
            armor = Mathf.Clamp01(nextArmor);
            crit = Mathf.Clamp01(nextCrit);
            appliedArmorMarkCount = Mathf.Lerp(DefaultArmorMarkCountMin, DefaultArmorMarkCountMax, armor);
            armorMarkWidthReferenceCount = DefaultArmorMarkCountMin;
            appliedCritSpikeCount = Mathf.Lerp(DefaultCritSpikeCountMin, DefaultCritSpikeCountMax, crit);
            critSpikeWidthReferenceCount = DefaultCritSpikeCountMin;
            ApplyProperties();
        }

        public void SetStats(float nextArmor, float nextCrit, float nextMaximumHealth)
        {
            SetStats(nextArmor, nextCrit);
            SetMaximumHealthWithoutApplying(nextMaximumHealth);
            ApplyProperties();
        }

        public void SetMaximumHealth(float nextMaximumHealth)
        {
            SetMaximumHealthWithoutApplying(nextMaximumHealth);
            ApplyProperties();
        }

        public void SetArmorMarks(float count, float width, float height)
        {
            armorMarkCount = Mathf.Max(0f, count);
            armorMarkWidth = Mathf.Clamp(width, 0.001f, 0.25f);
            armorMarkHeight = Mathf.Clamp(height, 0.001f, 0.2f);
            appliedArmorMarkCount = armorMarkCount;
            appliedArmorMarkWidth = armorMarkWidth;
            appliedArmorMarkHeight = armorMarkHeight;
            armorMarkWidthReferenceCount = DefaultArmorMarkCountMin;
            ApplyProperties();
        }

        public void SetCritSpikes(float count, float width, float height)
        {
            critSpikeCount = Mathf.Max(0f, count);
            critSpikeWidth = Mathf.Clamp(width, 0.001f, 0.25f);
            critSpikeHeight = Mathf.Clamp(height, 0.001f, 0.45f);
            appliedCritSpikeCount = critSpikeCount;
            appliedCritSpikeWidth = critSpikeWidth;
            appliedCritSpikeTipWidth = critSpikeTipWidth;
            appliedCritSpikeHeight = critSpikeHeight;
            critSpikeWidthReferenceCount = DefaultCritSpikeCountMin;
            ApplyProperties();
        }

        public void SetColors(Color nextGlowColor, Color nextFillColor)
        {
            glowColor = nextGlowColor;
            fillColor = nextFillColor;
            ApplyProperties();
        }

        public void SetColors(Color nextGlowColor, Color nextCoreColor, Color nextFillColor)
        {
            glowColor = nextGlowColor;
            coreColor = nextCoreColor;
            fillColor = nextFillColor;
            ApplyProperties();
        }

        public void SetCoreColor(Color nextCoreColor)
        {
            coreColor = nextCoreColor;
            ApplyProperties();
        }

        public void SetMirroring(bool mirrorHorizontally, bool mirrorVertically)
        {
            mirrorX = mirrorHorizontally;
            mirrorY = mirrorVertically;
            ApplyProperties();
        }

        public void SetRotation(float degrees)
        {
            rotation = degrees;
            ApplyProperties();
        }

        public void SetTriangleSize(float size)
        {
            triangleSize = Mathf.Clamp(size, 0.45f, 1.2f);
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
                    name = "M_UnitShape_Runtime",
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

        private void ApplyProperties()
        {
            CacheComponents();

            if (meshRenderer == null)
            {
                return;
            }

            if (appliedArmorMarkWidth <= 0f || appliedArmorMarkHeight <= 0f || appliedCritSpikeWidth <= 0f || appliedCritSpikeHeight <= 0f)
            {
                ResetAppliedShapeControls();
            }

            int markCount = Mathf.RoundToInt(Mathf.Max(0f, appliedArmorMarkCount));
            int spikeCount = Mathf.RoundToInt(Mathf.Max(0f, appliedCritSpikeCount));
            float markWidthReferenceCount = Mathf.Max(1f, armorMarkWidthReferenceCount);
            float spikeWidthReferenceCount = Mathf.Max(1f, critSpikeWidthReferenceCount);

            propertyBlock.Clear();
            propertyBlock.SetFloat(ShapeTypeId, (float)shapeType);
            propertyBlock.SetFloat(TriangleSizeId, triangleSize);
            propertyBlock.SetColor(ColorId, glowColor);
            propertyBlock.SetColor(CoreColorId, coreColor);
            propertyBlock.SetColor(FillColorId, fillColor);
            propertyBlock.SetFloat(CenterDotRadiusId, GetCenterDotRadius());
            if (hasCenterGlowRadiusOverride)
            {
                propertyBlock.SetFloat(CenterGlowRadiusId, centerGlowRadius);
            }

            propertyBlock.SetFloat(ArmorId, armor);
            propertyBlock.SetFloat(CritId, crit);
            propertyBlock.SetFloat(MarkCountId, markCount);
            propertyBlock.SetFloat(MarkWidthReferenceCountId, markWidthReferenceCount);
            propertyBlock.SetFloat(MarkWidthId, appliedArmorMarkWidth);
            propertyBlock.SetFloat(MarkLengthId, appliedArmorMarkHeight);
            propertyBlock.SetFloat(SpikeCountId, spikeCount);
            propertyBlock.SetFloat(SpikeWidthReferenceCountId, spikeWidthReferenceCount);
            propertyBlock.SetFloat(SpikeWidthId, appliedCritSpikeWidth);
            propertyBlock.SetFloat(SpikeTipWidthId, appliedCritSpikeTipWidth);
            propertyBlock.SetFloat(SpikeLengthId, appliedCritSpikeHeight);
            propertyBlock.SetFloat(RotationId, rotation);
            propertyBlock.SetFloat(MirrorXId, mirrorX ? 1f : 0f);
            propertyBlock.SetFloat(MirrorYId, mirrorY ? 1f : 0f);
            propertyBlock.SetFloat(PulseAmountId, pulseAmount);
            propertyBlock.SetFloat(PulseSpeedId, pulseSpeed);
            propertyBlock.SetFloat(ContourSpinSpeedId, contourSpinSpeed);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        private float GetCenterDotRadius()
        {
            return centerDotRadius;
        }

        private void ResetAppliedShapeControls()
        {
            appliedArmorMarkCount = armorMarkCount;
            appliedArmorMarkWidth = armorMarkWidth;
            appliedArmorMarkHeight = armorMarkHeight;
            appliedCritSpikeCount = critSpikeCount;
            appliedCritSpikeWidth = critSpikeWidth;
            appliedCritSpikeTipWidth = critSpikeTipWidth;
            appliedCritSpikeHeight = critSpikeHeight;
            hasCenterGlowRadiusOverride = false;
        }

        private void SetMaximumHealthWithoutApplying(float nextMaximumHealth)
        {
            float normalizedHealth = Mathf.InverseLerp(
                DefaultMaximumHealthMin,
                DefaultMaximumHealthMax,
                Mathf.Max(0f, nextMaximumHealth));
            centerDotRadius = Mathf.Lerp(DefaultCenterDotRadiusMin, DefaultCenterDotRadiusMax, normalizedHealth);
        }

        private void ResolveUnit()
        {
            if (unit == null)
            {
                unit = GetComponentInParent<Unit>();
            }
        }

        private void SubscribeToUnit()
        {
            Unit nextUnit = unit != null ? unit : GetComponentInParent<Unit>();
            if (subscribedUnit == nextUnit)
            {
                return;
            }

            UnsubscribeFromUnit();
            subscribedUnit = nextUnit;
            if (subscribedUnit != null)
            {
                subscribedUnit.OnStatsRecalculated += HandleUnitStatsRecalculated;
            }
        }

        private void UnsubscribeFromUnit()
        {
            if (subscribedUnit == null)
            {
                return;
            }

            subscribedUnit.OnStatsRecalculated -= HandleUnitStatsRecalculated;
            subscribedUnit = null;
        }

        private void HandleUnitStatsRecalculated()
        {
            ResetAppliedShapeControls();
            ApplyStatVisualBindingsFromUnit();
            ApplyProperties();
            RefreshWisps();
        }

        private void ApplyStatVisualBindingsFromUnit()
        {
            if (!Application.isPlaying && previewBindingsInEditMode)
            {
                ApplyPreviewStatVisualBindings();
                return;
            }

            if (unit == null || unit.BaseUnitModifiers == null || statVisualBindings == null)
            {
                return;
            }

            for (int i = 0; i < statVisualBindings.Count; i++)
            {
                UnitStatVisualBinding binding = statVisualBindings[i];
                if (binding == null)
                {
                    continue;
                }

                float statValue = unit.BaseUnitModifiers.GetStatValue(binding.StatType);
                binding.Apply(statValue, ApplyVisualProperty);
            }
        }

        private void EnsureStatVisualBindings()
        {
            if (statVisualBindingsVersion != CurrentStatVisualBindingsVersion || statVisualBindings == null || statVisualBindings.Count == 0)
            {
                statVisualBindings = CreateDefaultStatVisualBindings();
                statVisualBindingsVersion = CurrentStatVisualBindingsVersion;
            }
        }

        private void ApplyPreviewStatVisualBindings()
        {
            if (statVisualBindings == null)
            {
                return;
            }

            for (int i = 0; i < statVisualBindings.Count; i++)
            {
                UnitStatVisualBinding binding = statVisualBindings[i];
                if (binding == null)
                {
                    continue;
                }

                binding.ApplyPreview(ApplyVisualProperty);
            }
        }

        private void ApplyVisualProperty(UnitShapeVisualProperty property, float value)
        {
            switch (property)
            {
                case UnitShapeVisualProperty.ArmorIntensity:
                    armor = Mathf.Clamp01(value);
                    break;
                case UnitShapeVisualProperty.CritIntensity:
                    crit = Mathf.Clamp01(value);
                    break;
                case UnitShapeVisualProperty.ArmorMarkCount:
                    appliedArmorMarkCount = Mathf.Max(0f, value);
                    break;
                case UnitShapeVisualProperty.ArmorMarkWidthReferenceCount:
                    armorMarkWidthReferenceCount = Mathf.Max(1f, value);
                    break;
                case UnitShapeVisualProperty.ArmorMarkWidth:
                    appliedArmorMarkWidth = Mathf.Clamp(value, 0.001f, 0.25f);
                    break;
                case UnitShapeVisualProperty.ArmorMarkHeight:
                    appliedArmorMarkHeight = Mathf.Clamp(value, 0.001f, 0.2f);
                    break;
                case UnitShapeVisualProperty.CritSpikeCount:
                    appliedCritSpikeCount = Mathf.Max(0f, value);
                    break;
                case UnitShapeVisualProperty.CritSpikeWidthReferenceCount:
                    critSpikeWidthReferenceCount = Mathf.Max(1f, value);
                    break;
                case UnitShapeVisualProperty.CritSpikeWidth:
                    appliedCritSpikeWidth = value;
                    break;
                case UnitShapeVisualProperty.CritSpikeTipWidth:
                    appliedCritSpikeTipWidth = value;
                    break;
                case UnitShapeVisualProperty.CritSpikeHeight:
                    appliedCritSpikeHeight = value;
                    break;
                case UnitShapeVisualProperty.CenterDotRadius:
                    centerDotRadius = Mathf.Clamp(value, 0f, MaxCenterDotRadius);
                    break;
                case UnitShapeVisualProperty.CenterGlowRadius:
                    centerGlowRadius = value;
                    hasCenterGlowRadiusOverride = true;
                    break;
                case UnitShapeVisualProperty.TriangleSize:
                    triangleSize = value;
                    break;
                case UnitShapeVisualProperty.PulseAmount:
                    pulseAmount = Mathf.Clamp(value, 0f, 0.5f);
                    break;
                case UnitShapeVisualProperty.PulseSpeed:
                    pulseSpeed = Mathf.Clamp(value, 0f, 8f);
                    break;
                case UnitShapeVisualProperty.ContourSpinSpeed:
                    contourSpinSpeed = value;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(property), property, null);
            }
        }

        private static List<UnitStatVisualBinding> CreateDefaultStatVisualBindings()
        {
            return new List<UnitStatVisualBinding>
            {
                UnitStatVisualBinding.Create(
                    StatType.Armor,
                    0f,
                    4000f,
                    new UnitStatVisualOutput(UnitShapeVisualProperty.ArmorMarkCount, DefaultArmorMarkCountMin, DefaultArmorMarkCountMax),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.ArmorMarkWidthReferenceCount, DefaultArmorMarkCountMin, DefaultArmorMarkCountMin),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.ArmorMarkWidth, DefaultArmorMarkWidth, DefaultArmorMarkWidth),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.ArmorMarkHeight, DefaultArmorMarkHeight, DefaultArmorMarkHeight),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.ArmorIntensity, 0f, 1f)),
                UnitStatVisualBinding.Create(
                    StatType.CritChance,
                    0f,
                    1f,
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritSpikeCount, DefaultCritSpikeCountMin, DefaultCritSpikeCountMax),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritSpikeWidthReferenceCount, DefaultCritSpikeCountMin, DefaultCritSpikeCountMin),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritSpikeWidth, DefaultCritSpikeWidth, DefaultCritSpikeWidth),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritSpikeTipWidth, DefaultCritSpikeTipWidth, DefaultCritSpikeTipWidth),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritSpikeHeight, DefaultCritSpikeHeight, DefaultCritSpikeHeight),
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CritIntensity, 0f, 1f)),
                UnitStatVisualBinding.Create(
                    StatType.MaximumHealth,
                    DefaultMaximumHealthMin,
                    DefaultMaximumHealthMax,
                    new UnitStatVisualOutput(UnitShapeVisualProperty.CenterDotRadius, DefaultCenterDotRadiusMin, DefaultCenterDotRadiusMax))
            };
        }
    }

    public enum UnitShapeType
    {
        Circle = 0,
        Square = 1,
        Triangle = 2
    }

    public enum UnitShapeVisualProperty
    {
        ArmorIntensity = 0,
        CritIntensity = 1,
        ArmorMarkCount = 2,
        ArmorMarkWidthReferenceCount = 3,
        CritSpikeCount = 4,
        CritSpikeWidthReferenceCount = 5,
        CenterDotRadius = 6,
        TriangleSize = 7,
        PulseAmount = 8,
        PulseSpeed = 9,
        ContourSpinSpeed = 10,
        ArmorMarkWidth = 11,
        ArmorMarkHeight = 12,
        CritSpikeWidth = 13,
        CritSpikeTipWidth = 14,
        CritSpikeHeight = 15,
        CenterGlowRadius = 16
    }

    [Serializable]
    public sealed class UnitStatVisualBinding
    {
        [SerializeField] private StatType statType;
        [SerializeField] private float statMin;
        [SerializeField] private float statMax = 1f;
        [SerializeField] private float previewStatValue;
        [SerializeField] private UnitStatVisualDistribution distribution;
        [SerializeField, Range(0.05f, 8f)] private float distributionPower = 2f;
        [SerializeField] private List<UnitStatVisualOutput> outputs = new List<UnitStatVisualOutput>();

        public StatType StatType => statType;

        public void Apply(float statValue, Action<UnitShapeVisualProperty, float> applyOutput)
        {
            if (applyOutput == null || outputs == null)
            {
                return;
            }

            float normalizedValue = Mathf.Approximately(statMin, statMax)
                ? 1f
                : Mathf.InverseLerp(statMin, statMax, statValue);
            normalizedValue = ApplyDistribution(normalizedValue);

            for (int i = 0; i < outputs.Count; i++)
            {
                UnitStatVisualOutput output = outputs[i];
                if (output == null)
                {
                    continue;
                }

                output.Apply(normalizedValue, applyOutput);
            }
        }

        private float ApplyDistribution(float normalizedValue)
        {
            float t = Mathf.Clamp01(normalizedValue);
            float power = distributionPower > 0f
                ? Mathf.Clamp(distributionPower, 0.05f, 8f)
                : 2f;

            switch (distribution)
            {
                case UnitStatVisualDistribution.Linear:
                    return t;
                case UnitStatVisualDistribution.EaseIn:
                    return Mathf.Pow(t, power);
                case UnitStatVisualDistribution.EaseOut:
                    return 1f - Mathf.Pow(1f - t, power);
                case UnitStatVisualDistribution.EaseInOut:
                    return t < 0.5f
                        ? 0.5f * Mathf.Pow(t * 2f, power)
                        : 1f - 0.5f * Mathf.Pow((1f - t) * 2f, power);
                case UnitStatVisualDistribution.SmoothStep:
                    return t * t * (3f - 2f * t);
                default:
                    throw new ArgumentOutOfRangeException(nameof(distribution), distribution, null);
            }
        }

        public void ApplyPreview(Action<UnitShapeVisualProperty, float> applyOutput)
        {
            Apply(previewStatValue, applyOutput);
        }

        public static UnitStatVisualBinding Create(
            StatType statType,
            float statMin,
            float statMax,
            params UnitStatVisualOutput[] outputs)
        {
            UnitStatVisualBinding binding = new UnitStatVisualBinding
            {
                statType = statType,
                statMin = statMin,
                statMax = statMax,
                previewStatValue = statMin,
                distribution = UnitStatVisualDistribution.Linear,
                distributionPower = 2f
            };

            if (outputs != null)
            {
                binding.outputs.AddRange(outputs);
            }

            return binding;
        }
    }

    public enum UnitStatVisualDistribution
    {
        Linear = 0,
        EaseIn = 1,
        EaseOut = 2,
        EaseInOut = 3,
        SmoothStep = 4
    }

    [Serializable]
    public sealed class UnitStatVisualOutput
    {
        [SerializeField] private UnitShapeVisualProperty property;
        [SerializeField] private float valueAtStatMin;
        [SerializeField] private float valueAtStatMax = 1f;

        public UnitStatVisualOutput()
        {
        }

        public UnitStatVisualOutput(UnitShapeVisualProperty property, float valueAtStatMin, float valueAtStatMax)
        {
            this.property = property;
            this.valueAtStatMin = valueAtStatMin;
            this.valueAtStatMax = valueAtStatMax;
        }

        public void Apply(float normalizedStatValue, Action<UnitShapeVisualProperty, float> applyOutput)
        {
            float value = Mathf.Lerp(valueAtStatMin, valueAtStatMax, Mathf.Clamp01(normalizedStatValue));
            applyOutput(property, value);
        }
    }
}
