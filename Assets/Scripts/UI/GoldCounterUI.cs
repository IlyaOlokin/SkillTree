using CurrencySystem;
using DG.Tweening;
using DropSystem;
using TMPro;
using UnityEngine;
using Zenject;

public sealed class GoldCounterUI : MonoBehaviour
{
    [Inject] private PlayerWallet _wallet;

    [Header("View")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private RectTransform flyTarget;
    [SerializeField] private Transform pulseRoot;

    [Header("Optional scene references")]
    [SerializeField] private EnemyItemDropSpawner dropSpawner;

    [Header("Pulse")]
    [SerializeField] [Min(1f)] private float pulseScale = 1.08f;
    [SerializeField] [Min(0f)] private float pulseDuration = 0.22f;
    [SerializeField] private Ease pulseEase = Ease.OutBack;

    private Vector3 _baseScale = Vector3.one;
    private Tween _pulseTween;
    private bool _subscribed;

    private void Awake()
    {
        ResolveReferences();
        CacheBaseTransform();
    }

    private void Start()
    {
        ResolveReferences();
        Subscribe();
        RegisterFlyTarget();

        if (_wallet != null)
            Refresh(_wallet.DisplayedGold);
    }

    private void OnEnable()
    {
        RegisterFlyTarget();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        _pulseTween?.Kill();
    }

    private void ResolveReferences()
    {
        if (goldText == null)
            goldText = GetComponentInChildren<TMP_Text>(true);

        if (flyTarget == null)
            flyTarget = transform as RectTransform;

        if (pulseRoot == null)
            pulseRoot = goldText != null ? goldText.transform : transform;

        if (dropSpawner == null)
            dropSpawner = FindAnyObjectByType<EnemyItemDropSpawner>(FindObjectsInactive.Include);
    }

    private void CacheBaseTransform()
    {
        Transform target = pulseRoot != null ? pulseRoot : transform;
        _baseScale = target.localScale;
    }

    private void Subscribe()
    {
        if (_subscribed || _wallet == null)
            return;

        _wallet.OnGoldDisplayChanged += Refresh;
        _wallet.OnGoldChanged += PlayPulse;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed || _wallet == null)
            return;

        _wallet.OnGoldDisplayChanged -= Refresh;
        _wallet.OnGoldChanged -= PlayPulse;
        _subscribed = false;
    }

    private void RegisterFlyTarget()
    {
        if (dropSpawner == null || flyTarget == null)
            return;

        dropSpawner.SetGoldFlyTarget(flyTarget);
    }

    private void Refresh(int displayedGold)
    {
        if (goldText != null)
            goldText.text = displayedGold.ToString();
    }

    private void PlayPulse(int _)
    {
        Transform target = pulseRoot != null ? pulseRoot : transform;
        _pulseTween?.Kill();
        target.localScale = _baseScale;

        _pulseTween = target
            .DOScale(_baseScale * Mathf.Max(1f, pulseScale), Mathf.Max(0.01f, pulseDuration))
            .SetEase(pulseEase)
            .SetLoops(2, LoopType.Yoyo)
            .SetUpdate(true);
    }
}
