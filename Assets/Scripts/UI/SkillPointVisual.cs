using System;
using Battle;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class SkillPointVisual : MonoBehaviour
{
    [Inject] private UnitLevel _unitLevel;
    
    [SerializeField] private TMP_Text skillPointCount;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color activeColor;

    [Header("Tween Settings")]
    [SerializeField] private float colorTweenDuration = 0.25f;
    [SerializeField] private float pulseScaleAmount = 1.15f;
    [SerializeField] private float pulseDuration = 0.2f;
    [SerializeField] private float idlePulseScaleAmount = 1.05f;
    [SerializeField] private float idlePulseDuration = 0.8f;
    [SerializeField] private float returnToOneDuration = 0.25f;
    
    private Tween _idlePulseTween;
    private Tween _colorTween;
    private Tween _returnTween;
    private Sequence _changePulseSequence;

    private int _currentCount;

    private void Start()
    {
        _unitLevel.OnSkillPointsChanged += UpdateCount;

        UpdateCount(_unitLevel.SkillPoints);
    }

    private void OnDestroy()
    {
        if (_unitLevel != null)
            _unitLevel.OnSkillPointsChanged -= UpdateCount;
    }

    private void UpdateCount(int count)
    {
        skillPointCount.text = count.ToString();

        bool isActive = count > 0;
        bool becameInactive = _currentCount > 0 && count == 0;

        _currentCount = count;

        UpdateColor(count);

        if (becameInactive)
        {
            StopIdlePulseSmooth();
            return;
        }

        PlayChangePulse(() =>
        {
            if (isActive)
                StartIdlePulse();
        });
    }

    private void UpdateColor(int count)
    {
        _colorTween?.Kill();
        Color target = count > 0 ? activeColor : defaultColor;
        _colorTween = backgroundImage.DOColor(target, colorTweenDuration);
    }

    private void PlayChangePulse(Action onComplete)
    {
        _idlePulseTween?.Kill();
        _returnTween?.Kill();
        _changePulseSequence?.Kill();

        backgroundImage.transform.localScale = Vector3.one;

        _changePulseSequence = DOTween.Sequence();

        _changePulseSequence
            .Append(backgroundImage.transform
                .DOScale(pulseScaleAmount, pulseDuration)
                .SetEase(Ease.OutQuad))
            .Append(backgroundImage.transform
                .DOScale(1f, pulseDuration)
                .SetEase(Ease.OutBack))
            .OnComplete(() => onComplete?.Invoke());
    }

    private void StartIdlePulse()
    {
        _idlePulseTween?.Kill();
        _returnTween?.Kill();

        _idlePulseTween = backgroundImage.transform
            .DOScale(idlePulseScaleAmount, idlePulseDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopIdlePulseSmooth()
    {
        _idlePulseTween?.Kill();
        _changePulseSequence?.Kill();
        _returnTween?.Kill();

        _returnTween = backgroundImage.transform
            .DOScale(1f, returnToOneDuration)
            .SetEase(Ease.OutCubic);
    }
}
