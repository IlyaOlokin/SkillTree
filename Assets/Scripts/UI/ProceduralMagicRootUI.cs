using UnityEngine;
using UnityEngine.UI;

public enum MagicRootStyle { Light, Darkness, Custom }

/// <summary>Deterministic, texture-free root for a rectangular uGUI Graphic.</summary>
[ExecuteAlways, DisallowMultipleComponent]
[AddComponentMenu("UI/Procedural Magic Root UI")]
public sealed class ProceduralMagicRootUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Graphic targetGraphic;
    [SerializeField] private MysticColorsConfig mysticColorsConfig;
    // Serialized scene/prefab reference keeps the shader in player builds.
    [SerializeField, HideInInspector] private Shader rootShader;

    [Header("Style")]
    public MagicRootStyle Style = MagicRootStyle.Light;
    [ColorUsage(true, true)] public Color Color = new Color(1f, 215f / 255f, 158f / 255f);
    [Min(0f)] public float Intensity = 1.5f;
    [Range(0f, 1f)] public float Alpha = 1f;
    public float Seed = 17f;
    [Range(0f, 1f)] public float StyleBlend;
    [Range(0f, 3f)] public float LeftIntensityBoost = 0.2f;

    [Header("Glow")]
    [Tooltip("HDR color of the bright inner core. Not overridden by Mystic Colors Config.")]
    [ColorUsage(true, true)] public Color GlowColor = UnityEngine.Color.white;
    [Min(0f)] public float GlowIntensity = 0.5f;
    [Tooltip("Fraction of each branch occupied by its brightest center.")]
    [Range(0.05f, 0.8f)] public float CoreWidth = 0.2f;
    [Tooltip("Higher values make a narrower, more contrasted center-to-edge gradient.")]
    [Range(0.5f, 4f)] public float CoreSharpness = 1.5f;
    [Tooltip("Brightness of the colored rim relative to the base color.")]
    [Range(0f, 1f)] public float EdgeBrightness = 0.5f;
    [Tooltip("How quickly core emission fades toward the current fill endpoint. Zero keeps it constant along the bar.")]
    [Range(0f, 4f)] public float GlowFalloff = 1.2f;


    [Header("Fill")]
    [Range(0f, 1f)] public float FillAmount = 1f;
    [Range(0f, 0.1f)] public float FillSoftness = 0.002f;

    [Header("Main Root")]
    [Tooltip("Root radius as a fraction of bar height.")]
    [Range(0.001f, 0.2f)] public float Thickness = 0.055f;
    [Range(0f, 0.05f)] public float EndThickness = 0.001f;
    [Range(0.1f, 2f)] public float VerticalScale = 1f;
    [Range(0f, 0.25f)] public float MainPathAmplitude = 0.09f;
    [Range(0.1f, 12f)] public float MainPathFrequency = 6f;
    [Range(-10f, 10f)] public float MainPathOffset = 0f;
    [Range(0f, 1f)] public float MainPathSharpness = 0.1f;

    [Header("Branches")]
    [Range(8, 96)] public int BranchCount = 64;
    [Range(0f, 1f)] public float BranchDensity = 0.8f;
    [Range(0f, 2f)] public float BranchLength = 0.85f;
    [Range(0f, 1f)] public float BranchWidth = 0.6f;
    [Range(5f, 80f)] public float BranchAngle = 28f;
    [Range(0f, 1f)] public float BranchAngleRandomness = 0.35f;
    [Range(0f, 1f)] public float BranchLengthRandomness = 0.45f;
    [Range(0f, 1f)] public float SecondaryBranchDensity = 0.2f;
    [Range(0f, 1f)] public float SecondaryBranchLength = 0.4f;
    [Range(2, 6)] public int MaxBranchSegments = 6;

    [Header("Shape")]
    [Range(0f, 1f)] public float Smoothness = 0.65f;
    [Range(0f, 0.15f)] public float Jaggedness = 0.015f;
    [Range(1f, 48f)] public float JaggedFrequency = 20f;
    [Range(0f, 1f)] public float SpikeAmount = 0f;
    [Range(0f, 0.3f)] public float Curvature = 0.16f;
    [Range(0.5f, 4f)] public float TipSharpness = 1.5f;
    [Range(0.1f, 4f)] public float TaperPower = 1.2f;

    [Header("Falloff")]
    [Range(0.1f, 4f)] public float ThicknessFalloff = 1f;
    [Range(0f, 3f)] public float LengthFalloff = 0.8f;
    [Range(0f, 3f)] public float DensityFalloff = 0.65f;

    [SerializeField, HideInInspector] private MagicRootStyle appliedStyle;
    private Graphic boundGraphic;
    private Material materialInstance;
    private Material previousMaterial;
    private bool reportedError;
    private Graphic sampledFillGraphic;
    private bool hasFillSample;
    private bool wasFillEmpty;

    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int CoreWidthId = Shader.PropertyToID("_CoreWidth");
    private static readonly int CoreSharpnessId = Shader.PropertyToID("_CoreSharpness");
    private static readonly int EdgeBrightnessId = Shader.PropertyToID("_EdgeBrightness");
    private static readonly int GlowFalloffId = Shader.PropertyToID("_GlowFalloff");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int SeedId = Shader.PropertyToID("_Seed");
    private static readonly int StyleBlendId = Shader.PropertyToID("_StyleBlend");
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FillSoftnessId = Shader.PropertyToID("_FillSoftness");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
    private static readonly int EndThicknessId = Shader.PropertyToID("_EndThickness");
    private static readonly int VerticalScaleId = Shader.PropertyToID("_VerticalScale");
    private static readonly int MainPathAmplitudeId = Shader.PropertyToID("_MainPathAmplitude");
    private static readonly int MainPathFrequencyId = Shader.PropertyToID("_MainPathFrequency");
    private static readonly int MainPathOffsetId = Shader.PropertyToID("_MainPathOffset");
    private static readonly int MainPathSharpnessId = Shader.PropertyToID("_MainPathSharpness");
    private static readonly int BranchCountId = Shader.PropertyToID("_BranchCount");
    private static readonly int BranchDensityId = Shader.PropertyToID("_BranchDensity");
    private static readonly int BranchLengthId = Shader.PropertyToID("_BranchLength");
    private static readonly int BranchWidthId = Shader.PropertyToID("_BranchWidth");
    private static readonly int BranchAngleId = Shader.PropertyToID("_BranchAngle");
    private static readonly int BranchAngleRandomnessId = Shader.PropertyToID("_BranchAngleRandomness");
    private static readonly int BranchLengthRandomnessId = Shader.PropertyToID("_BranchLengthRandomness");
    private static readonly int SecondaryBranchDensityId = Shader.PropertyToID("_SecondaryBranchDensity");
    private static readonly int SecondaryBranchLengthId = Shader.PropertyToID("_SecondaryBranchLength");
    private static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
    private static readonly int JaggednessId = Shader.PropertyToID("_Jaggedness");
    private static readonly int JaggedFrequencyId = Shader.PropertyToID("_JaggedFrequency");
    private static readonly int SpikeAmountId = Shader.PropertyToID("_SpikeAmount");
    private static readonly int CurvatureId = Shader.PropertyToID("_Curvature");
    private static readonly int TipSharpnessId = Shader.PropertyToID("_TipSharpness");
    private static readonly int TaperPowerId = Shader.PropertyToID("_TaperPower");
    private static readonly int ThicknessFalloffId = Shader.PropertyToID("_ThicknessFalloff");
    private static readonly int LengthFalloffId = Shader.PropertyToID("_LengthFalloff");
    private static readonly int DensityFalloffId = Shader.PropertyToID("_DensityFalloff");
    private static readonly int LeftIntensityBoostId = Shader.PropertyToID("_LeftIntensityBoost");
    private static readonly int MaxBranchSegmentsId = Shader.PropertyToID("_MaxBranchSegments");
    private static readonly int RootRectId = Shader.PropertyToID("_RootRect");
    private static readonly int RootWorldToLocalId = Shader.PropertyToID("_RootWorldToLocal");
    private static readonly int AspectRatioId = Shader.PropertyToID("_AspectRatio");

    private void Reset()
    {
        targetGraphic = GetComponent<Graphic>();
        rootShader = Shader.Find("UI/Procedural Magic Root");
        ApplyLightPreset();
    }

    private void OnEnable()
    {
        Canvas.willRenderCanvases += ApplyToMaterial;
        ApplyToMaterial();
    }

    // Inspector edits are applied on the main thread, including in edit mode.
    private void Update() => ApplyToMaterial();
    private void OnDisable()
    {
        Canvas.willRenderCanvases -= ApplyToMaterial;
        hasFillSample = false;
        ReleaseMaterial();
    }
    private void OnDestroy() => ReleaseMaterial();

    private void ReleaseMaterial()
    {
        if (boundGraphic != null && boundGraphic.material == materialInstance)
            boundGraphic.material = previousMaterial;
        boundGraphic = null;
        previousMaterial = null;
        if (materialInstance == null) return;
        if (Application.isPlaying) Destroy(materialInstance);
        else DestroyImmediate(materialInstance);
        materialInstance = null;
    }

    public void ApplyToMaterial()
    {
        if (!isActiveAndEnabled) return;
        UpdatePreset();
        if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        if (boundGraphic != targetGraphic) ReleaseMaterial();
        if (targetGraphic == null)
        {
            ReportError("Add this component to a RawImage or Image, or assign Target Graphic.");
            return;
        }
        if (rootShader == null) rootShader = Shader.Find("UI/Procedural Magic Root");
        if (rootShader == null || !rootShader.isSupported)
        {
            ReportError("UI/Procedural Magic Root shader is missing or unsupported. Include it in the build.");
            return;
        }
        reportedError = false;
        if (materialInstance == null)
        {
            boundGraphic = targetGraphic;
            previousMaterial = targetGraphic.material;
            materialInstance = new Material(rootShader)
            {
                name = "Magic Root UI (Instance)",
                hideFlags = HideFlags.HideAndDontSave
            };
            targetGraphic.material = materialInstance;
        }
        UpdateSeedForFill();
        WriteProperties(materialInstance);
        // MaskableGraphic caches a stencil material. Preserve its stencil state.
        Material rendered = targetGraphic.materialForRendering;
        if (rendered != null && rendered != materialInstance && rendered.shader == rootShader)
            WriteProperties(rendered);
    }

    private void ReportError(string message)
    {
        if (!reportedError) Debug.LogError("ProceduralMagicRootUI: " + message, this);
        reportedError = true;
    }

    private void WriteProperties(Material material)
    {
        Color tint = Color;
        if (mysticColorsConfig != null)
        {
            if (Style == MagicRootStyle.Light) tint = mysticColorsConfig.LightColor;
            else if (Style == MagicRootStyle.Darkness) tint = mysticColorsConfig.DarknessColor;
        }
        Rect rect = GetDrawingRect();
        material.SetMatrix(RootWorldToLocalId, targetGraphic.rectTransform.worldToLocalMatrix);
        material.SetVector(RootRectId, new Vector4(rect.xMin, rect.yMin, rect.width, rect.height));
        material.SetFloat(AspectRatioId, Mathf.Max(0.01f, rect.width / Mathf.Max(0.001f, rect.height)));
        material.SetColor(ColorId, tint);
        material.SetColor(GlowColorId, GlowColor);
        material.SetFloat(CoreWidthId, Mathf.Clamp(CoreWidth, 0.05f, 0.8f));
        material.SetFloat(CoreSharpnessId, Mathf.Clamp(CoreSharpness, 0.5f, 4f));
        material.SetFloat(EdgeBrightnessId, Mathf.Clamp(EdgeBrightness, 0f, 1f));
        material.SetFloat(GlowFalloffId, Mathf.Clamp(GlowFalloff, 0f, 4f));

        material.SetFloat(GlowIntensityId, Mathf.Max(0f, GlowIntensity));
        material.SetFloat(IntensityId, Intensity);
        material.SetFloat(AlphaId, Mathf.Clamp01(Alpha));
        material.SetFloat(SeedId, Mathf.Repeat(Seed, 65536f));
        material.SetFloat(StyleBlendId, Mathf.Clamp01(StyleBlend));
        material.SetFloat(FillAmountId, EffectiveFillAmount);
        material.SetFloat(FillSoftnessId, FillSoftness);
        material.SetFloat(ThicknessId, Thickness);
        material.SetFloat(EndThicknessId, EndThickness);
        material.SetFloat(VerticalScaleId, VerticalScale);
        material.SetFloat(MainPathAmplitudeId, MainPathAmplitude);
        material.SetFloat(MainPathFrequencyId, MainPathFrequency);
        material.SetFloat(MainPathOffsetId, MainPathOffset);
        material.SetFloat(MainPathSharpnessId, MainPathSharpness);
        material.SetFloat(BranchCountId, Mathf.Clamp(BranchCount, 8, 96));
        material.SetFloat(BranchDensityId, BranchDensity);
        material.SetFloat(BranchLengthId, BranchLength);
        material.SetFloat(BranchWidthId, BranchWidth);
        material.SetFloat(BranchAngleId, BranchAngle);
        material.SetFloat(BranchAngleRandomnessId, BranchAngleRandomness);
        material.SetFloat(BranchLengthRandomnessId, BranchLengthRandomness);
        material.SetFloat(SecondaryBranchDensityId, SecondaryBranchDensity);
        material.SetFloat(SecondaryBranchLengthId, SecondaryBranchLength);
        material.SetFloat(SmoothnessId, Smoothness);
        material.SetFloat(JaggednessId, Jaggedness);
        material.SetFloat(JaggedFrequencyId, JaggedFrequency);
        material.SetFloat(SpikeAmountId, SpikeAmount);
        material.SetFloat(CurvatureId, Curvature);
        material.SetFloat(TipSharpnessId, Mathf.Max(0.05f, TipSharpness));
        material.SetFloat(TaperPowerId, TaperPower);
        material.SetFloat(ThicknessFalloffId, ThicknessFalloff);
        material.SetFloat(LengthFalloffId, LengthFalloff);
        material.SetFloat(DensityFalloffId, DensityFalloff);
        material.SetFloat(LeftIntensityBoostId, LeftIntensityBoost);
        material.SetFloat(MaxBranchSegmentsId, MaxBranchSegments);
    }

    [ContextMenu("Apply Light Preset")]
    public void ApplyLightPreset() { SetPreset(false); ApplyToMaterial(); }
    [ContextMenu("Apply Darkness Preset")]
    public void ApplyDarknessPreset() { SetPreset(true); ApplyToMaterial(); }
    [ContextMenu("Randomize Seed")]
    public void RandomizeSeed()
    {
        RollSeed();
        ApplyToMaterial();
    }

    private void UpdateSeedForFill()
    {
        bool empty = EffectiveFillAmount <= 0f;
        // First observation (including re-enable/rebind) establishes a baseline.
        // Both Update and Canvas callbacks run this; remember the edge before rendering.
        if (hasFillSample && sampledFillGraphic == targetGraphic && wasFillEmpty && !empty)
            RollSeed();
        sampledFillGraphic = targetGraphic;
        wasFillEmpty = empty;
        hasFillSample = true;
    }

    private void RollSeed()
    {
        // Exclude the current seed, without consuming Unity's gameplay random state.
        int previous = Mathf.FloorToInt(Mathf.Repeat(Seed, 65536f));
        int next = new System.Random(System.Guid.NewGuid().GetHashCode()).Next(0, 65535);
        Seed = next >= previous ? next + 1 : next;
    }

    public float EffectiveFillAmount
    {
        get
        {
            float fill = Mathf.Clamp01(FillAmount);
            // Read the existing health Image without changing its mesh or gameplay values.
            if (targetGraphic is Image image && image.type == Image.Type.Filled &&
                image.fillMethod == Image.FillMethod.Horizontal && image.fillOrigin == 0)
                fill = Mathf.Min(fill, image.fillAmount);
            return fill;
        }
    }

    private Rect GetDrawingRect()
    {
        Rect rect = targetGraphic.GetPixelAdjustedRect();
        if (!(targetGraphic is Image image) ||
            (image.type != Image.Type.Simple && image.type != Image.Type.Filled)) return rect;
        Sprite sprite = image.overrideSprite != null ? image.overrideSprite : image.sprite;
        if (sprite == null) return rect;
        Vector2 size = sprite.rect.size;
        if (image.preserveAspect && size.x > 0 && size.y > 0 && rect.height > 0)
        {
            float aspect = size.x / size.y;
            Vector2 pivot = image.rectTransform.pivot;
            if (aspect > rect.width / rect.height)
            {
                float height = rect.width / aspect;
                rect.y += (rect.height - height) * pivot.y;
                rect.height = height;
            }
            else
            {
                float width = rect.height * aspect;
                rect.x += (rect.width - width) * pivot.x;
                rect.width = width;
            }
        }
        // Match Image's padded drawing rectangle so its mesh cutoff and our tip coincide.
        Vector4 padding = UnityEngine.Sprites.DataUtility.GetPadding(sprite);
        float w = Mathf.Max(1, Mathf.RoundToInt(size.x)), h = Mathf.Max(1, Mathf.RoundToInt(size.y));
        return Rect.MinMaxRect(rect.xMin + rect.width * padding.x / w,
            rect.yMin + rect.height * padding.y / h,
            rect.xMax - rect.width * padding.z / w, rect.yMax - rect.height * padding.w / h);
    }

    [SerializeField, HideInInspector] private Preset lightPreset;
    [SerializeField, HideInInspector] private Preset darknessPreset;
    [SerializeField, HideInInspector] private bool presetsInitialized;

    private void SetPreset(bool dark)
    {
        Style = dark ? MagicRootStyle.Darkness : MagicRootStyle.Light;
        UpdatePreset();
    }

    private void UpdatePreset()
    {
        if (!presetsInitialized || lightPreset == null || darknessPreset == null)
        {
            lightPreset = new Preset(false);
            darknessPreset = new Preset(true);
            // Migrate existing components without discarding their Inspector tuning.
            if (appliedStyle == MagicRootStyle.Light) lightPreset.Capture(this);
            if (appliedStyle == MagicRootStyle.Darkness) darknessPreset.Capture(this);
            presetsInitialized = true;
        }
        if (appliedStyle == MagicRootStyle.Light) lightPreset.Capture(this);
        if (appliedStyle == MagicRootStyle.Darkness) darknessPreset.Capture(this);
        if (Style == appliedStyle) return;
        if (Style == MagicRootStyle.Light) lightPreset.Apply(this);
        if (Style == MagicRootStyle.Darkness) darknessPreset.Apply(this);
        // Custom starts from the visible values, and never changes either stored preset.
        appliedStyle = Style;
    }

    [System.Serializable]
    private sealed class Preset
    {
        public Color Color;
        public Color GlowColor = UnityEngine.Color.white;
        public float GlowIntensity = 0.5f;
        public float CoreWidth = 0.2f;
        public float CoreSharpness = 1.5f;
        public float EdgeBrightness = 0.5f;
        public float GlowFalloff = 1.2f;

        public float Intensity;
        public float Alpha;
        public float StyleBlend;
        public float LeftIntensityBoost;
        public float Thickness;
        public float EndThickness;
        public float VerticalScale;
        public float MainPathAmplitude;
        public float MainPathFrequency;
        public float MainPathOffset;
        public float MainPathSharpness;
        public int BranchCount;
        public float BranchDensity;
        public float BranchLength;
        public float BranchWidth;
        public float BranchAngle;
        public float BranchAngleRandomness;
        public float BranchLengthRandomness;
        public float SecondaryBranchDensity;
        public float SecondaryBranchLength;
        public int MaxBranchSegments;
        public float Smoothness;
        public float Jaggedness;
        public float JaggedFrequency;
        public float SpikeAmount;
        public float Curvature;
        public float TipSharpness;
        public float TaperPower;
        public float ThicknessFalloff;
        public float LengthFalloff;
        public float DensityFalloff;

        public Preset(bool dark)
        {
            Color = dark ? new Color(108f / 255f, 59f / 255f, 1f) : new Color(1f, 215f / 255f, 158f / 255f);
            Intensity = 1.5f;
            Alpha = 1f;
            Thickness = dark ? 0.06f : 0.055f;
            EndThickness = 0.001f;
            VerticalScale = 1f;
            MainPathAmplitude = 0.09f;
            MainPathFrequency = dark ? 8f : 6f;
            MainPathOffset = 0f;
            MainPathSharpness = dark ? 0.9f : 0.1f;
            BranchDensity = dark ? 1f : 0.8f;
            BranchLength = dark ? 0.95f : 0.85f;
            BranchWidth = dark ? 0.7f : 0.6f;
            BranchAngle = dark ? 37f : 28f;
            BranchAngleRandomness = 0.35f;
            BranchLengthRandomness = 0.45f;
            SecondaryBranchDensity = dark ? 0.65f : 0.2f;
            SecondaryBranchLength = 0.4f;
            Smoothness = dark ? 0.12f : 0.65f;
            Jaggedness = dark ? 0.035f : 0.015f;
            JaggedFrequency = 20f;
            SpikeAmount = dark ? 0.85f : 0f;
            Curvature = dark ? 0.06f : 0.16f;
            TipSharpness = dark ? 2.2f : 1.5f;
            TaperPower = 1.2f;
            ThicknessFalloff = 1f;
            LengthFalloff = 0.8f;
            DensityFalloff = dark ? 0.45f : 0.65f;
            LeftIntensityBoost = 0.2f;
            MaxBranchSegments = 6;
            StyleBlend = dark ? 1f : 0f;
            BranchCount = 64;
        }

        public void Capture(ProceduralMagicRootUI root)
        {
            Color = root.Color;
            GlowColor = root.GlowColor;
            GlowIntensity = root.GlowIntensity;
            CoreWidth = root.CoreWidth;
            CoreSharpness = root.CoreSharpness;
            EdgeBrightness = root.EdgeBrightness;
            GlowFalloff = root.GlowFalloff;

            Intensity = root.Intensity;
            Alpha = root.Alpha;
            StyleBlend = root.StyleBlend;
            LeftIntensityBoost = root.LeftIntensityBoost;
            Thickness = root.Thickness;
            EndThickness = root.EndThickness;
            VerticalScale = root.VerticalScale;
            MainPathAmplitude = root.MainPathAmplitude;
            MainPathFrequency = root.MainPathFrequency;
            MainPathOffset = root.MainPathOffset;
            MainPathSharpness = root.MainPathSharpness;
            BranchCount = root.BranchCount;
            BranchDensity = root.BranchDensity;
            BranchLength = root.BranchLength;
            BranchWidth = root.BranchWidth;
            BranchAngle = root.BranchAngle;
            BranchAngleRandomness = root.BranchAngleRandomness;
            BranchLengthRandomness = root.BranchLengthRandomness;
            SecondaryBranchDensity = root.SecondaryBranchDensity;
            SecondaryBranchLength = root.SecondaryBranchLength;
            MaxBranchSegments = root.MaxBranchSegments;
            Smoothness = root.Smoothness;
            Jaggedness = root.Jaggedness;
            JaggedFrequency = root.JaggedFrequency;
            SpikeAmount = root.SpikeAmount;
            Curvature = root.Curvature;
            TipSharpness = root.TipSharpness;
            TaperPower = root.TaperPower;
            ThicknessFalloff = root.ThicknessFalloff;
            LengthFalloff = root.LengthFalloff;
            DensityFalloff = root.DensityFalloff;
        }

        public void Apply(ProceduralMagicRootUI root)
        {
            root.Color = Color;
            root.GlowColor = GlowColor;
            root.GlowIntensity = GlowIntensity;
            root.CoreWidth = CoreWidth;
            root.CoreSharpness = CoreSharpness;
            root.EdgeBrightness = EdgeBrightness;
            root.GlowFalloff = GlowFalloff;

            root.Intensity = Intensity;
            root.Alpha = Alpha;
            root.StyleBlend = StyleBlend;
            root.LeftIntensityBoost = LeftIntensityBoost;
            root.Thickness = Thickness;
            root.EndThickness = EndThickness;
            root.VerticalScale = VerticalScale;
            root.MainPathAmplitude = MainPathAmplitude;
            root.MainPathFrequency = MainPathFrequency;
            root.MainPathOffset = MainPathOffset;
            root.MainPathSharpness = MainPathSharpness;
            root.BranchCount = BranchCount;
            root.BranchDensity = BranchDensity;
            root.BranchLength = BranchLength;
            root.BranchWidth = BranchWidth;
            root.BranchAngle = BranchAngle;
            root.BranchAngleRandomness = BranchAngleRandomness;
            root.BranchLengthRandomness = BranchLengthRandomness;
            root.SecondaryBranchDensity = SecondaryBranchDensity;
            root.SecondaryBranchLength = SecondaryBranchLength;
            root.MaxBranchSegments = MaxBranchSegments;
            root.Smoothness = Smoothness;
            root.Jaggedness = Jaggedness;
            root.JaggedFrequency = JaggedFrequency;
            root.SpikeAmount = SpikeAmount;
            root.Curvature = Curvature;
            root.TipSharpness = TipSharpness;
            root.TaperPower = TaperPower;
            root.ThicknessFalloff = ThicknessFalloff;
            root.LengthFalloff = LengthFalloff;
            root.DensityFalloff = DensityFalloff;
        }
    }
}
