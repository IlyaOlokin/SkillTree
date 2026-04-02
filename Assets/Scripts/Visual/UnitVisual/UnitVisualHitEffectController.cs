using Battle;
using DG.Tweening;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Visual
{
    [Serializable]
    public class UnitVisualHitEffectController
    {
        [SerializeField] private SpriteRenderer unitVisual;
        [Header("Flash")]
        [SerializeField] private Transform wobbleTransform;
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashInDuration = 0.06f;
        [SerializeField] private float flashOutDuration = 0.14f;
        [SerializeField] private float wobbleDuration = 0.18f;
        [SerializeField] private float wobbleZStrength = 8f;
        [SerializeField] private int wobbleVibrato = 12;
        [SerializeField] private float wobbleRandomness = 40f;

        [Header("Hit")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifetimeFallback = 1f;
        [SerializeField] private float hitEffectDestroyCheckDelay = 0.2f;

        private Color _baseColor;
        private Quaternion _baseWobbleLocalRotation;
        private bool _isInitialized;

        private Sequence _hitSequence;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _baseColor = unitVisual != null ? unitVisual.color : Color.white;
            _baseWobbleLocalRotation = wobbleTransform != null ? wobbleTransform.localRotation : Quaternion.identity;
            _isInitialized = true;
        }

        public void PlayHitEffect(DamageInfo damageInfo)
        {
            Initialize();

            if (unitVisual == null || !HasDamage(damageInfo.DamageInstance))
            {
                return;
            }

            UnitFlash();
            HitEffect();
        }

        private void HitEffect()
        {
            if (hitEffectPrefab == null || unitVisual == null)
            {
                return;
            }

            var hitEffectInstance = Object.Instantiate(
                hitEffectPrefab,
                unitVisual.bounds.center,
                hitEffectPrefab.transform.rotation);
            var autoDestroy = hitEffectInstance.GetComponent<AutoDestroyVisualEffect>();
            if (autoDestroy == null)
            {
                autoDestroy = hitEffectInstance.AddComponent<AutoDestroyVisualEffect>();
            }

            autoDestroy.Initialize(hitEffectLifetimeFallback, hitEffectDestroyCheckDelay);
        }

        private void UnitFlash()
        {
            _hitSequence?.Kill();
            unitVisual.color = _baseColor;
            ResetWobbleRotation();

            _hitSequence = DOTween.Sequence();

            _hitSequence.Append(unitVisual.DOColor(flashColor, flashInDuration).SetEase(Ease.OutQuad));

            if (wobbleTransform != null)
            {
                _hitSequence.Join(wobbleTransform.DOPunchRotation(
                    new Vector3(0f, 0f, wobbleZStrength),
                    wobbleDuration,
                    wobbleVibrato,
                    wobbleRandomness));
            }

            _hitSequence.Append(unitVisual.DOColor(_baseColor, flashOutDuration).SetEase(Ease.InQuad));
            _hitSequence.OnComplete(ResetWobbleRotation);
            _hitSequence.OnKill(ResetWobbleRotation);
        }

        public void Dispose()
        {
            _hitSequence?.Kill();
            if (unitVisual != null)
            {
                unitVisual.color = _baseColor;
            }

            ResetWobbleRotation();
        }

        private static bool HasDamage(DamageInstance damageInstance)
        {
            if (damageInstance == null || damageInstance.Damage == null)
            {
                return false;
            }

            foreach (var pair in damageInstance.Damage)
            {
                if (pair.Value > 0f)
                {
                    return true;
                }
            }

            return false;
        }

        private void ResetWobbleRotation()
        {
            if (wobbleTransform == null)
            {
                return;
            }

            wobbleTransform.localRotation = _baseWobbleLocalRotation;
        }
    }
}
