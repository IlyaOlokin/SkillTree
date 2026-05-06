using UnityEngine;

namespace VFX
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Renderer))]
    public class ProceduralShockwavePlayer : MonoBehaviour
    {
        private static readonly int LifePropertyId = Shader.PropertyToID("_Life");
        private static readonly int DominantBaseDamageTypePropertyId = Shader.PropertyToID("_DominantBaseDamageType");

        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private float duration = 1f;
        [SerializeField] private bool playOnEnable = true;
        [SerializeField] private bool destroyOnFinish;

        private MaterialPropertyBlock _propertyBlock;
        private float _elapsed;
        private bool _isPlaying;
        private int _dominantBaseDamageType;

        public bool IsPlaying => _isPlaying;

        private void Reset()
        {
            targetRenderer = GetComponent<Renderer>();
        }

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            _propertyBlock = new MaterialPropertyBlock();
            SetLife(0f);
            SetDominantBaseDamageType(_dominantBaseDamageType);
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                Play();
            }
        }

        private void Update()
        {
            if (!_isPlaying)
            {
                return;
            }
            
            _elapsed += Time.deltaTime;
            float normalizedLife = duration <= 0f ? 1f : Mathf.Clamp01(_elapsed / duration);
            SetLife(normalizedLife);

            if (normalizedLife < 1f)
            {
                return;
            }

            _isPlaying = false;

            if (destroyOnFinish)
            {
                Destroy(gameObject);
            }
        }

        public void Play()
        {
            _elapsed = 0f;
            _isPlaying = true;
            SetLife(0f);
        }

        public void Play(float newDuration)
        {
            duration = newDuration;
            Play();
        }

        public void Stop(bool resetLife = false)
        {
            _isPlaying = false;

            if (resetLife)
            {
                _elapsed = 0f;
                SetLife(0f);
            }
        }

        public void SetNormalizedLife(float life)
        {
            _elapsed = Mathf.Clamp01(life) * Mathf.Max(duration, 0f);
            SetLife(life);
        }

        public void SetDominantBaseDamageType(int dominantBaseDamageType)
        {
            _dominantBaseDamageType = dominantBaseDamageType;

            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetInt(DominantBaseDamageTypePropertyId, _dominantBaseDamageType);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void SetLife(float life)
        {
            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetFloat(LifePropertyId, Mathf.Clamp01(life));
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}
