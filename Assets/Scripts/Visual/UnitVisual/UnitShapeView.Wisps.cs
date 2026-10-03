using System.Collections.Generic;
using UnityEngine;

namespace Visual
{
    public sealed partial class UnitShapeView
    {
        [Header("Wisps")]
        [SerializeField] private bool showWisps = true;
        [SerializeField, Tooltip("Shared wisp material. Use SkillTree/Wisp Glow for HDR colors.")]
        private Material wispGlowMaterial;
        // Keep serialized legacy values to initialize the per-type settings on existing prefabs.
        [SerializeField, HideInInspector] private float wispOrbitRadius = 0.95f;
        [SerializeField, HideInInspector] private float wispOrbitSpeed = 55f;
        [SerializeField, HideInInspector] private float wispSize = 0.22f;
        [SerializeField, HideInInspector] private float wispFlutter = 0.06f;

        [SerializeField] private WispMotionSettings steelWisp;
        [SerializeField] private WispMotionSettings ashWisp;
        [SerializeField] private WispMotionSettings frostWisp;
        [SerializeField] private WispMotionSettings stormWisp;
        [SerializeField] private StormTrajectorySettings stormTrajectory = new StormTrajectorySettings();
        [SerializeField, HideInInspector] private bool wispMotionSettingsInitialized;
        [SerializeField, HideInInspector] private int wispStyleVersion;

        [System.Serializable]
        private sealed class WispMotionSettings
        {
            [Tooltip("Radius in local units: Value +/- Spread.")]
            public WispRange orbitRadius = new WispRange(0.95f, 0.12f);
            [Tooltip("Degrees per second. Negative values reverse the orbit.")]
            public WispRange orbitSpeed = new WispRange(55f, 12f);
            public WispRange size = new WispRange(0.22f, 0.035f);
            public WispRange flutterAmplitude = new WispRange(0.06f, 0.02f);
            [Tooltip("Flutter cycles per second.")]
            public WispRange flutterFrequency = new WispRange(0.37f, 0.08f);
            [Tooltip("Size pulsation as a fraction of the base size.")]
            public WispRange pulseAmount = new WispRange(0.12f, 0.04f);
            [Tooltip("Pulse cycles per second.")]
            public WispRange pulseFrequency = new WispRange(0.5f, 0.12f);
            [HideInInspector] public float turnInterval = 0.085f;
            [HideInInspector] public float turnJitter = 12f;
            [Header("Trail")]
            [Min(0f)] public float trailLifetime = 0.35f;
            [Min(0f)] public float trailWidth = 0.08f;
            [Min(0f)] public float trailIntensity = 0.8f;
            [Header("Painterly Color (Wisp and Trail)")]
            [Range(0f, 1f)] public float paintStrength = 1f;
            [Range(2f, 12f)] public float paintSteps = 5f;
            [Range(0.001f, 0.4f)] public float paintSize = 0.1f;
            [Range(0f, 1f)] public float paintVariation = 0.65f;
        }

        private enum StormRadiusPattern { AlternateInnerOuter, Random, Fixed }

        [System.Serializable]
        private sealed class StormTrajectorySettings
        {
            [Tooltip("Seconds per straight leg, randomly selected between X and Y.")]
            public Vector2 turnDuration = new Vector2(0.06375f, 0.10625f);
            [Tooltip("Additional angle per corner in degrees, randomly selected between X and Y.")]
            public Vector2 angleOffset = new Vector2(-12f, 12f);
            public StormRadiusPattern radiusPattern = StormRadiusPattern.AlternateInnerOuter;
            [Tooltip("Maximum inward/outward offset from Storm Wisp > Orbit Radius, in local units.")]
            public WispRange radiusExcursion = new WispRange(0.3f, 0.05f);
            [Tooltip("Fraction of Radius Excursion used for each corner (X minimum, Y maximum).")]
            public Vector2 excursionMultiplier = new Vector2(0.5f, 1f);
            [Tooltip("Radius offset in local units when Radius Pattern is Fixed.")]
            public float fixedRadiusOffset;
            [Tooltip("Progress along each leg. Linear gives constant speed and sharp turns; Ease In/Out gives pulses of speed.")]
            public AnimationCurve travelProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);
            [HideInInspector] public bool initialized;
        }

        [System.Serializable]
        private struct WispRange
        {
            public float value;
            [Min(0f)] public float spread;

            public WispRange(float value, float spread)
            {
                this.value = value;
                this.spread = spread;
            }

            public float Evaluate(float variation) => value + Mathf.Max(0f, spread) * variation;
        }

        private sealed class WispInstance
        {
            public SpriteRenderer renderer;
            public TrailRenderer trail;
            public StatType type;
            public float angle, flutterPhase, pulsePhase;
            public Vector3 stormFrom, stormTo, previousWorldPosition;
            public float stormElapsed, stormDuration;
            public bool stormOuter, positioned;
            public readonly float[] variations = new float[7];
        }

        [Header("Wisp Colors")]
        [SerializeField, Min(0f)] private float wispGlowIntensity = 3f;
        [SerializeField, ColorUsage(true, true)] private Color steelWispColor = new Color(0.45f, 1f, 0.2f);
        [SerializeField, ColorUsage(true, true)] private Color ashWispColor = new Color(1f, 0.3f, 0.04f);
        [SerializeField, ColorUsage(true, true)] private Color frostWispColor = new Color(0.25f, 0.8f, 1f);
        [SerializeField, ColorUsage(true, true)] private Color stormWispColor = new Color(0.4f, 0.35f, 1f);

        private static readonly StatType[] WispTypes =
        {
            StatType.SteelWisp, StatType.AshWisp, StatType.FrostWisp, StatType.StormWisp
        };

        private readonly List<WispInstance> wisps = new List<WispInstance>();
        // Cosmetic randomness must not change Unity's gameplay random sequence.
        private readonly System.Random wispRandom = new System.Random();
        private Texture2D wispTexture;
        private Sprite wispSprite;
        private MaterialPropertyBlock wispColorProperties;
        private static readonly int WispEmissionColorId = Shader.PropertyToID("_EmissionColor");
        private bool wispsVisible;

        private void RefreshWisps()
        {
            // Do not create transient children during prefab editing or OnValidate.
            if (!Application.isPlaying || !isActiveAndEnabled)
                return;

            wispsVisible = showWisps;
            EnsureWispMotionSettings();
            foreach (StatType type in WispTypes)
            {
                float value = showWisps && unit != null && unit.BaseUnitModifiers != null
                    ? unit.BaseUnitModifiers.GetStatValue(type) : 0f;
                int count = float.IsNaN(value) || float.IsInfinity(value)
                    ? 0 : Mathf.FloorToInt(Mathf.Max(0f, value));
                int existing = 0;
                for (int i = wisps.Count - 1; i >= 0; i--)
                {
                    WispInstance wisp = wisps[i];
                    if (wisp.type != type) continue;
                    if (existing < count)
                    {
                        existing++;
                        continue;
                    }
                    wisp.renderer.gameObject.SetActive(false);
                    Destroy(wisp.renderer.gameObject);
                    wisps.RemoveAt(i);
                }
                for (; existing < count; existing++)
                {
                    EnsureWispSprite();
                    var child = new GameObject(type.ToString(), typeof(SpriteRenderer));
                    child.hideFlags = HideFlags.DontSave;
                    child.layer = gameObject.layer;
                    child.transform.SetParent(transform, false);
                    var wisp = new WispInstance
                    {
                        renderer = child.GetComponent<SpriteRenderer>(), type = type,
                        angle = RandomWispPhase(), flutterPhase = RandomWispPhase(), pulsePhase = RandomWispPhase()
                    };
                    wisp.renderer.sprite = wispSprite;
                    wisp.renderer.sharedMaterial = wispGlowMaterial;
                    wisp.trail = child.AddComponent<TrailRenderer>();
                    wisp.trail.emitting = false;
                    wisp.trail.minVertexDistance = 0.015f;
                    wisp.trail.textureMode = LineTextureMode.Stretch;
                    wisp.trail.alignment = LineAlignment.View;
                    wisp.trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    wisp.trail.receiveShadows = false;
                    wisp.trail.numCornerVertices = type == StatType.StormWisp || type == StatType.SteelWisp ? 0 : 2;
                    wisp.trail.widthCurve = type == StatType.SteelWisp
                        ? AnimationCurve.Linear(0f, 1f, 1f, 0f)
                        : new AnimationCurve(new Keyframe(0f, 0.7f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
                    var gradient = new Gradient();
                    gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                        new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                    wisp.trail.colorGradient = gradient;
                    for (int i = 0; i < wisp.variations.Length; i++)
                        wisp.variations[i] = (float)wispRandom.NextDouble() * 2f - 1f;
                    wisps.Add(wisp);
                }
            }
            UpdateWispTransforms(0f);
        }

        private float RandomWispPhase() => (float)wispRandom.NextDouble() * Mathf.PI * 2f;

        private void LateUpdate()
        {
            if (!Application.isPlaying)
                return;

            if (wispsVisible != showWisps)
                RefreshWisps();

            UpdateWispTransforms(Time.deltaTime);
        }

        private void UpdateWispTransforms(float deltaTime)
        {
            foreach (WispInstance wisp in wisps)
            {
                WispMotionSettings settings = GetWispMotionSettings(wisp.type);
                float[] variation = wisp.variations;
                if (wisp.type != StatType.StormWisp)
                    wisp.angle = Mathf.Repeat(wisp.angle + settings.orbitSpeed.Evaluate(variation[1]) * Mathf.Deg2Rad * deltaTime, Mathf.PI * 2f);
                wisp.flutterPhase = Mathf.Repeat(wisp.flutterPhase + Mathf.Max(0f, settings.flutterFrequency.Evaluate(variation[4])) * Mathf.PI * 2f * deltaTime, Mathf.PI * 2f);
                wisp.pulsePhase = Mathf.Repeat(wisp.pulsePhase + Mathf.Max(0f, settings.pulseFrequency.Evaluate(variation[6])) * Mathf.PI * 2f * deltaTime, Mathf.PI * 2f);
                float radius = Mathf.Max(0f, settings.orbitRadius.Evaluate(variation[0])
                    + Mathf.Sin(wisp.flutterPhase) * Mathf.Max(0f, settings.flutterAmplitude.Evaluate(variation[3])));
                float size = Mathf.Max(0.01f, settings.size.Evaluate(variation[2]));
                float pulse = 1f + Mathf.Sin(wisp.pulsePhase) * Mathf.Clamp(settings.pulseAmount.Evaluate(variation[5]), 0f, 0.95f);
                SpriteRenderer renderer = wisp.renderer;
                if (renderer.sharedMaterial != wispGlowMaterial)
                    renderer.sharedMaterial = wispGlowMaterial;
                renderer.transform.localPosition = wisp.type == StatType.StormWisp
                    ? UpdateStormPosition(wisp, settings, deltaTime)
                    : new Vector3(Mathf.Cos(wisp.angle) * radius, Mathf.Sin(wisp.angle) * radius, -0.01f);
                renderer.transform.localScale = Vector3.one * size * pulse;
                // Sprite vertex colors can clamp HDR values; send emission directly to the shader.
                Color emission = GetWispColor(wisp.type);
                float intensity = Mathf.Max(0f, wispGlowIntensity);
                if (wisp.type == StatType.AshWisp) intensity *= pulse;
                emission.r *= intensity;
                emission.g *= intensity;
                emission.b *= intensity;
                ApplyWispPaint(renderer, settings, emission, wisp.type, false);
                renderer.sortingLayerID = meshRenderer.sortingLayerID;
                renderer.sortingOrder = meshRenderer.sortingOrder + 1;
                renderer.enabled = meshRenderer.enabled;
                TrailRenderer trail = wisp.trail;
                trail.sharedMaterial = wispGlowMaterial;
                trail.time = Mathf.Max(0f, settings.trailLifetime);
                Vector3 scale = transform.lossyScale;
                trail.widthMultiplier = Mathf.Max(0f, settings.trailWidth) * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                trail.sortingLayerID = renderer.sortingLayerID;
                trail.sortingOrder = renderer.sortingOrder;
                trail.enabled = renderer.enabled;
                if (!wisp.positioned || !renderer.enabled || Vector3.Distance(wisp.previousWorldPosition, renderer.transform.position) > Mathf.Max(1f, radius * 3f * scale.magnitude))
                    trail.Clear();
                trail.emitting = renderer.enabled && trail.time > 0f;
                wisp.positioned = true;
                wisp.previousWorldPosition = renderer.transform.position;
                float trailIntensity = Mathf.Max(0f, settings.trailIntensity);
                emission.r *= trailIntensity;
                emission.g *= trailIntensity;
                emission.b *= trailIntensity;
                ApplyWispPaint(trail, settings, emission, wisp.type, true);
            }
        }

        private void ApplyWispPaint(Renderer renderer, WispMotionSettings settings, Color emission, StatType type, bool trail)
        {
            wispColorProperties ??= new MaterialPropertyBlock();
            renderer.GetPropertyBlock(wispColorProperties);
            wispColorProperties.SetColor(WispEmissionColorId, emission);
            wispColorProperties.SetFloat("_TrailMode", trail ? 1f : 0f);
            wispColorProperties.SetFloat("_WispTheme", type == StatType.SteelWisp ? 0f : type == StatType.AshWisp ? 1f : type == StatType.FrostWisp ? 2f : 3f);
            wispColorProperties.SetFloat("_PaintStrength", settings.paintStrength);
            wispColorProperties.SetFloat("_PaintSteps", settings.paintSteps);
            wispColorProperties.SetFloat("_PaintSize", settings.paintSize);
            wispColorProperties.SetFloat("_PaintVariation", settings.paintVariation);
            renderer.SetPropertyBlock(wispColorProperties);
        }

        private Vector3 UpdateStormPosition(WispInstance wisp, WispMotionSettings settings, float deltaTime)
        {
            float radius = Mathf.Max(0f, settings.orbitRadius.Evaluate(wisp.variations[0]));
            if (wisp.stormDuration <= 0f)
            {
                wisp.stormTo = new Vector3(Mathf.Cos(wisp.angle) * radius, Mathf.Sin(wisp.angle) * radius, -0.01f);
                NextStormCorner(wisp, settings, radius);
            }
            // Bounded cosmetic catch-up after a stall; each leg is straight with a sharp corner.
            wisp.stormElapsed += Mathf.Clamp(deltaTime, 0f, 0.25f);
            while (wisp.stormElapsed >= wisp.stormDuration)
            {
                wisp.stormElapsed -= wisp.stormDuration;
                NextStormCorner(wisp, settings, radius);
            }
            float progress = wisp.stormElapsed / wisp.stormDuration;
            if (stormTrajectory.travelProgress != null && stormTrajectory.travelProgress.length > 0)
                progress = stormTrajectory.travelProgress.Evaluate(progress);
            return Vector3.Lerp(wisp.stormFrom, wisp.stormTo, progress);
        }

        private void NextStormCorner(WispInstance wisp, WispMotionSettings settings, float radius)
        {
            wisp.stormFrom = wisp.stormTo;
            wisp.stormDuration = Mathf.Max(0.02f, SampleStormRange(stormTrajectory.turnDuration));
            float jitter = SampleStormRange(stormTrajectory.angleOffset);
            wisp.angle = Mathf.Repeat(wisp.angle + (settings.orbitSpeed.Evaluate(wisp.variations[1]) * wisp.stormDuration + jitter) * Mathf.Deg2Rad, Mathf.PI * 2f);
            wisp.stormOuter = !wisp.stormOuter;
            float direction = stormTrajectory.radiusPattern == StormRadiusPattern.Random
                ? (wispRandom.NextDouble() < 0.5 ? -1f : 1f) : (wisp.stormOuter ? 1f : -1f);
            float excursion = Mathf.Max(0f, stormTrajectory.radiusExcursion.Evaluate(wisp.variations[3]));
            float offset = stormTrajectory.radiusPattern == StormRadiusPattern.Fixed
                ? stormTrajectory.fixedRadiusOffset
                : direction * excursion * Mathf.Max(0f, SampleStormRange(stormTrajectory.excursionMultiplier));
            radius = Mathf.Max(0f, radius + offset);
            wisp.stormTo = new Vector3(Mathf.Cos(wisp.angle) * radius, Mathf.Sin(wisp.angle) * radius, -0.01f);
        }

        private float SampleStormRange(Vector2 range) => Mathf.Lerp(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y), (float)wispRandom.NextDouble());

        private void EnsureStormTrajectorySettings()
        {
            stormTrajectory ??= new StormTrajectorySettings();
            if (stormTrajectory.initialized) return;
            // Preserve previously authored Storm values when moving them to the trajectory group.
            stormTrajectory.turnDuration = new Vector2(stormWisp.turnInterval * 0.75f, stormWisp.turnInterval * 1.25f);
            stormTrajectory.angleOffset = new Vector2(-stormWisp.turnJitter, stormWisp.turnJitter);
            stormTrajectory.radiusExcursion = stormWisp.flutterAmplitude;
            stormTrajectory.initialized = true;
        }

        private void EnsureWispMotionSettings()
        {
            if (!wispMotionSettingsInitialized)
            {
                steelWisp = CreateWispMotionSettings();
                ashWisp = CreateWispMotionSettings();
                frostWisp = CreateWispMotionSettings();
                stormWisp = CreateWispMotionSettings();
                wispMotionSettingsInitialized = true;
            }
            if (wispStyleVersion >= 1)
            {
                EnsureStormTrajectorySettings();
                return;
            }
            // One-time visual migration preserves authored radius, size and colors.
            steelWisp.orbitSpeed = new WispRange(210f, 0f);
            steelWisp.flutterAmplitude = new WispRange(0f, 0f);
            steelWisp.pulseAmount = new WispRange(0f, 0f);
            steelWisp.trailLifetime = 0.22f; steelWisp.trailWidth = 0.055f; steelWisp.paintSize = 0.06f;
            ashWisp.orbitSpeed = new WispRange(55f, 8f);
            ashWisp.flutterAmplitude = new WispRange(0.07f, 0.02f);
            ashWisp.flutterFrequency = new WispRange(0.35f, 0.05f);
            ashWisp.pulseAmount = new WispRange(0.3f, 0.06f);
            ashWisp.pulseFrequency = new WispRange(1.8f, 0.3f);
            ashWisp.trailLifetime = 0.45f; ashWisp.trailWidth = 0.14f; ashWisp.paintSize = 0.12f;
            frostWisp.orbitSpeed = new WispRange(25f, 4f);
            frostWisp.flutterAmplitude = new WispRange(0.2f, 0.04f);
            frostWisp.flutterFrequency = new WispRange(0.15f, 0.025f);
            frostWisp.pulseAmount = new WispRange(0.08f, 0.02f);
            frostWisp.trailLifetime = 0.8f; frostWisp.trailWidth = 0.09f; frostWisp.paintSize = 0.08f;
            stormWisp.orbitSpeed = new WispRange(140f, 20f);
            stormWisp.flutterAmplitude = new WispRange(0.3f, 0.05f);
            stormWisp.pulseFrequency = new WispRange(5f, 1f);
            stormWisp.trailLifetime = 0.32f; stormWisp.trailWidth = 0.06f; stormWisp.paintSize = 0.055f;
            wispStyleVersion = 1;
            EnsureStormTrajectorySettings();
        }

        private WispMotionSettings CreateWispMotionSettings() => new WispMotionSettings
        {
            orbitRadius = new WispRange(wispOrbitRadius, Mathf.Abs(wispOrbitRadius) * 0.12f),
            orbitSpeed = new WispRange(wispOrbitSpeed, Mathf.Abs(wispOrbitSpeed) * 0.2f),
            size = new WispRange(wispSize, Mathf.Abs(wispSize) * 0.15f),
            flutterAmplitude = new WispRange(wispFlutter, Mathf.Abs(wispFlutter) * 0.3f)
        };

        private WispMotionSettings GetWispMotionSettings(StatType type)
        {
            switch (type)
            {
                case StatType.SteelWisp: return steelWisp;
                case StatType.AshWisp: return ashWisp;
                case StatType.FrostWisp: return frostWisp;
                default: return stormWisp;
            }
        }

        private Color GetWispColor(StatType type)
        {
            switch (type)
            {
                case StatType.SteelWisp: return steelWispColor;
                case StatType.AshWisp: return ashWispColor;
                case StatType.FrostWisp: return frostWispColor;
                default: return stormWispColor;
            }
        }

        private void EnsureWispSprite()
        {
            if (wispSprite != null)
                return;

            const int size = 64;
            wispTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Wisp Glow", hideFlags = HideFlags.DontSave,
                wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float radius = new Vector2((x + 0.5f) / size * 2f - 1f, (y + 0.5f) / size * 2f - 1f).magnitude;
                float halo = Mathf.Pow(Mathf.Clamp01(1f - radius), 2f);
                float core = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.3f, radius));
                pixels[y * size + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + halo * 0.65f));
            }
            wispTexture.SetPixels(pixels);
            wispTexture.Apply(false, true);
            wispSprite = Sprite.Create(wispTexture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            wispSprite.name = "Wisp Glow";
            wispSprite.hideFlags = HideFlags.DontSave;
        }

        private void DestroyWisps()
        {
            foreach (WispInstance wisp in wisps)
            {
                SpriteRenderer renderer = wisp.renderer;
                if (renderer == null) continue;
                renderer.gameObject.SetActive(false);
                ReleaseWispObject(renderer.gameObject);
            }
            wisps.Clear();
            ReleaseWispObject(wispSprite);
            ReleaseWispObject(wispTexture);
            wispSprite = null;
            wispTexture = null;
        }

        private static void ReleaseWispObject(Object value)
        {
            if (value == null) return;
            if (Application.isPlaying) Destroy(value);
            else DestroyImmediate(value);
        }
    }
}
