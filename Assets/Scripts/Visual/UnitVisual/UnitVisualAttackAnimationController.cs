using Battle;
using DG.Tweening;
using System;
using UnityEngine;

namespace Visual
{
    [Serializable]
    public class UnitVisualAttackAnimationController
    {
        [SerializeField] private Transform attackVisualTransform;
        [SerializeField] private Vector2 attackMoveDirection = Vector2.right;
        [SerializeField] private float windupDistance = 0.08f;
        [SerializeField] private float dashDistance = 0.22f;
        [SerializeField] private Vector2 strikeRotationZRange = new Vector2(18f, 28f);
        [SerializeField] private float windupRotationZ = 10f;
        [SerializeField] private float windupDuration = 0.05f;
        [SerializeField] private float strikeDuration = 0.08f;
        [SerializeField] private float returnDuration = 0.12f;

        private Transform _ownerTransform;
        private GameObject _ownerGameObject;
        private Sequence _attackSequence;
        private Vector3 _baseAttackLocalPosition;
        private Quaternion _baseAttackLocalRotation;
        private bool _isInitialized;

        public void Initialize(Transform ownerTransform, GameObject ownerGameObject)
        {
            if (_isInitialized)
            {
                return;
            }

            _ownerTransform = ownerTransform;
            _ownerGameObject = ownerGameObject;

            if (attackVisualTransform == null)
            {
                attackVisualTransform = ownerTransform;
            }

            _baseAttackLocalPosition = attackVisualTransform.localPosition;
            _baseAttackLocalRotation = attackVisualTransform.localRotation;
            _isInitialized = true;
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

            if (attackVisualTransform == null)
            {
                return;
            }

            direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFallbackAttackDirection();
            float rotationSide = UnityEngine.Random.value < 0.5f ? -1f : 1f;
            float minStrikeRotationZ = Mathf.Min(strikeRotationZRange.x, strikeRotationZRange.y);
            float maxStrikeRotationZ = Mathf.Max(strikeRotationZRange.x, strikeRotationZRange.y);
            float strikeRotationZ = UnityEngine.Random.Range(minStrikeRotationZ, maxStrikeRotationZ) * rotationSide;
            float windupRotation = -windupRotationZ * rotationSide;

            Vector3 direction3 = new Vector3(direction.x, direction.y, 0f);
            Vector3 windupPosition = _baseAttackLocalPosition - direction3 * windupDistance;
            Vector3 strikePosition = _baseAttackLocalPosition + direction3 * dashDistance;
            Vector3 windupRotationEuler = _baseAttackLocalRotation.eulerAngles + new Vector3(0f, 0f, windupRotation);
            Vector3 strikeRotationEuler = _baseAttackLocalRotation.eulerAngles + new Vector3(0f, 0f, strikeRotationZ);

            _attackSequence?.Kill();
            _attackSequence = DOTween.Sequence()
                .Append(attackVisualTransform.DOLocalMove(windupPosition, windupDuration).SetEase(Ease.OutQuad))
                .Join(attackVisualTransform.DOLocalRotate(windupRotationEuler, windupDuration).SetEase(Ease.OutQuad))
                .Append(attackVisualTransform.DOLocalMove(strikePosition, strikeDuration).SetEase(Ease.OutCubic))
                .Join(attackVisualTransform.DOLocalRotate(strikeRotationEuler, strikeDuration).SetEase(Ease.OutCubic))
                .Append(attackVisualTransform.DOLocalMove(_baseAttackLocalPosition, returnDuration).SetEase(Ease.OutQuad))
                .Join(attackVisualTransform.DOLocalRotate(_baseAttackLocalRotation.eulerAngles, returnDuration).SetEase(Ease.OutQuad))
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
            if (attackVisualTransform != null && attackVisualTransform.parent != null)
            {
                worldDirection = attackVisualTransform.parent.InverseTransformVector(worldDirection);
            }

            Vector2 direction = new Vector2(worldDirection.x, worldDirection.y);
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : GetFallbackAttackDirection();
        }

        private Vector2 GetFallbackAttackDirection()
        {
            return attackMoveDirection.sqrMagnitude > 0.0001f ? attackMoveDirection.normalized : Vector2.right;
        }

        private void ResetAttackVisualTransform()
        {
            if (attackVisualTransform == null || !_isInitialized)
            {
                return;
            }

            attackVisualTransform.localPosition = _baseAttackLocalPosition;
            attackVisualTransform.localRotation = _baseAttackLocalRotation;
        }
    }
}
