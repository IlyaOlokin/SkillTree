using System;
using DG.Tweening;
using UnityEngine;

namespace MenuTree
{
    [RequireComponent(typeof(Camera))]
    public class MenuTreeCameraController : MonoBehaviour
    {
        [SerializeField] private float moveDuration = 0.6f;
        [SerializeField] private Ease moveEase = Ease.InOutSine;
        [SerializeField] private float zoomDuration = 0.45f;
        [SerializeField] private Ease zoomEase = Ease.InOutSine;
        [SerializeField] private Vector3 focusOffset;
        [SerializeField] private bool ignoreTimeScale = true;

        private Camera _camera;
        private Sequence _focusSequence;

        public bool IsFocusing => IsTweenRunning(_focusSequence);

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            _focusSequence?.Kill();
        }

        public void FocusOn(MenuCameraFocusTarget focusTarget)
        {
            FocusOn(focusTarget, null);
        }

        public void FocusOn(MenuCameraFocusTarget focusTarget, Action onComplete)
        {
            if (focusTarget == null)
                return;

            Vector3 focusPosition = focusTarget.GetFocusPosition(_camera) + focusOffset;
            float? orthographicSize = focusTarget.GetOrthographicSize(_camera);
            FocusOn(focusPosition, orthographicSize, onComplete);
        }

        public void FocusOn(Transform focusTransform)
        {
            FocusOn(focusTransform, null);
        }

        public void FocusOn(Transform focusTransform, Action onComplete)
        {
            if (focusTransform == null)
                return;

            FocusOn(focusTransform.position + focusOffset, null, onComplete);
        }

        public void FocusOn(Vector3 worldPosition, float? orthographicSize = null)
        {
            FocusOn(worldPosition, orthographicSize, null);
        }

        public void FocusOn(Vector3 worldPosition, float? orthographicSize, Action onComplete)
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            _focusSequence?.Kill();

            Vector3 targetPosition = new(worldPosition.x, worldPosition.y, transform.position.z);
            Sequence sequence = DOTween.Sequence()
                .SetUpdate(ignoreTimeScale);

            sequence.Join(
                transform.DOMove(targetPosition, Mathf.Max(0f, moveDuration))
                    .SetEase(moveEase));

            if (_camera.orthographic && orthographicSize.HasValue)
            {
                sequence.Join(
                    DOTween.To(
                            () => _camera.orthographicSize,
                            value => _camera.orthographicSize = value,
                            Mathf.Max(0.01f, orthographicSize.Value),
                            Mathf.Max(0f, zoomDuration))
                        .SetEase(zoomEase));
            }

            if (sequence.Duration(false) <= 0f)
            {
                transform.position = targetPosition;
                if (_camera.orthographic && orthographicSize.HasValue)
                    _camera.orthographicSize = Mathf.Max(0.01f, orthographicSize.Value);

                sequence.Kill();
                onComplete?.Invoke();
                return;
            }

            _focusSequence = sequence
                .OnComplete(() =>
                {
                    _focusSequence = null;
                    onComplete?.Invoke();
                })
                .OnKill(() =>
                {
                    if (_focusSequence == sequence)
                        _focusSequence = null;
                });
        }

        public void SnapTo(MenuCameraFocusTarget focusTarget)
        {
            if (focusTarget == null)
                return;

            if (_camera == null)
                _camera = GetComponent<Camera>();

            _focusSequence?.Kill();

            Vector3 focusPosition = focusTarget.GetFocusPosition(_camera) + focusOffset;
            transform.position = new Vector3(focusPosition.x, focusPosition.y, transform.position.z);

            float? orthographicSize = focusTarget.GetOrthographicSize(_camera);
            if (_camera != null && _camera.orthographic && orthographicSize.HasValue)
                _camera.orthographicSize = Mathf.Max(0.01f, orthographicSize.Value);
        }

        private static bool IsTweenRunning(Tween tween)
        {
            return tween != null && tween.active && tween.IsPlaying();
        }
    }
}
