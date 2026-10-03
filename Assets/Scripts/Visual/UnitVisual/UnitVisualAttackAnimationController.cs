using Battle;
using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Visual
{
    [Serializable]
    public class UnitVisualAttackAnimationController
    {
        [SerializeField] private Transform attackVisualTransform;
        [SerializeField] private Transform weaponSwingTransform;
        [SerializeField] private SpriteRenderer weaponSpriteRenderer;
        [Header("Weapon Sprites")]
        [SerializeField] private Sprite swordAttackSprite;
        [SerializeField] private Sprite hammerAttackSprite;
        [SerializeField] private Sprite staffAttackSprite;
        [Header("Swing")]
        [FormerlySerializedAs("attackMoveDirection")]
        [SerializeField] private Vector2 combatDirection = Vector2.right;
        [SerializeField] private float frontOffset = 0.12f;
        [SerializeField] private float arcHalfAngle = 68f;
        [SerializeField] private bool invertSwingDirection;
        [SerializeField] private bool mirrorSwingWhenAttackingDown = true;
        [SerializeField] private float windupDuration = 0.04f;
        [SerializeField] private float strikeDuration = 0.11f;
        [SerializeField] private float returnDuration = 0.09f;

        private Transform _ownerTransform;
        private GameObject _ownerGameObject;
        private Sequence _attackSequence;
        private Transform _animatedTransform;
        private Vector3 _baseAnimatedLocalPosition;
        private Quaternion _baseAnimatedLocalRotation;
        private WeaponType _currentWeaponType = WeaponType.Unarmed;
        private bool _isInitialized;

        public Vector2 CombatDirection => GetFallbackAttackDirection();

        public void Initialize(Transform ownerTransform, GameObject ownerGameObject)
        {
            if (_isInitialized)
            {
                return;
            }

            _ownerTransform = ownerTransform;
            _ownerGameObject = ownerGameObject;
            AutoAssignWeaponVisuals(ownerTransform);

            _animatedTransform = weaponSwingTransform != null ? weaponSwingTransform : attackVisualTransform;
            if (_animatedTransform == null)
            {
                _animatedTransform = ownerTransform;
            }

            _baseAnimatedLocalPosition = _animatedTransform.localPosition;
            _baseAnimatedLocalRotation = _animatedTransform.localRotation;
            _isInitialized = true;
            ApplyWeaponSprite(_currentWeaponType);
        }

        public void SetWeaponType(WeaponType weaponType)
        {
            _currentWeaponType = weaponType;
            ApplyWeaponSprite(weaponType);
        }

        public void PlayAttackAnimation(ITarget target)
        {
            PlayAttackAnimation(GetAttackDirection(target));
        }

        public void PlayAttackAnimation(Vector2 direction)
        {
            if (!_isInitialized)
            {
                return;
            }

            if (_animatedTransform == null)
            {
                return;
            }

            ApplyWeaponSprite(_currentWeaponType);
            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFallbackAttackDirection();
            float swingSide = invertSwingDirection ? -1f : 1f;
            if (mirrorSwingWhenAttackingDown && direction.y < -0.001f)
            {
                swingSide *= -1f;
            }

            float directionAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float safeArcHalfAngle = Mathf.Max(1f, arcHalfAngle);
            float startAngle = directionAngle - safeArcHalfAngle * swingSide;
            float strikeAngle = directionAngle + safeArcHalfAngle * swingSide;
            Vector3 direction3 = new Vector3(direction.x, direction.y, 0f);
            Vector3 swingCenterPosition = _baseAnimatedLocalPosition + direction3 * frontOffset;

            _attackSequence?.Kill();
            ResetAttackVisualTransform();
            _attackSequence = DOTween.Sequence()
                .Append(_animatedTransform.DOLocalMove(swingCenterPosition, windupDuration).SetEase(Ease.OutQuad))
                .Join(BuildLocalZRotationTween(0f, startAngle, windupDuration, Ease.OutQuad))
                .Append(BuildLocalZRotationTween(startAngle, strikeAngle, strikeDuration, Ease.OutCubic))
                .Append(_animatedTransform.DOLocalMove(_baseAnimatedLocalPosition, returnDuration).SetEase(Ease.OutQuad))
                .Join(BuildLocalZRotationTween(strikeAngle, 0f, returnDuration, Ease.OutQuad))
                .OnComplete(ResetAttackVisualTransform);

            if (_ownerGameObject != null)
            {
                _attackSequence.SetLink(_ownerGameObject);
            }
        }

        public void Dispose()
        {
            _attackSequence?.Kill();
            ResetAttackVisualTransform();
        }

        private Vector2 GetAttackDirection(ITarget target)
        {
            if (target?.UnitObject == null || _ownerTransform == null)
            {
                return GetFallbackAttackDirection();
            }

            Vector3 worldDirection = target.UnitObject.transform.position - _ownerTransform.position;
            if (_animatedTransform != null && _animatedTransform.parent != null)
            {
                worldDirection = _animatedTransform.parent.InverseTransformVector(worldDirection);
            }

            Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFallbackAttackDirection();
        }

        private Vector2 GetFallbackAttackDirection()
        {
            return combatDirection.sqrMagnitude > 0.0001f ? combatDirection.normalized : Vector2.right;
        }

        private void ResetAttackVisualTransform()
        {
            if (_animatedTransform == null || !_isInitialized)
            {
                return;
            }

            _animatedTransform.localPosition = _baseAnimatedLocalPosition;
            _animatedTransform.localRotation = _baseAnimatedLocalRotation;
        }

        private Tween BuildLocalZRotationTween(float fromAngle, float toAngle, float duration, Ease ease)
        {
            float angle = fromAngle;
            return DOTween.To(
                    () => angle,
                    value =>
                    {
                        angle = value;
                        _animatedTransform.localRotation = _baseAnimatedLocalRotation * Quaternion.Euler(0f, 0f, angle);
                    },
                    toAngle,
                    duration)
                .SetEase(ease);
        }

        private void AutoAssignWeaponVisuals(Transform ownerTransform)
        {
            if (ownerTransform == null)
            {
                return;
            }

            if (weaponSpriteRenderer == null)
            {
                SpriteRenderer[] spriteRenderers = ownerTransform.GetComponentsInChildren<SpriteRenderer>(true);
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    if (spriteRenderers[i].name == "Weapon")
                    {
                        weaponSpriteRenderer = spriteRenderers[i];
                        break;
                    }
                }
            }

            if (weaponSwingTransform != null || weaponSpriteRenderer == null)
            {
                return;
            }

            Transform weaponTransform = weaponSpriteRenderer.transform;
            weaponSwingTransform = weaponTransform.parent != null && weaponTransform.parent.name == "WeaponParent"
                ? weaponTransform.parent
                : weaponTransform;
        }

        private void ApplyWeaponSprite(WeaponType weaponType)
        {
            if (weaponSpriteRenderer == null)
            {
                return;
            }

            Sprite sprite = GetWeaponSprite(weaponType);
            if (sprite != null)
            {
                weaponSpriteRenderer.sprite = sprite;
            }
        }

        private Sprite GetWeaponSprite(WeaponType weaponType)
        {
            return weaponType switch
            {
                WeaponType.Sword => swordAttackSprite,
                WeaponType.Hammer => hammerAttackSprite,
                WeaponType.FireStaff or WeaponType.ColdStaff or WeaponType.LightningStaff => staffAttackSprite,
                _ => null
            };
        }
    }
}
