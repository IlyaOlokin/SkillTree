using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    public sealed class TimingClickMiniGameView : MonoBehaviour, IBattleMiniGameView, IPointerClickHandler
    {
        [SerializeField] private Button hitButton;
        [SerializeField] private RectTransform targetRing;
        [SerializeField] private RectTransform approachRing;
        [SerializeField] private Image targetRingImage;
        [SerializeField] private Image approachRingImage;

        [Header("Colors")]
        [SerializeField] private Color targetIdleColor = Color.white;
        [SerializeField] private Color targetSuccessColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color targetFailColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField] private Color approachIdleColor = new Color(0.35f, 0.7f, 1f);
        [SerializeField] private Color approachSuccessWindowColor = new Color(1f, 0.95f, 0.35f);
        [SerializeField, Min(0f)] private float approachWindowColorDuration = 0.08f;

        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float perfectTimeSeconds = 1f;
        [SerializeField, Min(0.01f)] private float successWindowBeforeSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float successWindowAfterSeconds = 0.18f;
        [SerializeField, Min(0.01f)] private float perfectWindowSeconds = 0.06f;

        [Header("Approach Ring")]
        [SerializeField, Min(1f)] private float approachStartScale = 3f;
        [SerializeField, Min(0.1f)] private float targetScale = 1f;
        [SerializeField, Min(0.1f)] private float approachFailScale = 0.65f;
        [SerializeField, Min(0f)] private float approachDisappearDuration = 0.08f;

        [Header("Fade")]
        [SerializeField] private List<Graphic> fadeGraphics = new List<Graphic>();
        [SerializeField, Range(0f, 1f)] private float resolvedAlpha = 0f;
        [SerializeField, Min(0f)] private float resolvedFadeDuration = 0.25f;

        [Header("Success Animation")]
        [SerializeField] private RectTransform successPulseTarget;
        [SerializeField, Min(1f)] private float successPulseScale = 1.18f;
        [SerializeField, Min(0f)] private float successPulseInDuration = 0.12f;
        [SerializeField, Min(0f)] private float successPulseOutDuration = 0.12f;
        [SerializeField, Min(1)] private int successPulseCount = 2;
        [SerializeField] private Ease successPulseInEase = Ease.OutQuad;
        [SerializeField] private Ease successPulseOutEase = Ease.InOutQuad;

        [Header("Fail Animation")]
        [SerializeField] private RectTransform failMoveTarget;
        [SerializeField] private Vector2 failMoveOffset = new Vector2(0f, -40f);
        [SerializeField, Min(0f)] private float failMoveDuration = 0.25f;
        [SerializeField] private Ease failMoveEase = Ease.InQuad;

        [Header("Result Icon")]
        [SerializeField] private GameObject successIconRoot;
        [SerializeField] private GameObject failIconRoot;
        [SerializeField] private RectTransform successIconTransform;
        [SerializeField] private RectTransform failIconTransform;
        [SerializeField, Min(0f)] private float resultIconShowDuration = 0.16f;
        [SerializeField, Min(0f)] private float resultIconHoldDuration = 0.25f;
        [SerializeField, Min(0f)] private float resultIconHideDuration = 0.14f;
        [SerializeField] private Ease resultIconShowEase = Ease.OutBack;
        [SerializeField] private Ease resultIconHideEase = Ease.InBack;

        [Header("Tween")]
        [SerializeField] private bool useUnscaledTweens;

        private BattleMiniGameRunContext _context;
        private float _elapsed;
        private bool _completed;
        private bool _approachInSuccessWindow;
        private Vector3 _successPulseInitialScale;
        private Vector2 _failMoveInitialPosition;
        private Sequence _resolveSequence;
        private Tween _approachColorTween;

        public event Action<BattleMiniGameResult> Completed;

        private void Awake()
        {
            if (hitButton == null)
            {
                hitButton = GetComponentInChildren<Button>(true);
            }

            if (targetRing == null)
            {
                targetRing = transform as RectTransform;
            }

            if (successPulseTarget == null)
            {
                successPulseTarget = targetRing;
            }

            if (failMoveTarget == null)
            {
                failMoveTarget = transform as RectTransform;
            }

            if (successIconTransform == null && successIconRoot != null)
            {
                successIconTransform = successIconRoot.GetComponent<RectTransform>();
            }

            if (failIconTransform == null && failIconRoot != null)
            {
                failIconTransform = failIconRoot.GetComponent<RectTransform>();
            }
        }

        private void OnEnable()
        {
            if (hitButton != null)
            {
                hitButton.onClick.AddListener(HandleHit);
            }
        }

        private void OnDisable()
        {
            if (hitButton != null)
            {
                hitButton.onClick.RemoveListener(HandleHit);
            }

            MiniGameTweenUtility.Kill(ref _resolveSequence);
            MiniGameTweenUtility.Kill(ref _approachColorTween);
        }

        public void StartGame(BattleMiniGameRunContext context)
        {
            MiniGameTweenUtility.Kill(ref _resolveSequence);
            MiniGameTweenUtility.Kill(ref _approachColorTween);

            _context = context;
            _elapsed = 0f;
            _completed = false;
            _approachInSuccessWindow = false;

            CacheInitialState();
            ResetResultIcons();
            MiniGameTweenUtility.SetAlpha(fadeGraphics, 1f);
            SetTargetColor(targetIdleColor);
            SetApproachColor(approachIdleColor);
            UpdateApproachRing();
        }

        private void Update()
        {
            if (_context == null || _completed)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            UpdateApproachRing();
            UpdateApproachWindowColor();

            if (_elapsed > GetSuccessEndTime())
            {
                Resolve(BattleMiniGameResult.Fail());
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            HandleHit();
        }

        public void HandleHit()
        {
            if (_context == null || _completed)
            {
                return;
            }

            float error = Mathf.Abs(_elapsed - perfectTimeSeconds);
            if (_elapsed < GetSuccessStartTime() || _elapsed > GetSuccessEndTime())
            {
                Resolve(BattleMiniGameResult.Fail());
                return;
            }

            float score01 = CalculateScore(error);
            Resolve(BattleMiniGameResult.Success(score01));
        }

        private float CalculateScore(float error)
        {
            if (error <= perfectWindowSeconds)
            {
                return 1f;
            }

            float successWindow = _elapsed <= perfectTimeSeconds
                ? successWindowBeforeSeconds
                : successWindowAfterSeconds;
            float lenientRange = Mathf.Max(0.001f, successWindow - perfectWindowSeconds);
            return Mathf.Clamp01(1f - (error - perfectWindowSeconds) / lenientRange);
        }

        private void UpdateApproachRing()
        {
            if (approachRing == null)
            {
                return;
            }

            float scale;
            if (_elapsed <= perfectTimeSeconds)
            {
                float progress01 = Mathf.Clamp01(_elapsed / Mathf.Max(0.001f, perfectTimeSeconds));
                scale = Mathf.Lerp(approachStartScale, targetScale, progress01);
            }
            else
            {
                float progress01 = Mathf.Clamp01(
                    (_elapsed - perfectTimeSeconds) / Mathf.Max(0.001f, successWindowAfterSeconds));
                scale = Mathf.Lerp(targetScale, approachFailScale, progress01);
            }

            approachRing.localScale = Vector3.one * scale;
        }

        private void UpdateApproachWindowColor()
        {
            if (_approachInSuccessWindow || approachRingImage == null)
            {
                return;
            }

            if (_elapsed < GetSuccessStartTime() || _elapsed > GetSuccessEndTime())
            {
                return;
            }

            _approachInSuccessWindow = true;
            MiniGameTweenUtility.Kill(ref _approachColorTween);
            _approachColorTween = approachRingImage
                .DOColor(approachSuccessWindowColor, approachWindowColorDuration)
                .SetUpdate(useUnscaledTweens);
        }

        private float GetSuccessStartTime()
        {
            return Mathf.Max(0f, perfectTimeSeconds - successWindowBeforeSeconds);
        }

        private float GetSuccessEndTime()
        {
            return perfectTimeSeconds + successWindowAfterSeconds;
        }

        private void SetTargetColor(Color color)
        {
            if (targetRingImage != null)
            {
                targetRingImage.color = color;
            }
        }

        private void SetApproachColor(Color color)
        {
            if (approachRingImage != null)
            {
                approachRingImage.color = color;
            }
        }

        private void Resolve(BattleMiniGameResult result)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;

            _context?.Complete(result);
            SetTargetColor(result.IsSuccess ? targetSuccessColor : targetFailColor);
            PlayResolveAnimation(result);
        }

        private void Complete(BattleMiniGameResult result)
        {
            _context = null;
            Completed?.Invoke(result);
        }

        private void CacheInitialState()
        {
            if (successPulseTarget != null)
            {
                _successPulseInitialScale = successPulseTarget.localScale;
            }

            if (failMoveTarget != null)
            {
                _failMoveInitialPosition = failMoveTarget.anchoredPosition;
            }
        }

        private void ResetResultIcons()
        {
            ResetResultIcon(successIconRoot, successIconTransform);
            ResetResultIcon(failIconRoot, failIconTransform);

            if (successPulseTarget != null)
            {
                successPulseTarget.localScale = _successPulseInitialScale;
            }

            if (failMoveTarget != null)
            {
                failMoveTarget.anchoredPosition = _failMoveInitialPosition;
            }

            if (approachRing != null)
            {
                approachRing.gameObject.SetActive(true);
            }
        }

        private void ResetResultIcon(GameObject iconRoot, RectTransform iconTransform)
        {
            if (iconRoot != null)
            {
                iconRoot.SetActive(false);
            }

            if (iconTransform != null)
            {
                iconTransform.localScale = Vector3.zero;
            }
        }

        private void PlayResolveAnimation(BattleMiniGameResult result)
        {
            MiniGameTweenUtility.Kill(ref _resolveSequence);
            MiniGameTweenUtility.Kill(ref _approachColorTween);

            _resolveSequence = DOTween.Sequence().SetUpdate(useUnscaledTweens);
            _resolveSequence.Join(BuildApproachDisappearTween());
            _resolveSequence.Join(MiniGameTweenUtility.FadeTo(fadeGraphics, resolvedAlpha, resolvedFadeDuration));

            if (result.IsSuccess)
            {
                _resolveSequence.Join(BuildSuccessPulseTween());
            }
            else
            {
                _resolveSequence.Join(BuildFailMoveTween());
            }

            _resolveSequence.Insert(0f, BuildResultIconTween(result.IsSuccess));
            _resolveSequence.OnComplete(() =>
            {
                _resolveSequence = null;
                Complete(result);
            });
        }

        private Tween BuildApproachDisappearTween()
        {
            if (approachRingImage == null)
            {
                if (approachRing != null)
                {
                    approachRing.gameObject.SetActive(false);
                }

                return DOVirtual.DelayedCall(0f, () => { }, false);
            }

            return approachRingImage
                .DOFade(0f, approachDisappearDuration)
                .SetUpdate(useUnscaledTweens)
                .OnComplete(() =>
                {
                    if (approachRing != null)
                    {
                        approachRing.gameObject.SetActive(false);
                    }
                });
        }

        private Tween BuildSuccessPulseTween()
        {
            if (successPulseTarget == null)
            {
                return DOVirtual.DelayedCall(0f, () => { }, false);
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTweens);
            for (int i = 0; i < successPulseCount; i++)
            {
                sequence.Append(successPulseTarget
                    .DOScale(_successPulseInitialScale * successPulseScale, successPulseInDuration)
                    .SetEase(successPulseInEase));
                sequence.Append(successPulseTarget
                    .DOScale(_successPulseInitialScale, successPulseOutDuration)
                    .SetEase(successPulseOutEase));
            }

            return sequence;
        }

        private Tween BuildFailMoveTween()
        {
            if (failMoveTarget == null)
            {
                return DOVirtual.DelayedCall(0f, () => { }, false);
            }

            return failMoveTarget
                .DOAnchorPos(_failMoveInitialPosition + failMoveOffset, failMoveDuration)
                .SetEase(failMoveEase)
                .SetUpdate(useUnscaledTweens);
        }

        private Tween BuildResultIconTween(bool success)
        {
            GameObject iconRoot = success ? successIconRoot : failIconRoot;
            RectTransform iconTransform = success ? successIconTransform : failIconTransform;
            if (iconRoot == null || iconTransform == null)
            {
                return DOVirtual.DelayedCall(0f, () => { }, false);
            }

            iconRoot.SetActive(true);
            iconTransform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTweens);
            sequence.Append(iconTransform
                .DOScale(Vector3.one, resultIconShowDuration)
                .SetEase(resultIconShowEase));
            sequence.AppendInterval(resultIconHoldDuration);
            sequence.Append(iconTransform
                .DOScale(Vector3.zero, resultIconHideDuration)
                .SetEase(resultIconHideEase));
            sequence.OnComplete(() => iconRoot.SetActive(false));
            return sequence;
        }
    }
}
