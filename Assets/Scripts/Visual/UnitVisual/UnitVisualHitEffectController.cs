using Battle;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;
using VFX;
using Object = UnityEngine.Object;

namespace Visual
{
    [Serializable]
    public class UnitVisualHitEffectController
    {
        private static readonly int GlowColorId = Shader.PropertyToID("_Color");
        private static readonly int CoreColorId = Shader.PropertyToID("_CoreColor");

        [FormerlySerializedAs("unitVisual")]
        [SerializeField] private Renderer unitRenderer;
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
        [Header("Knockback")]
        [SerializeField] private Transform knockbackTransform;
        [FormerlySerializedAs("attackMoveDirection")]
        [SerializeField] private Vector2 combatDirection = Vector2.right;
        [SerializeField] private float knockbackDistance = 0.1f;
        [SerializeField] private float knockbackInDuration = 0.05f;
        [SerializeField] private float knockbackReturnDuration = 0.12f;

        [Header("Hit")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject swordHitEffectPrefab;
        [SerializeField] private GameObject staffHitEffectPrefab;
        [SerializeField] private GameObject hammerHitEffectPrefab;
        [SerializeField] private float hitEffectLifetimeFallback = 1f;
        [SerializeField] private float hitEffectDestroyCheckDelay = 0.2f;
        [Header("VFX Damage Flags")]
        [SerializeField] private string physicalDamageFlag = "Physical";
        [SerializeField] private string fireDamageFlag = "Fire";
        [SerializeField] private string coldDamageFlag = "Cold";
        [SerializeField] private string lightningDamageFlag = "Lightning";
        [SerializeField] private string lightDamageFlag = "Light";
        [SerializeField] private string darknessDamageFlag = "Darkness";
        [SerializeField] private string dominantBaseDamageTypeProperty = "DominantBaseDamageType";

        private Color _baseColor;
        private Color _baseGlowColor;
        private Color _baseCoreColor;
        private Transform _ownerTransform;
        private Quaternion _baseWobbleLocalRotation;
        private Vector3 _baseScale;
        private Vector3 _baseKnockbackLocalPosition;
        private bool _isInitialized;
        private bool _isReplacingHitSequence;
        private SpriteRenderer _spriteRenderer;
        private UnitShapeView _unitShapeView;
        private MaterialPropertyBlock _propertyBlock;
        private VisualEffect[] _spawnedHitEffectsBuffer = Array.Empty<VisualEffect>();
        private ProceduralShockwavePlayer[] _spawnedShockwavePlayersBuffer = Array.Empty<ProceduralShockwavePlayer>();

        private Sequence _hitSequence;

        public void Initialize(Transform ownerTransform, Vector2 fallbackCombatDirection)
        {
            if (_isInitialized)
            {
                return;
            }

            _ownerTransform = ownerTransform;
            ResolveUnitRenderer();
            SetCombatDirection(fallbackCombatDirection);
            CaptureBaseVisualColors();
            _baseWobbleLocalRotation = wobbleTransform != null ? wobbleTransform.localRotation : Quaternion.identity;
            if (scaleTransform == null)
            {
                scaleTransform = wobbleTransform;
            }

            if (knockbackTransform == null)
            {
                knockbackTransform = scaleTransform != null ? scaleTransform : wobbleTransform;
            }

            _baseScale = scaleTransform != null ? scaleTransform.localScale : Vector3.one;
            _baseKnockbackLocalPosition = knockbackTransform != null ? knockbackTransform.localPosition : Vector3.zero;
            _isInitialized = true;
        }

        public void SetCombatDirection(Vector2 fallbackCombatDirection)
        {
            if (fallbackCombatDirection.sqrMagnitude > 0.0001f)
            {
                combatDirection = fallbackCombatDirection.normalized;
            }
        }

        public void PlayHitEffect(DamageInfo damageInfo)
        {
            Initialize(_ownerTransform, combatDirection);

            if (unitRenderer == null || damageInfo == null || !HasDamage(damageInfo.DamageInstance))
            {
                return;
            }

            UnitFlash(damageInfo);
            HitEffect(damageInfo);
        }

        private void HitEffect(DamageInfo damageInfo)
        {
            WeaponType attackerWeaponType = damageInfo.Owner != null ? damageInfo.Owner.WeaponType : WeaponType.Unarmed;
            GameObject effectPrefab = GetHitEffectPrefab(attackerWeaponType);
            if (effectPrefab == null || unitRenderer == null)
            {
                return;
            }

            var hitEffectInstance = Object.Instantiate(
                effectPrefab,
                unitRenderer.bounds.center,
                effectPrefab.transform.rotation);
            var autoDestroy = hitEffectInstance.GetComponent<AutoDestroyVisualEffect>();
            if (autoDestroy == null)
            {
                autoDestroy = hitEffectInstance.AddComponent<AutoDestroyVisualEffect>();
            }

            ApplyDamageTypeFlags(hitEffectInstance, damageInfo);
            autoDestroy.Initialize(hitEffectLifetimeFallback, hitEffectDestroyCheckDelay);
        }

        private GameObject GetHitEffectPrefab(WeaponType attackerWeaponType)
        {
            return attackerWeaponType switch
            {
                WeaponType.Sword => swordHitEffectPrefab != null ? swordHitEffectPrefab : hitEffectPrefab,
                WeaponType.FireStaff or WeaponType.ColdStaff or WeaponType.LightningStaff => staffHitEffectPrefab != null ? staffHitEffectPrefab : hitEffectPrefab,
                WeaponType.Hammer => hammerHitEffectPrefab != null ? hammerHitEffectPrefab : hitEffectPrefab,
                _ => hitEffectPrefab
            };
        }

        private void UnitFlash(DamageInfo damageInfo)
        {
            _isReplacingHitSequence = true;
            _hitSequence?.Kill();
            _isReplacingHitSequence = false;

            ResetVisualColor();
            ResetWobbleRotation();
            ResetKnockbackPosition();

            _hitSequence = DOTween.Sequence();
            Vector3 knockbackPosition = _baseKnockbackLocalPosition + GetKnockbackDirection(damageInfo) * knockbackDistance;

            _hitSequence.Append(CreateVisualColorTween(1f, flashInDuration).SetEase(Ease.OutQuad));

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

            if (knockbackTransform != null && knockbackDistance > 0f)
            {
                _hitSequence.Join(knockbackTransform
                    .DOLocalMove(knockbackPosition, knockbackInDuration)
                    .SetEase(Ease.OutQuad));
            }

            _hitSequence.Append(CreateVisualColorTween(0f, flashOutDuration).SetEase(Ease.InQuad));

            if (scaleTransform != null)
            {
                _hitSequence.Join(scaleTransform
                    .DOScale(_baseScale, scaleOutDuration)
                    .SetEase(Ease.OutQuad));
            }

            if (knockbackTransform != null)
            {
                _hitSequence.Join(knockbackTransform
                    .DOLocalMove(_baseKnockbackLocalPosition, knockbackReturnDuration)
                    .SetEase(Ease.OutQuad));
            }

            _hitSequence.OnComplete(ResetTransformEffects);
            _hitSequence.OnKill(ResetTransformEffectsOnKill);
        }

        public void Dispose()
        {
            _hitSequence?.Kill();
            ResetVisualColor();

            ResetWobbleRotation();
            ResetScale();
            ResetKnockbackPosition();
        }

        private void ResolveUnitRenderer()
        {
            if (_ownerTransform == null)
            {
                return;
            }

            if (_unitShapeView == null)
            {
                _unitShapeView = _ownerTransform.GetComponentInChildren<UnitShapeView>(true);
            }

            if (unitRenderer == null && _unitShapeView != null)
            {
                unitRenderer = _unitShapeView.GetComponent<Renderer>();
            }

            if (unitRenderer == null)
            {
                SpriteRenderer[] spriteRenderers = _ownerTransform.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i] == null || spriteRenderers[i].name == "Weapon")
                    {
                        continue;
                    }

                    unitRenderer = spriteRenderers[i];
                    break;
                }
            }

            if (_unitShapeView == null && unitRenderer != null)
            {
                _unitShapeView = unitRenderer.GetComponent<UnitShapeView>();
            }

            _spriteRenderer = unitRenderer as SpriteRenderer;
            _propertyBlock ??= new MaterialPropertyBlock();
        }

        private void CaptureBaseVisualColors()
        {
            _spriteRenderer = unitRenderer as SpriteRenderer;
            if (_spriteRenderer != null)
            {
                _baseColor = _spriteRenderer.color;
                return;
            }

            if (_unitShapeView != null)
            {
                _baseGlowColor = _unitShapeView.GlowColor;
                _baseCoreColor = _unitShapeView.CoreColor;
                return;
            }

            _baseGlowColor = GetMaterialColor(GlowColorId, Color.white);
            _baseCoreColor = GetMaterialColor(CoreColorId, Color.white);
        }

        private Tween CreateVisualColorTween(float targetBlend, float duration)
        {
            float blend = targetBlend > 0.5f ? 0f : 1f;
            return DOTween.To(
                () => blend,
                value =>
                {
                    blend = value;
                    ApplyVisualFlashBlend(blend);
                },
                targetBlend,
                duration);
        }

        private void ApplyVisualFlashBlend(float blend)
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = Color.Lerp(_baseColor, flashColor, blend);
                return;
            }

            if (unitRenderer == null)
            {
                return;
            }

            unitRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(GlowColorId, Color.Lerp(_baseGlowColor, flashColor, blend));
            _propertyBlock.SetColor(CoreColorId, Color.Lerp(_baseCoreColor, flashColor, blend));
            unitRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResetVisualColor()
        {
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _baseColor;
                return;
            }

            if (unitRenderer == null)
            {
                return;
            }

            unitRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(GlowColorId, _baseGlowColor);
            _propertyBlock.SetColor(CoreColorId, _baseCoreColor);
            unitRenderer.SetPropertyBlock(_propertyBlock);
        }

        private Color GetMaterialColor(int propertyId, Color fallback)
        {
            if (unitRenderer == null || unitRenderer.sharedMaterial == null || !unitRenderer.sharedMaterial.HasProperty(propertyId))
            {
                return fallback;
            }

            return unitRenderer.sharedMaterial.GetColor(propertyId);
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

        private void ApplyDamageTypeFlags(GameObject hitEffectInstance, DamageInfo damageInfo)
        {
            if (hitEffectInstance == null || damageInfo?.DamageInstance?.Damage == null)
            {
                return;
            }

            _spawnedHitEffectsBuffer = hitEffectInstance.GetComponentsInChildren<VisualEffect>(true);
            bool physical = false;
            bool fire = false;
            bool cold = false;
            bool lightning = false;
            bool light = false;
            bool darkness = false;
            int dominantBaseDamageType = 0;

            GetDamagePresenceFlags(
                damageInfo.DamageInstance,
                ref physical,
                ref fire,
                ref cold,
                ref lightning,
                ref light,
                ref darkness);
            dominantBaseDamageType = GetDominantBaseDamageType(damageInfo.DamageInstance);

            for (int i = 0; i < _spawnedHitEffectsBuffer.Length; i++)
            {
                var visualEffect = _spawnedHitEffectsBuffer[i];
                if (visualEffect == null)
                {
                    continue;
                }

                SetDamageFlag(visualEffect, physicalDamageFlag, physical);
                SetDamageFlag(visualEffect, fireDamageFlag, fire);
                SetDamageFlag(visualEffect, coldDamageFlag, cold);
                SetDamageFlag(visualEffect, lightningDamageFlag, lightning);
                SetDamageFlag(visualEffect, lightDamageFlag, light);
                SetDamageFlag(visualEffect, darknessDamageFlag, darkness);
                SetDamageTypeProperty(visualEffect, dominantBaseDamageTypeProperty, dominantBaseDamageType);
            }

            _spawnedShockwavePlayersBuffer = hitEffectInstance.GetComponentsInChildren<ProceduralShockwavePlayer>(true);
            for (int i = 0; i < _spawnedShockwavePlayersBuffer.Length; i++)
            {
                var shockwavePlayer = _spawnedShockwavePlayersBuffer[i];
                if (shockwavePlayer == null)
                {
                    continue;
                }

                shockwavePlayer.SetDominantBaseDamageType(dominantBaseDamageType);
            }
        }

        private static void GetDamagePresenceFlags(
            DamageInstance damageInstance,
            ref bool physical,
            ref bool fire,
            ref bool cold,
            ref bool lightning,
            ref bool light,
            ref bool darkness)
        {
            physical = GetDamageValue(damageInstance, DamageType.Physical) > 0f;
            fire = GetDamageValue(damageInstance, DamageType.Fire) > 0f;
            cold = GetDamageValue(damageInstance, DamageType.Cold) > 0f;
            lightning = GetDamageValue(damageInstance, DamageType.Lightning) > 0f;
            light = GetDamageValue(damageInstance, DamageType.Light) > 0f;
            darkness = GetDamageValue(damageInstance, DamageType.Darkness) > 0f;
        }

        private static float GetDamageValue(DamageInstance damageInstance, DamageType damageType)
        {
            if (damageInstance?.Damage == null)
            {
                return 0f;
            }

            return damageInstance.Damage.TryGetValue(damageType, out float value) ? value : 0f;
        }

        private static int GetDominantBaseDamageType(DamageInstance damageInstance)
        {
            DamageType dominantDamageType = DamageType.Physical;
            float dominantDamageValue = GetDamageValue(damageInstance, dominantDamageType);

            UpdateDominantBaseDamageType(damageInstance, DamageType.Fire, ref dominantDamageType, ref dominantDamageValue);
            UpdateDominantBaseDamageType(damageInstance, DamageType.Cold, ref dominantDamageType, ref dominantDamageValue);
            UpdateDominantBaseDamageType(damageInstance, DamageType.Lightning, ref dominantDamageType, ref dominantDamageValue);

            if (dominantDamageValue <= 0f)
            {
                return 0;
            }

            return dominantDamageType switch
            {
                DamageType.Physical => 1,
                DamageType.Fire => 2,
                DamageType.Cold => 3,
                DamageType.Lightning => 4,
                _ => 0
            };
        }

        private static void UpdateDominantBaseDamageType(
            DamageInstance damageInstance,
            DamageType damageType,
            ref DamageType dominantDamageType,
            ref float dominantDamageValue)
        {
            float damageValue = GetDamageValue(damageInstance, damageType);
            if (damageValue > dominantDamageValue)
            {
                dominantDamageType = damageType;
                dominantDamageValue = damageValue;
            }
        }

        private static void SetDamageFlag(VisualEffect visualEffect, string propertyName, bool value)
        {
            if (visualEffect == null || string.IsNullOrWhiteSpace(propertyName) || !visualEffect.HasBool(propertyName))
            {
                return;
            }

            visualEffect.SetBool(propertyName, value);
        }

        private static void SetDamageTypeProperty(VisualEffect visualEffect, string propertyName, int value)
        {
            if (visualEffect == null || string.IsNullOrWhiteSpace(propertyName) || !visualEffect.HasInt(propertyName))
            {
                return;
            }

            visualEffect.SetInt(propertyName, value);
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

        private void ResetKnockbackPosition()
        {
            if (knockbackTransform == null)
            {
                return;
            }

            knockbackTransform.localPosition = _baseKnockbackLocalPosition;
        }

        private Vector3 GetKnockbackDirection(DamageInfo damageInfo)
        {
            if (damageInfo?.Owner != null && _ownerTransform != null)
            {
                Vector3 worldDirection = _ownerTransform.position - damageInfo.Owner.transform.position;
                if (knockbackTransform != null && knockbackTransform.parent != null)
                {
                    worldDirection = knockbackTransform.parent.InverseTransformVector(worldDirection);
                }

                Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
                if (direction.sqrMagnitude > 0.0001f)
                {
                    return new Vector3(direction.normalized.x, direction.normalized.y, 0f);
                }
            }

            Vector2 fallbackDirection = combatDirection.sqrMagnitude > 0.0001f
                ? -combatDirection.normalized
                : Vector2.left;
            return new Vector3(fallbackDirection.x, fallbackDirection.y, 0f);
        }

        private void ResetTransformEffects()
        {
            ResetWobbleRotation();
            ResetScale();
            ResetKnockbackPosition();
            ResetVisualColor();
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
