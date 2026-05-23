using UnityEngine;

namespace Visual
{
    public class NodePowerVisual : MonoBehaviour
    {
        private static readonly int PowerId = Shader.PropertyToID("_Power");
        private static readonly int PowerColorId = Shader.PropertyToID("_PowerColor");
        private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
        private static readonly int RingIntensityId = Shader.PropertyToID("_RingIntensity");
        private static readonly int RayIntensityId = Shader.PropertyToID("_RayIntensity");
        private const float VisiblePowerThreshold = 0.001f;

        [Header("References")]
        [SerializeField] private Renderer auraRenderer;

        [Header("Power Mapping")]
        [SerializeField] [Min(0.0001f)] private float powerForFullEffect = 1f;
        [SerializeField] private bool hideWhenPowerIsZero = true;
        [SerializeField] [Min(0f)] private float smoothingSpeed = 10f;

        [Header("Color")]
        [SerializeField] [GradientUsage(true)] private Gradient powerColor = CreateDefaultPowerGradient();
        [SerializeField] [ColorUsage(true, true)] private Color deallocatedPowerColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        [SerializeField] [Min(0f)] private float allocationColorSmoothingSpeed = 7f;

        [Header("Shader Intensity")]
        [SerializeField] private AnimationCurve glowIntensityByPower = AnimationCurve.EaseInOut(0f, 0.1f, 1f, 3.5f);
        [SerializeField] private AnimationCurve ringIntensityByPower = AnimationCurve.EaseInOut(0f, 0.25f, 1f, 2.2f);
        [SerializeField] private AnimationCurve rayIntensityByPower = AnimationCurve.EaseInOut(0f, 0f, 1f, 3f);

        private MaterialPropertyBlock _propertyBlock;
        private float _targetPower;
        private float _currentPower;
        private float _targetAllocatedColorWeight = 1f;
        private float _currentAllocatedColorWeight = 1f;
        private bool _targetHasPower;
        private bool _presentationVisible = true;
        private bool _isInitialized;
        private bool _hasReceivedState;

        private void Reset()
        {
            auraRenderer = FindAuraRenderer();
        }

        private void Awake()
        {
            Initialize();
            _currentPower = _targetPower;
            _currentAllocatedColorWeight = _targetAllocatedColorWeight;
            ApplyPower(_currentPower);
        }

        private void OnEnable()
        {
            Initialize();
            ApplyPower(_currentPower);
        }

        private void Update()
        {
            if (Mathf.Approximately(_currentPower, _targetPower) &&
                Mathf.Approximately(_currentAllocatedColorWeight, _targetAllocatedColorWeight))
                return;

            float powerStep = smoothingSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-smoothingSpeed * Time.deltaTime);
            float colorStep = allocationColorSmoothingSpeed <= 0f
                ? 1f
                : 1f - Mathf.Exp(-allocationColorSmoothingSpeed * Time.deltaTime);

            _currentPower = Mathf.Lerp(_currentPower, _targetPower, powerStep);
            _currentAllocatedColorWeight = Mathf.Lerp(_currentAllocatedColorWeight, _targetAllocatedColorWeight, colorStep);
            ApplyPower(_currentPower);
        }

        public void SetPower(float power)
        {
            float normalizedPower = Mathf.Clamp01(power / Mathf.Max(0.0001f, powerForFullEffect));
            SetNormalizedPower(normalizedPower, _targetAllocatedColorWeight > 0.5f, power > 0f);
        }

        public void SetPower(float power, bool isAllocated)
        {
            float normalizedPower = Mathf.Clamp01(power / Mathf.Max(0.0001f, powerForFullEffect));
            SetNormalizedPower(normalizedPower, isAllocated, power > 0f);
        }

        public void SetNormalizedPower(float normalizedPower)
        {
            SetNormalizedPower(normalizedPower, _targetAllocatedColorWeight > 0.5f);
        }

        public void SetNormalizedPower(float normalizedPower, bool isAllocated)
        {
            SetNormalizedPower(normalizedPower, isAllocated, normalizedPower > VisiblePowerThreshold);
        }

        public Renderer ManagedRenderer
        {
            get
            {
                Initialize();
                return auraRenderer;
            }
        }

        public void SetPresentationVisible(bool visible)
        {
            Initialize();

            if (_presentationVisible == visible)
                return;

            _presentationVisible = visible;
            ApplyPower(_currentPower);
        }

        private void SetNormalizedPower(float normalizedPower, bool isAllocated, bool hasPower)
        {
            Initialize();

            _targetPower = Mathf.Clamp01(normalizedPower);
            _targetHasPower = hasPower;
            _targetAllocatedColorWeight = isAllocated ? 1f : 0f;
            if (!Application.isPlaying || !_hasReceivedState)
            {
                _currentPower = _targetPower;
                _currentAllocatedColorWeight = _targetAllocatedColorWeight;
                _hasReceivedState = true;
            }

            ApplyPower(_currentPower);
        }

        private void Initialize()
        {
            if (_isInitialized)
                return;

            if (auraRenderer == null)
                auraRenderer = FindAuraRenderer();

            _propertyBlock = new MaterialPropertyBlock();
            _isInitialized = true;
        }

        private void ApplyPower(float normalizedPower)
        {
            bool visible = _presentationVisible &&
                           (!hideWhenPowerIsZero || _targetHasPower || normalizedPower > VisiblePowerThreshold);
            ApplyAura(normalizedPower, visible);
        }

        private void ApplyAura(float normalizedPower, bool visible)
        {
            if (auraRenderer == null)
                return;

            auraRenderer.enabled = visible;
            if (!visible)
                return;

            Color color = Color.Lerp(deallocatedPowerColor, powerColor.Evaluate(normalizedPower), _currentAllocatedColorWeight);

            auraRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(PowerId, normalizedPower);
            _propertyBlock.SetColor(PowerColorId, color);
            _propertyBlock.SetFloat(GlowIntensityId, Mathf.Max(0f, glowIntensityByPower.Evaluate(normalizedPower)));
            _propertyBlock.SetFloat(RingIntensityId, Mathf.Max(0f, ringIntensityByPower.Evaluate(normalizedPower)));
            _propertyBlock.SetFloat(RayIntensityId, Mathf.Max(0f, rayIntensityByPower.Evaluate(normalizedPower)));
            auraRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Renderer FindAuraRenderer()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer is not ParticleSystemRenderer)
                    return renderer;
            }

            return null;
        }

        private static Gradient CreateDefaultPowerGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.85f, 0.82f, 0.76f, 1f), 0f),
                    new GradientColorKey(new Color(1.6f, 1.55f, 1.42f, 1f), 0.3f),
                    new GradientColorKey(new Color(0.3f, 1.0f, 2.4f, 1f), 0.5f),
                    new GradientColorKey(new Color(2.8f, 1.35f, 0.18f, 1f), 0.65f),
                    new GradientColorKey(new Color(4.2f, 1.75f, 0.22f, 1f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.65f, 0f),
                    new GradientAlphaKey(1f, 0.25f),
                    new GradientAlphaKey(1f, 1f)
                });

            return gradient;
        }
    }
}
