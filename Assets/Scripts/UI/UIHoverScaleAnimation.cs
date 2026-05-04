using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UIHoverScaleAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform animatedTarget;
        [SerializeField] [Min(0f)] private float hoverScaleMultiplier = 1.06f;
        [SerializeField] [Min(0f)] private float hoverEnterDuration = 0.12f;
        [SerializeField] [Min(0f)] private float hoverExitDuration = 0.1f;
        [SerializeField] private Ease hoverEnterEase = Ease.OutQuad;
        [SerializeField] private Ease hoverExitEase = Ease.OutQuad;

        private Vector3 _baseScale = Vector3.one;
        private Tween _hoverTween;

        private void Awake()
        {
            if (animatedTarget == null)
                animatedTarget = transform as RectTransform;

            if (animatedTarget != null)
                _baseScale = animatedTarget.localScale;
        }

        private void OnDestroy()
        {
            _hoverTween?.Kill();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            PlayHoverTween(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            PlayHoverTween(false);
        }

        private void PlayHoverTween(bool hovered)
        {
            if (animatedTarget == null)
                return;

            _hoverTween?.Kill();

            float duration = hovered ? hoverEnterDuration : hoverExitDuration;
            Vector3 targetScale = hovered
                ? _baseScale * Mathf.Max(0f, hoverScaleMultiplier)
                : _baseScale;

            if (duration <= 0f)
            {
                animatedTarget.localScale = targetScale;
                return;
            }

            _hoverTween = animatedTarget
                .DOScale(targetScale, duration)
                .SetEase(hovered ? hoverEnterEase : hoverExitEase)
                .SetUpdate(true);
        }
    }
}
