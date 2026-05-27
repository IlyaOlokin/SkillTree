using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace Visual
{
    public class AutoDestroyVisualEffect : MonoBehaviour
    {
        private float _fallbackLifetime;
        private float _destroyCheckDelay;
        private double _startRealtime;
        [SerializeField] private List<VisualEffect> _visualEffects = new List<VisualEffect>();
        private bool _isInitialized;
        private bool _hasSeenAliveParticles;

        public void Initialize(float fallbackLifetime, float destroyCheckDelay)
        {
            _fallbackLifetime = Mathf.Max(0.01f, fallbackLifetime);
            _destroyCheckDelay = Mathf.Max(0f, destroyCheckDelay);
            _startRealtime = Time.realtimeSinceStartupAsDouble;
            _hasSeenAliveParticles = false;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized)
            {
                return;
            }

            float elapsedTime = (float)(Time.realtimeSinceStartupAsDouble - _startRealtime);
            if (elapsedTime >= _fallbackLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (_visualEffects == null || _visualEffects.Count == 0)
            {
                return;
            }

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

            if (elapsedTime < _destroyCheckDelay)
            {
                return;
            }

            if (_hasSeenAliveParticles)
            {
                Destroy(gameObject);
            }
        }
    }
}
