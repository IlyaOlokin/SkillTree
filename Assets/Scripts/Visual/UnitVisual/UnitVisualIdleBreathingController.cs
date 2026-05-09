using DG.Tweening;
using System;
using UnityEngine;

namespace Visual
{
    [Serializable]
    public class UnitVisualIdleBreathingController
    {
        [SerializeField] private Transform breathingTransform;
        [SerializeField] private Vector3 inhaleScaleMultiplier = new Vector3(1.015f, 1.035f, 1f);
        [SerializeField] private float inhaleDuration = 1.1f;
        [SerializeField] private Vector2 startDelayRange = new Vector2(0f, 0.7f);
        [SerializeField] private Ease ease = Ease.InOutSine;

        private GameObject _ownerGameObject;
        private Tween _breathingTween;
        private Vector3 _baseScale;
        private bool _isInitialized;

        public void Initialize(Transform ownerTransform, GameObject ownerGameObject)
        {
            if (_isInitialized)
            {
                return;
            }

            _ownerGameObject = ownerGameObject;

            if (breathingTransform == null)
            {
                breathingTransform = ownerTransform;
            }

            _baseScale = breathingTransform != null ? breathingTransform.localScale : Vector3.one;
            _isInitialized = true;
        }

        public void Play()
        {
            if (!_isInitialized || breathingTransform == null)
            {
                return;
            }

            _breathingTween?.Kill();
            breathingTransform.localScale = _baseScale;

            _breathingTween = breathingTransform
                .DOScale(Vector3.Scale(_baseScale, inhaleScaleMultiplier), Mathf.Max(0.01f, inhaleDuration))
                .SetEase(ease)
                .SetDelay(GetRandomStartDelay())
                .SetLoops(-1, LoopType.Yoyo);

            if (_ownerGameObject != null)
            {
                _breathingTween.SetLink(_ownerGameObject);
            }
        }

        public void Dispose()
        {
            _breathingTween?.Kill();
            ResetScale();
        }

        private void ResetScale()
        {
            if (!_isInitialized || breathingTransform == null)
            {
                return;
            }

            breathingTransform.localScale = _baseScale;
        }

        private float GetRandomStartDelay()
        {
            float minDelay = Mathf.Min(startDelayRange.x, startDelayRange.y);
            float maxDelay = Mathf.Max(startDelayRange.x, startDelayRange.y);
            return maxDelay > 0f ? UnityEngine.Random.Range(Mathf.Max(0f, minDelay), maxDelay) : 0f;
        }
    }
}
