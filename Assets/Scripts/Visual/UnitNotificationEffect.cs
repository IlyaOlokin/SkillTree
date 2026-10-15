using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Visual
{
    public class UnitNotificationEffect : MonoBehaviour
{
    [SerializeField] private Transform criticalEffect;
    [SerializeField] private Transform objectToMove;
    [SerializeField] private TextMeshProUGUI text;
    [SerializeField] private float lifeTime;
    [SerializeField] private float effectDuration;
    [SerializeField] private float scaleMultiplier;
    [SerializeField] private float moveDistance;
    [SerializeField] private float posSpreading;
    [Header("Text flight")]
    [Tooltip("World XY direction, normalized at spawn. Zero keeps text stationary.")]
    [SerializeField] private Vector2 textMoveDirection = Vector2.up;
    [SerializeField, Range(0f, 90f)] private float textDirectionSpread = 16f;
    [SerializeField, Range(0f, 0.5f)] private float textDistanceVariation = 0.12f;
    [Tooltip("Sideways arc height as a fraction of Move Distance.")]
    [SerializeField, Range(0f, 0.5f)] private float textArc = 0.12f;
    [SerializeField, Range(0.1f, 1f)] private float textStartScale = 0.6f;
    [SerializeField, Range(0f, 0.5f)] private float textStretch = 0.2f;
    [SerializeField, Range(0f, 20f)] private float textTilt = 7f;
    [SerializeField, Range(0.05f, 0.8f)] private float textFadePortion = 0.32f;
    [Header("Critical damage text")]
    [SerializeField, Min(1f)] private float criticalTextSpeedMultiplier = 1.4f;
    [SerializeField, Min(1f)] private float criticalTextScaleMultiplier = 1.2f;
    [SerializeField] private Color criticalTextColor = new(1f, 0.68f, 0.16f, 1f);
    [Header("Icon notification")]
    [SerializeField] private Vector2 iconSize = new(48f, 48f);
    [SerializeField] private Vector2 iconMoveDirection = new(-1f, 1f);
    [SerializeField, Min(0f)] private float iconMoveDistance = 1.3f;
    [SerializeField, Min(0.01f)] private float iconLifeTime = 0.9f;
    [SerializeField, Min(0.01f)] private float iconGrowDuration = 0.2f;
    [SerializeField, Min(0.01f)] private float iconDisappearDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float iconPeakScale = 1.2f;
    [SerializeField] private Image icon;
    private bool _isIcon;
    private Sequence _iconSequence;
    private Sequence _textSequence;
    
    [Header("Colors")]
    [SerializeField] private Color dmgColor = Color.white;
    [SerializeField] private Color dmgEnergyShieldColor = Color.cyan;
    [SerializeField] private Color healColor = Color.green;
    [SerializeField] private Color messageColor = new(1f, 0.82f, 0.18f, 1f);
    private Camera _worldCamera;
    private Vector2? _directionOverride;
    private bool _isCriticalText;

    void Start()
    {
        EnsureRuntimeReferences();
        objectToMove.position += new Vector3(Random.Range(-posSpreading, posSpreading),
            Random.Range(-posSpreading, posSpreading));
        AssignCanvasCamera();

        if (_isIcon)
        {
            PlayIconAnimation();
            return;
        }

        PlayTextAnimation();
    }

    private void PlayTextAnimation()
    {
        if (criticalEffect != null) criticalEffect.gameObject.SetActive(false);
        Vector3 startScale = objectToMove.localScale * (_isCriticalText ? Mathf.Max(1f, criticalTextScaleMultiplier) : 1f);
        Vector3 peakScale = startScale * scaleMultiplier;
        Vector3 startPos = objectToMove.position;
        Quaternion startRotation = objectToMove.localRotation;
        float tilt = Random.Range(-textTilt, textTilt);
        Vector2 direction = _directionOverride ?? textMoveDirection;
        direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.zero;
        direction = Quaternion.Euler(0f, 0f, Random.Range(-textDirectionSpread, textDirectionSpread)) * direction;
        float distance = moveDistance * Random.Range(1f - textDistanceVariation, 1f + textDistanceVariation);
        Vector3 targetPos = startPos + (Vector3)direction * distance;
        Vector3 arcOffset = new Vector3(-direction.y, direction.x) * (distance * textArc * Random.Range(-1f, 1f));
        float speed = _isCriticalText ? Mathf.Max(1f, criticalTextSpeedMultiplier) : 1f;
        float settleDuration = Mathf.Min(effectDuration, lifeTime * 0.6f) / speed;
        float growDuration = settleDuration * 0.28f;
        float fadeDuration = lifeTime * textFadePortion;
        float fadeStart = lifeTime - fadeDuration;
        Vector3 animatedScale = startScale * textStartScale;
        float stretch = 0f;
        float fadeScale = 1f;

        // Compose these channels so configurable fade and growth can safely overlap.
        void ApplyScale()
        {
            objectToMove.localScale = Vector3.Scale(animatedScale,
                new Vector3(1f - stretch * 0.5f, 1f + stretch, 1f)) * fadeScale;
        }

        void SetScale(Vector3 value)
        {
            animatedScale = value;
            ApplyScale();
        }

        ApplyScale();
        objectToMove.localRotation = startRotation * Quaternion.Euler(0f, 0f, tilt);
        _textSequence = DOTween.Sequence().SetEase(Ease.Linear);
        _textSequence.Insert(0f, DOVirtual.Float(0f, 1f, lifeTime / speed, travel =>
        {
            objectToMove.position = Vector3.LerpUnclamped(startPos, targetPos, travel)
                + arcOffset * Mathf.Sin(Mathf.PI * travel);
        }).SetEase(_isCriticalText ? Ease.OutExpo : Ease.OutCubic));
        _textSequence.Insert(0f,
            DOTween.To(() => animatedScale, SetScale, peakScale, growDuration).SetEase(Ease.OutCubic));
        _textSequence.Insert(growDuration,
            DOTween.To(() => animatedScale, SetScale, startScale, settleDuration - growDuration)
                .SetEase(Ease.InOutSine));
        _textSequence.Insert(0f, DOVirtual.Float(0f, 1f, growDuration, phase =>
        {
            stretch = Mathf.Sin(phase * Mathf.PI) * textStretch;
            ApplyScale();
        }).SetEase(Ease.Linear));
        _textSequence.Insert(growDuration,
            objectToMove.DOLocalRotateQuaternion(startRotation, settleDuration - growDuration)
                .SetEase(Ease.InOutSine));
        _textSequence.Insert(fadeStart, DOVirtual.Float(1f, 0.82f, fadeDuration, value =>
        {
            fadeScale = value;
            ApplyScale();
        }).SetEase(Ease.InOutSine));
        _textSequence.Insert(fadeStart, text.DOFade(0f, fadeDuration).SetEase(Ease.InOutSine));
        _textSequence.OnComplete(() => Destroy(gameObject));
    }

    public void WriteDamage(float dmg, bool energyShield = false, bool isCritical = false)
    {
        EnsureRuntimeReferences();
        _isCriticalText = isCritical;
        text.color = isCritical ? criticalTextColor : energyShield ? dmgEnergyShieldColor : dmgColor;
        text.text = Math.Round(dmg).ToString();
    }

    public void WriteHeal(float heal)
    {
        EnsureRuntimeReferences();
        _isCriticalText = false;
        text.color = healColor;
        text.text = Math.Round(heal).ToString();
    }
    
    public void WriteMessage(string message)
    {
        EnsureRuntimeReferences();
        _isCriticalText = false;
        text.color = messageColor;
        text.text = message;
    }

    /// <summary>Configure a fresh notification instance before its Start callback.</summary>
    public void ShowIcon(Sprite sprite, Color? tint = null)
    {
        if (sprite == null)
        {
            Destroy(gameObject);
            return;
        }

        EnsureRuntimeReferences();
        _isIcon = true;
        text.enabled = false;
        if (criticalEffect != null) criticalEffect.gameObject.SetActive(false);

        if (icon == null)
        {
            Debug.LogWarning("Icon notification requires an Image assigned in its prefab.", this);
            Destroy(gameObject);
            return;
        }
        icon.gameObject.SetActive(true);
        icon.rectTransform.sizeDelta = iconSize;
        icon.sprite = sprite;
        icon.color = tint ?? Color.white;
    }

    private void PlayIconAnimation()
    {
        float duration = Mathf.Max(0.03f, iconLifeTime);
        float growDuration = Mathf.Clamp(iconGrowDuration, 0.01f, duration * 0.5f);
        float disappearDuration = Mathf.Clamp(iconDisappearDuration, 0.01f, duration - growDuration);
        Vector3 peakScale = objectToMove.localScale * Mathf.Max(0.01f, iconPeakScale);
        Vector2 direction = iconMoveDirection.sqrMagnitude > 0.0001f
            ? iconMoveDirection.normalized : Vector2.zero;
        Vector3 destination = objectToMove.position + (Vector3)direction * Mathf.Max(0f, iconMoveDistance);
        objectToMove.localScale = peakScale * 0.1f;

        _iconSequence = DOTween.Sequence();
        _iconSequence.Append(objectToMove.DOScale(peakScale, growDuration).SetEase(Ease.OutBack));
        _iconSequence.Insert(0f, objectToMove.DOMove(destination, duration).SetEase(Ease.OutCubic));
        _iconSequence.Insert(duration - disappearDuration,
            objectToMove.DOScale(Vector3.zero, disappearDuration).SetEase(Ease.InBack));
        _iconSequence.Insert(duration - disappearDuration,
            icon.DOFade(0f, disappearDuration).SetEase(Ease.InQuad));
        _iconSequence.OnComplete(() => Destroy(gameObject));
    }

    private void OnDisable()
    {
        _textSequence?.Kill();
        _textSequence = null;
        _iconSequence?.Kill();
        _iconSequence = null;
        // Notifications are disposable; a killed sequence must not leave an orphan.
        if (_isIcon || Application.isPlaying) Destroy(gameObject);
    }

    public void SetWorldCamera(Camera worldCamera)
    {
        _worldCamera = worldCamera;
        AssignCanvasCamera();
    }

    public void InitTargetPos(Vector3 damagerPos, bool isCritical)
    {
        EnsureRuntimeReferences();
        Vector2 away = objectToMove.position - damagerPos;
        _directionOverride = away.sqrMagnitude < posSpreading * posSpreading
            ? textMoveDirection : away.normalized;
        // Retain the legacy API without re-enabling the retired critical decoration.
    }

    private void EnsureRuntimeReferences()
    {
        if (objectToMove == null)
            objectToMove = transform;

        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);

        if (text == null)
            text = CreateFallbackText();

        if (criticalEffect == null)
        {
            Transform critical = transform.Find("CriticalEffect");
            if (critical != null)
                criticalEffect = critical;
        }

        if (lifeTime <= 0f)
            lifeTime = 0.85f;

        if (effectDuration <= 0f)
            effectDuration = 0.32f;

        if (scaleMultiplier <= 0f)
            scaleMultiplier = 1.25f;

        if (moveDistance <= 0f)
            moveDistance = transform is RectTransform ? 45f : 0.45f;

        if (posSpreading <= 0f)
            posSpreading = transform is RectTransform ? 12f : 0.12f;
    }

    private TextMeshProUGUI CreateFallbackText()
    {
        GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer));
        RectTransform rectTransform = textObject.transform as RectTransform;
        rectTransform.SetParent(transform, false);
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(160f, 42f);

        TextMeshProUGUI createdText = textObject.AddComponent<TextMeshProUGUI>();
        createdText.alignment = TextAlignmentOptions.Center;
        createdText.fontSize = 24f;
        createdText.raycastTarget = false;
        return createdText;
    }

    private void AssignCanvasCamera()
    {
        Canvas canvas = GetComponent<Canvas>() ?? GetComponentInChildren<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return;

        Camera camera = _worldCamera != null ? _worldCamera : ResolveParentCanvasCamera();
        if (camera == null)
            camera = FindBattleCamera();
        if (camera == null)
            camera = Camera.main;

        if (camera != null)
            canvas.worldCamera = camera;
    }

    private Camera ResolveParentCanvasCamera()
    {
        Canvas parentCanvas = transform.parent != null
            ? transform.parent.GetComponentInParent<Canvas>()
            : null;

        if (parentCanvas == null || parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return parentCanvas.worldCamera;
    }

    private static Camera FindBattleCamera()
    {
        GameObject battleCameraObject = GameObject.FindWithTag("BattleCamera");
        if (battleCameraObject == null)
            return null;

        Camera camera = battleCameraObject.GetComponent<Camera>();
        if (camera != null)
            return camera;

        return battleCameraObject.GetComponentInChildren<Camera>();
    }
}

[Serializable]
public class InnerColor
{
    public int bottomBorder;
    public Color color;
}
}
