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
        [Header("Scale")]
        [SerializeField] private Transform scaleTransform;
        [SerializeField] private float hitScaleMultiplier = 0.92f;
        [SerializeField] private float scaleInDuration = 0.04f;
        [SerializeField] private float scaleOutDuration = 0.1f;

        [Header("Hit")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject swordHitEffectPrefab;
        [SerializeField] private GameObject staffHitEffectPrefab;
        [SerializeField] private GameObject hammerHitEffectPrefab;
        [SerializeField] private float hitEffectLifetimeFallback = 1f;
        [SerializeField] private float hitEffectDestroyCheckDelay = 0.2f;

        private Color _baseColor;
        private Quaternion _baseWobbleLocalRotation;
        private Vector3 _baseScale;
        private bool _isInitialized;
        private bool _isReplacingHitSequence;

        private Sequence _hitSequence;

        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            _baseColor = unitVisual != null ? unitVisual.color : Color.white;
            _baseWobbleLocalRotation = wobbleTransform != null ? wobbleTransform.localRotation : Quaternion.identity;
            if (scaleTransform == null)
            {
                scaleTransform = wobbleTransform;
            }

            _baseScale = scaleTransform != null ? scaleTransform.localScale : Vector3.one;
            _isInitialized = true;
        }

        public void PlayHitEffect(DamageInfo damageInfo)
        {
            Initialize();

            if (unitVisual == null || damageInfo == null || !HasDamage(damageInfo.DamageInstance))
            {
                return;
            }

            UnitFlash();
            HitEffect(damageInfo.Owner != null ? damageInfo.Owner.WeaponType : WeaponType.Unarmed);
        }

        private void HitEffect(WeaponType attackerWeaponType)
        {
            GameObject effectPrefab = GetHitEffectPrefab(attackerWeaponType);
            if (effectPrefab == null || unitVisual == null)
            {
                return;
            }

            var hitEffectInstance = Object.Instantiate(
                effectPrefab,
                unitVisual.bounds.center,
                effectPrefab.transform.rotation);
            var autoDestroy = hitEffectInstance.GetComponent<AutoDestroyVisualEffect>();
            if (autoDestroy == null)
            {
                autoDestroy = hitEffectInstance.AddComponent<AutoDestroyVisualEffect>();
            }

            autoDestroy.Initialize(hitEffectLifetimeFallback, hitEffectDestroyCheckDelay);
        }

        private GameObject GetHitEffectPrefab(WeaponType attackerWeaponType)
        {
            return attackerWeaponType switch
            {
                WeaponType.Sword => swordHitEffectPrefab != null ? swordHitEffectPrefab : hitEffectPrefab,
                WeaponType.Staff => staffHitEffectPrefab != null ? staffHitEffectPrefab : hitEffectPrefab,
                WeaponType.Hammer => hammerHitEffectPrefab != null ? hammerHitEffectPrefab : hitEffectPrefab,
                _ => hitEffectPrefab
            };
        }

        private void UnitFlash()
        {
            _isReplacingHitSequence = true;
            _hitSequence?.Kill();
            _isReplacingHitSequence = false;

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

            if (scaleTransform != null)
            {
                _hitSequence.Join(scaleTransform
                    .DOScale(_baseScale * Mathf.Max(0f, hitScaleMultiplier), scaleInDuration)
                    .SetEase(Ease.OutQuad));
            }

            _hitSequence.Append(unitVisual.DOColor(_baseColor, flashOutDuration).SetEase(Ease.InQuad));

            if (scaleTransform != null)
            {
                _hitSequence.Join(scaleTransform
                    .DOScale(_baseScale, scaleOutDuration)
                    .SetEase(Ease.OutQuad));
            }

            _hitSequence.OnComplete(ResetTransformEffects);
            _hitSequence.OnKill(ResetTransformEffectsOnKill);
        }

        public void Dispose()
        {
            _hitSequence?.Kill();
            if (unitVisual != null)
            {
                unitVisual.color = _baseColor;
            }

            ResetWobbleRotation();
            ResetScale();
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

        private void ResetScale()
        {
            if (scaleTransform == null)
            {
                return;
            }

            scaleTransform.localScale = _baseScale;
        }

        private void ResetTransformEffects()
        {
            ResetWobbleRotation();
            ResetScale();
        }

        private void ResetTransformEffectsOnKill()
        {
            if (_isReplacingHitSequence)
            {
                return;
            }

            ResetTransformEffects();
        }
    }
}
