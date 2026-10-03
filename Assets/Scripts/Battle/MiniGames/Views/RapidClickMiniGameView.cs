using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    public sealed class RapidClickMiniGameView : MonoBehaviour, IBattleMiniGameView
    {
        [SerializeField] private Button hitButton;
        [SerializeField] private GameObject timerRoot;
        [SerializeField] private Image timerFillImage;
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private RectTransform pressBounceTarget;
        [SerializeField] private Graphic resultColorGraphic;

        [Header("Gameplay")]
        [SerializeField, Min(1)] private int requiredPresses = 5;
        [SerializeField, Min(0.1f)] private float timeLimitSeconds = 2f;
        [SerializeField] private string counterFormat = "{0}";

        [Header("Colors")]
        [SerializeField] private Color idleColor = Color.white;
        [SerializeField] private Color successColor = new Color(0.35f, 1f, 0.45f);
        [SerializeField] private Color failColor = new Color(1f, 0.35f, 0.35f);

        [Header("Press Bounce")]
        [SerializeField, Min(1f)] private float pressBounceScale = 1.12f;
        [SerializeField, Min(0f)] private float pressBounceInDuration = 0.06f;
        [SerializeField, Min(0f)] private float pressBounceOutDuration = 0.08f;
        [SerializeField] private Ease pressBounceInEase = Ease.OutQuad;
        [SerializeField] private Ease pressBounceOutEase = Ease.InQuad;

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
        private int _remainingPresses;
        private bool _completed;
        private Vector3 _pressBounceInitialScale;
        private Vector3 _successPulseInitialScale;
        private Vector2 _failMoveInitialPosition;
        private Tween _pressBounceTween;
        private Sequence _resolveSequence;

        public event Action<BattleMiniGameResult> Completed;

        private void Awake()
        {
            if (hitButton == null)
            {
                hitButton = GetComponentInChildren<Button>(true);
            }

            if (timerRoot == null && timerFillImage != null)
            {
                timerRoot = timerFillImage.gameObject;
            }

            RectTransform ownRect = transform as RectTransform;
            if (pressBounceTarget == null)
            {
                pressBounceTarget = ownRect;
            }

            if (successPulseTarget == null)
            {
                successPulseTarget = pressBounceTarget != null ? pressBounceTarget : ownRect;
            }

            if (failMoveTarget == null)
            {
                failMoveTarget = ownRect;
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
                hitButton.onClick.AddListener(HandlePress);
            }
        }

        private void OnDisable()
        {
            if (hitButton != null)
            {
                hitButton.onClick.RemoveListener(HandlePress);
            }

            MiniGameTweenUtility.Kill(ref _pressBounceTween);
            MiniGameTweenUtility.Kill(ref _resolveSequence);
        }

        public void StartGame(BattleMiniGameRunContext context)
        {
            MiniGameTweenUtility.Kill(ref _pressBounceTween);
            MiniGameTweenUtility.Kill(ref _resolveSequence);

            _context = context;
            _elapsed = 0f;
            _remainingPresses = Mathf.Max(1, requiredPresses);
            _completed = false;

            CacheInitialState();
            ResetResultIcons();
            SetTimerActive(true);
            MiniGameTweenUtility.SetAlpha(fadeGraphics, 1f);
            SetResultColor(idleColor);
            UpdateCounter();
            UpdateTimerFill();
        }

        private void Update()
        {
            if (_context == null || _completed)
            {
                return;
            }

            _elapsed += Time.deltaTime;
            UpdateTimerFill();

            if (_elapsed >= timeLimitSeconds)
            {
                Resolve(BattleMiniGameResult.Fail());
            }
        }

        private void HandlePress()
        {
            if (_context == null || _completed)
            {
                return;
            }

            _remainingPresses = Mathf.Max(0, _remainingPresses - 1);
            UpdateCounter();
            PlayPressBounce();

            if (_remainingPresses <= 0)
            {
                float score01 = Mathf.Clamp01(1f - _elapsed / Mathf.Max(0.001f, timeLimitSeconds));
                Resolve(BattleMiniGameResult.Success(score01));
            }
        }

        private void UpdateTimerFill()
        {
            if (timerFillImage == null)
            {
                return;
            }

            timerFillImage.fillAmount = Mathf.Clamp01(1f - _elapsed / Mathf.Max(0.001f, timeLimitSeconds));
        }

        private void UpdateCounter()
        {
            if (counterText == null)
            {
                return;
            }

            try
            {
                counterText.text = string.Format(counterFormat, _remainingPresses, requiredPresses);
            }
            catch (FormatException)
            {
                counterText.text = _remainingPresses.ToString();
            }
        }

        private void PlayPressBounce()
        {
            if (pressBounceTarget == null)
            {
                return;
            }

            MiniGameTweenUtility.Kill(ref _pressBounceTween);
            pressBounceTarget.localScale = _pressBounceInitialScale;

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTweens);
            sequence.Append(pressBounceTarget
                .DOScale(_pressBounceInitialScale * pressBounceScale, pressBounceInDuration)
                .SetEase(pressBounceInEase));
            sequence.Append(pressBounceTarget
                .DOScale(_pressBounceInitialScale, pressBounceOutDuration)
                .SetEase(pressBounceOutEase));
            _pressBounceTween = sequence;
        }

        private void Resolve(BattleMiniGameResult result)
        {
            if (_completed)
            {
                return;
            }

            _completed = true;

            MiniGameTweenUtility.Kill(ref _pressBounceTween);
            _context?.Complete(result);
            SetResultColor(result.IsSuccess ? successColor : failColor);
            SetTimerActive(false);
            PlayResolveAnimation(result);
        }

        private void Complete(BattleMiniGameResult result)
        {
            _context = null;
            Completed?.Invoke(result);
        }

        private void CacheInitialState()
        {
            if (pressBounceTarget != null)
            {
                _pressBounceInitialScale = pressBounceTarget.localScale;
            }

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

            if (pressBounceTarget != null)
            {
                pressBounceTarget.localScale = _pressBounceInitialScale;
            }

            if (successPulseTarget != null)
            {
                successPulseTarget.localScale = _successPulseInitialScale;
            }

            if (failMoveTarget != null)
            {
                failMoveTarget.anchoredPosition = _failMoveInitialPosition;
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

        private void SetResultColor(Color color)
        {
            if (resultColorGraphic != null)
            {
                resultColorGraphic.color = color;
            }
        }

        private void SetTimerActive(bool active)
        {
            if (timerRoot != null)
            {
                timerRoot.SetActive(active);
            }
        }

        private void PlayResolveAnimation(BattleMiniGameResult result)
        {
            MiniGameTweenUtility.Kill(ref _resolveSequence);

            _resolveSequence = DOTween.Sequence().SetUpdate(useUnscaledTweens);
            _resolveSequence.Join(MiniGameTweenUtility.FadeTo(fadeGraphics, resolvedAlpha, resolvedFadeDuration));

            if (result.IsSuccess)
            {
                _resolveSequence.Join(MiniGameTweenUtility.BuildPulseTween(
                    successPulseTarget,
                    _successPulseInitialScale,
                    successPulseScale,
                    successPulseInDuration,
                    successPulseOutDuration,
                    successPulseCount,
                    successPulseInEase,
                    successPulseOutEase,
                    useUnscaledTweens));
            }
            else
            {
                _resolveSequence.Join(MiniGameTweenUtility.BuildMoveTween(
                    failMoveTarget,
                    _failMoveInitialPosition + failMoveOffset,
                    failMoveDuration,
                    failMoveEase,
                    useUnscaledTweens));
            }

            _resolveSequence.Insert(0f, BuildResultIconTween(result.IsSuccess));
            _resolveSequence.OnComplete(() =>
            {
                _resolveSequence = null;
                Complete(result);
            });
        }

        private Tween BuildResultIconTween(bool success)
        {
            GameObject iconRoot = success ? successIconRoot : failIconRoot;
            RectTransform iconTransform = success ? successIconTransform : failIconTransform;
            return MiniGameTweenUtility.BuildResultIconTween(
                iconRoot,
                iconTransform,
                resultIconShowDuration,
                resultIconHoldDuration,
                resultIconHideDuration,
                resultIconShowEase,
                resultIconHideEase,
                useUnscaledTweens);
        }
    }
}
