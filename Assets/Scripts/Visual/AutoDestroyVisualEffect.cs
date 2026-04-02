using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Visual
{
    public class AutoDestroyVisualEffect : MonoBehaviour
    {
        private float _fallbackLifetime;
        private float _destroyCheckDelay;
        private float _elapsedTime;
        [SerializeField] private List<VisualEffect> _visualEffects = new List<VisualEffect>();
        private bool _isInitialized;
        private bool _hasSeenAliveParticles;

        public void Initialize(float fallbackLifetime, float destroyCheckDelay)
        {
            _fallbackLifetime = Mathf.Max(0.01f, fallbackLifetime);
            _destroyCheckDelay = Mathf.Max(0f, destroyCheckDelay);
            _elapsedTime = 0f;
            _hasSeenAliveParticles = false;
            _isInitialized = true;

            if (_visualEffects.Count == 0)
            {
                Destroy(gameObject, _fallbackLifetime);
            }
        }

        private void Update()
        {
            if (!_isInitialized || _visualEffects == null || _visualEffects.Count == 0)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;

            bool hasAliveParticles = false;
            for (int i = 0; i < _visualEffects.Count; i++)
            {
                var visualEffect = _visualEffects[i];
                if (visualEffect != null && visualEffect.aliveParticleCount > 0)
                {
                    hasAliveParticles = true;
                    break;
                }
            }

            if (hasAliveParticles)
            {
                _hasSeenAliveParticles = true;
                return;
            }

            if (_elapsedTime < _destroyCheckDelay)
            {
                return;
            }

            if (_hasSeenAliveParticles || _elapsedTime >= _fallbackLifetime)
            {
                Destroy(gameObject);
            }
        }
    }
}
