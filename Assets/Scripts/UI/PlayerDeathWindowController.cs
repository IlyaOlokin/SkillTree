using System;
using System.Collections.Generic;
using Battle;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class PlayerDeathWindowController : MonoBehaviour
{
    [Inject] private BattleTickSystem _battleTickSystem;
    [Inject] private PlayerUnit _player;
    [Inject] private EnemySpawner _enemySpawner;
    
    [SerializeField] private LocationFlowController locationFlowController;
    [SerializeField] private GameObject window;
    [SerializeField] private Image background;
    [SerializeField] private List<Image> uiObjects = new List<Image>();
    [SerializeField] private List<TMP_Text> uiTexts = new List<TMP_Text>();
    [SerializeField] private float backgroundFadeDuration = 0.5f;
    [SerializeField] private float uiFadeDuration = 0.25f;
    [SerializeField] private float uiFadeDelay = 0.3f;

    public event Action OnDeathWindowOpened;
    public event Action OnDeathWindowClosed;

    private float _backgroundTargetAlpha;
    private readonly List<float> _uiTargetAlphas = new();
    private readonly List<float> _uiTextTargetAlphas = new();
    private Sequence _deathSequence;

    private void Awake()
    {
        if (locationFlowController == null)
            locationFlowController = FindFirstObjectByType<LocationFlowController>();

        CacheTargetAlphas();
        HideInstant();
    }

    private void OnEnable()
    {
        if (_player != null)
        {
            _player.OnDeath += HandlePlayerDeath;
        }

        if (_enemySpawner != null)
        {
            _enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
        {
            locationFlowController.OnModeChanged += HandleLocationModeChanged;
        }
    }

    private void OnDisable()
    {
        if (_player != null)
        {
            _player.OnDeath -= HandlePlayerDeath;
        }

        if (_enemySpawner != null)
        {
            _enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
        }

        if (locationFlowController != null)
        {
            locationFlowController.OnModeChanged -= HandleLocationModeChanged;
        }
    }

    public void RestartBattle()
    {
        CloseDeathWindow();

        _player.gameObject.SetActive(true);
        _player.ResetCombatState();

        _enemySpawner.RestartCurrentLevel();
        _battleTickSystem.Resume();
    }

    public void ExitLocation()
    {
        CloseDeathWindow();

        _player.gameObject.SetActive(true);
        _player.ResetCombatState();

        if (locationFlowController != null)
        {
            locationFlowController.ReturnToMap();
        }
        else
        {
            _enemySpawner.ExitBattle();
            _battleTickSystem.Pause();
        }
    }

    private void HandlePlayerDeath(Unit unit)
    {
        _battleTickSystem.Pause();
        PlayDeathAnimation();
        window.SetActive(true);
        OnDeathWindowOpened?.Invoke();
    }

    private void HandleBattleActivityChanged(bool isBattleActive)
    {
        if (!isBattleActive)
        {
            CloseDeathWindow();
        }
    }

    private void HandleLocationModeChanged(LocationFlowController.FlowMode mode)
    {
        if (mode == LocationFlowController.FlowMode.Map)
        {
            CloseDeathWindow();
        }
    }

    private void OnDestroy()
    {
        _deathSequence?.Kill();
    }

    private void CacheTargetAlphas()
    {
        _backgroundTargetAlpha = background != null ? background.color.a : 0f;

        _uiTargetAlphas.Clear();
        _uiTextTargetAlphas.Clear();
        
        foreach (var t in uiObjects)
        {
            _uiTargetAlphas.Add(t != null ? t.color.a : 0f);
        }

        foreach (var t in uiTexts)
        {
            _uiTextTargetAlphas.Add(t != null ? t.color.a : 0f);
        }
    }

    private void HideInstant()
    {
        var backgroundColor = background.color;
        backgroundColor.a = 0f;
        background.color = backgroundColor;
        
        foreach (var image in uiObjects)
        {
            var color = image.color;
            color.a = 0f;
            image.color = color;
        }

        foreach (var text in uiTexts)
        {
            var color = text.color;
            color.a = 0f;
            text.color = color;
        }
    }

    private void CloseDeathWindow()
    {
        bool wasOpen = window != null && window.activeSelf;

        _deathSequence?.Kill();
        HideInstant();

        if (window != null)
        {
            window.SetActive(false);
        }

        if (wasOpen)
        {
            OnDeathWindowClosed?.Invoke();
        }
    }

    private void PlayDeathAnimation()
    {
        _deathSequence?.Kill();
        HideInstant();

        _deathSequence = DOTween.Sequence();

        _deathSequence.Append(background.DOFade(_backgroundTargetAlpha, backgroundFadeDuration));
        float uiStartTime = backgroundFadeDuration + uiFadeDelay;

        for (int i = 0; i < uiObjects.Count; i++)
        {
            float targetAlpha = i < _uiTargetAlphas.Count ? _uiTargetAlphas[i] : 1f;
            _deathSequence.Insert(uiStartTime, uiObjects[i].DOFade(targetAlpha, uiFadeDuration));
        }

        for (int i = 0; i < uiTexts.Count; i++)
        {
            float targetAlpha = i < _uiTextTargetAlphas.Count ? _uiTextTargetAlphas[i] : 1f;
            _deathSequence.Insert(uiStartTime, uiTexts[i].DOFade(targetAlpha, uiFadeDuration));
        }
    }
}
