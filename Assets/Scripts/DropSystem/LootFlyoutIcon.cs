using System;
using DG.Tweening;
using InventorySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DropSystem
{
    public sealed class LootFlyoutIcon : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private CanvasGroup canvasGroup;

        private RectTransform _rectTransform;
        private Sequence _sequence;

        public RectTransform RectTransform
        {
            get
            {
                if (_rectTransform == null)
                    _rectTransform = transform as RectTransform;

                return _rectTransform;
            }
        }

        private void Awake()
        {
            EnsureReferences();
        }

        private void OnDestroy()
        {
            _sequence?.Kill();
        }

        public void Initialize(InventoryItem item)
        {
            EnsureReferences();
            EnsureVisibleSize(new Vector2(48f, 48f));

            Initialize(item?.Icon, item != null && item.StackCount > 1 ? item.StackCount.ToString() : string.Empty);
        }

        public void Initialize(Sprite icon, string amountLabel)
        {
            EnsureReferences();
            EnsureVisibleSize(new Vector2(48f, 48f));

            if (iconImage != null)
            {
                iconImage.enabled = icon != null;
                iconImage.sprite = icon;
                iconImage.raycastTarget = false;
            }

            if (amountText != null)
            {
                bool shouldShowAmount = !string.IsNullOrWhiteSpace(amountLabel);
                amountText.gameObject.SetActive(shouldShowAmount);
                amountText.text = shouldShowAmount ? amountLabel : string.Empty;
                amountText.raycastTarget = false;
            }

            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

            if (RectTransform != null)
                RectTransform.localScale = Vector3.one;
        }

        public void Configure(Image image, TMP_Text amount, CanvasGroup group)
        {
            iconImage = image;
            amountText = amount;
            canvasGroup = group;
            _rectTransform = transform as RectTransform;
        }

        public void EnsureVisibleSize(Vector2 fallbackSize)
        {
            RectTransform rectTransform = RectTransform;
            if (rectTransform == null)
                return;

            Vector2 currentSize = rectTransform.rect.size;
            bool hasCollapsedWidth = currentSize.x <= 0.01f && Mathf.Abs(rectTransform.sizeDelta.x) <= 0.01f;
            bool hasCollapsedHeight = currentSize.y <= 0.01f && Mathf.Abs(rectTransform.sizeDelta.y) <= 0.01f;
            if (!hasCollapsedWidth && !hasCollapsedHeight)
                return;

            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = new Vector2(
                Mathf.Max(1f, fallbackSize.x),
                Mathf.Max(1f, fallbackSize.y));
        }

        public void Play(
            Vector2 startPosition,
            Vector2 targetPosition,
            float delay,
            float duration,
            float arcHeight,
            float endScale,
            Ease ease,
            bool useUnscaledTime,
            Action onComplete,
            Action onFlyStarted = null)
        {
            EnsureReferences();
            _sequence?.Kill();

            RectTransform rectTransform = RectTransform;
            if (rectTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            rectTransform.anchoredPosition = startPosition;
            rectTransform.localScale = Vector3.one;

            Vector2 control = (startPosition + targetPosition) * 0.5f + Vector2.up * arcHeight;
            _sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
            if (delay > 0f)
                _sequence.AppendInterval(delay);

            _sequence.AppendCallback(() => onFlyStarted?.Invoke());

            _sequence.Append(DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, duration), t =>
            {
                float oneMinusT = 1f - t;
                rectTransform.anchoredPosition =
                    oneMinusT * oneMinusT * startPosition +
                    2f * oneMinusT * t * control +
                    t * t * targetPosition;
            }).SetEase(ease));

            _sequence.Join(rectTransform.DOScale(Mathf.Max(0f, endScale), Mathf.Max(0.01f, duration)).SetEase(Ease.InQuad));

            if (canvasGroup != null)
                _sequence.Join(canvasGroup.DOFade(0f, Mathf.Max(0.01f, duration * 0.45f)).SetDelay(Mathf.Max(0f, duration * 0.55f)));

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                onComplete?.Invoke();
            });
        }

        public void PlayDropThenFly(
            Vector2 spawnPosition,
            Vector2 settledPosition,
            Vector2 targetPosition,
            float dropDuration,
            Ease dropEase,
            float delay,
            float flyDuration,
            float arcHeight,
            float endScale,
            Ease flyEase,
            bool useUnscaledTime,
            Action onComplete,
            Action onFlyStarted = null)
        {
            EnsureReferences();
            _sequence?.Kill();

            RectTransform rectTransform = RectTransform;
            if (rectTransform == null)
            {
                onComplete?.Invoke();
                return;
            }

            rectTransform.anchoredPosition = spawnPosition;
            rectTransform.localScale = Vector3.one;

            _sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);

            if (dropDuration > 0f)
            {
                _sequence.Append(DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, dropDuration), t =>
                {
                    rectTransform.anchoredPosition = Vector2.LerpUnclamped(spawnPosition, settledPosition, t);
                }).SetEase(dropEase));
            }
            else
            {
                rectTransform.anchoredPosition = settledPosition;
            }

            if (delay > 0f)
                _sequence.AppendInterval(delay);

            _sequence.AppendCallback(() => onFlyStarted?.Invoke());

            Vector2 control = (settledPosition + targetPosition) * 0.5f + Vector2.up * arcHeight;
            _sequence.Append(DOVirtual.Float(0f, 1f, Mathf.Max(0.01f, flyDuration), t =>
            {
                float oneMinusT = 1f - t;
                rectTransform.anchoredPosition =
                    oneMinusT * oneMinusT * settledPosition +
                    2f * oneMinusT * t * control +
                    t * t * targetPosition;
            }).SetEase(flyEase));

            _sequence.Join(rectTransform.DOScale(Mathf.Max(0f, endScale), Mathf.Max(0.01f, flyDuration)).SetEase(Ease.InQuad));

            if (canvasGroup != null)
                _sequence.Join(canvasGroup.DOFade(0f, Mathf.Max(0.01f, flyDuration * 0.45f)).SetDelay(Mathf.Max(0f, flyDuration * 0.55f)));

            _sequence.OnComplete(() =>
            {
                _sequence = null;
                onComplete?.Invoke();
            });
        }

        private void EnsureReferences()
        {
            if (_rectTransform == null)
                _rectTransform = transform as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
