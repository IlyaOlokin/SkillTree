using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Zenject;

namespace Tutorials
{
    // Keep this component on an always-active object, outside windowRoot.
    // The root contains a full-screen raycast-blocking dimmer and the panel above it.
    public sealed class TutorialWindow : MonoBehaviour
    {
        [SerializeField] private GameObject windowRoot;
        [SerializeField] private CanvasGroup dimmer;
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text body;
        [SerializeField] private Button closeButton;
        [SerializeField] private Toggle skipAll;
        [SerializeField] private TMP_Text headingLabel;
        [SerializeField] private TMP_Text closeLabel;
        [SerializeField] private TMP_Text skipAllLabel;
        [SerializeField] private ScrollRect textScroll;
        [SerializeField, Min(160f)] private float panelWidthPixels = 480f;
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        [SerializeField, Range(0f, 1f)] private float dimOpacity = 0.7f;
        [Tooltip("Death, reward, transition and minigame roots which must finish before a tutorial starts.")]
        [SerializeField] private GameObject[] blockingWindows = new GameObject[0];
        [Tooltip("Only input behaviours that bypass UI raycasts (camera controls, direct hotkeys). Do not add node handlers, UI buttons, the EventSystem or save services.")]
        [SerializeField] private Behaviour[] gameplayInput = new Behaviour[0];

        [Inject] private TutorialService tutorials;
        [Inject] private BattleTickSystem battle;
        [Inject] private EnemySpawner spawner;
        [Inject] private UnitLevel level;
        private readonly List<Behaviour> disabledInput = new List<Behaviour>();
        private Sequence tween;
        private bool ownsPause;
        private bool transitioning;
        private float previousTimeScale;
        private float hiddenX;
        private GameObject previousSelection;

        private void Start()
        {
            if (windowRoot == null || windowRoot == gameObject || transform.IsChildOf(windowRoot.transform) ||
                dimmer == null || panel == null || title == null || body == null || closeButton == null || skipAll == null)
            {
                Debug.LogError("TutorialWindow needs a separate window root, dimmer, panel, text, close button and toggle.", this);
                enabled = false;
                return;
            }
            windowRoot.SetActive(false);
            dimmer.alpha = 0f;
            closeButton.onClick.AddListener(Close);
            tutorials.ProfileReset += ResetPresentation;
        }

        private void LateUpdate()
        {
            tutorials.SetContext(spawner.SelectedLocationId, level.Level);
            if (ownsPause || transitioning || !tutorials.IsReady) return;
            foreach (var blocker in blockingWindows)
                if (blocker != null && blocker.activeInHierarchy) return;
            if (tutorials.TryBeginNext(out var next))
            {
                AcquirePause();
                Show(next);
            }
        }

        private void AcquirePause()
        {
            ownsPause = true;
            previousSelection = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            battle.AcquirePause(this);
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            foreach (var input in gameplayInput)
                SuspendInput(input);
        }

        private void SuspendInput(Behaviour input)
        {
            if (input == null || input == this || !input.enabled) return;
            disabledInput.Add(input);
            input.enabled = false;
        }

        private void Show(TutorialDefinition definition)
        {
            transitioning = true;
            windowRoot.SetActive(true);
            title.text = definition.GetLocalizedTitle();
            body.text = definition.GetLocalizedBody();
            if (headingLabel != null)
                headingLabel.text = GameLocalization.GetFromTable(GameLocalization.TutorialTable, "ui.tutorial.heading", "Tutorial");
            if (closeLabel != null)
                closeLabel.text = GameLocalization.GetFromTable(GameLocalization.TutorialTable, "ui.tutorial.close", "Close");
            if (skipAllLabel != null)
                skipAllLabel.text = GameLocalization.GetFromTable(GameLocalization.TutorialTable, "ui.tutorial.skipAll", "Skip all tutorials");
            skipAll.SetIsOnWithoutNotify(false);
            closeButton.interactable = false;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            // Panel is edge-anchored, independent of aspect ratio and CanvasScaler scale.
            bool left = definition.side == TutorialSide.Left;
            panel.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            panel.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            panel.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
            panel.sizeDelta = new Vector2(panelWidthPixels / Mathf.Max(0.01f, canvas.scaleFactor), 0f);
            Canvas.ForceUpdateCanvases();
            if (textScroll != null) textScroll.verticalNormalizedPosition = 1f;
            hiddenX = (left ? -1f : 1f) * (panel.rect.width + 32f);
            panel.anchoredPosition = new Vector2(hiddenX, 0f);
            dimmer.blocksRaycasts = true;
            tween = DOTween.Sequence().SetUpdate(true)
                .Join(panel.DOAnchorPosX(0f, duration).SetEase(Ease.OutCubic))
                .Join(dimmer.DOFade(dimOpacity, duration))
                .OnComplete(() => { transitioning = false; closeButton.interactable = true; });
        }

        private void Close()
        {
            if (transitioning || !ownsPause) return;
            transitioning = true;
            closeButton.interactable = false;
            tutorials.CompleteCurrent(skipAll.isOn);
            tween = DOTween.Sequence().SetUpdate(true)
                .Append(panel.DOAnchorPosX(hiddenX, duration).SetEase(Ease.InCubic))
                .OnComplete(() =>
                {
                    tutorials.SetContext(spawner.SelectedLocationId, level.Level);
                    if (tutorials.TryBeginNext(out var next)) Show(next);
                    else
                    {
                        tween = DOTween.Sequence().SetUpdate(true)
                            .Append(dimmer.DOFade(0f, duration))
                            .OnComplete(ResetPresentation);
                    }
                });
        }

        private void ResetPresentation()
        {
            tween?.Kill();
            tween = null;
            transitioning = false;
            if (windowRoot != null && windowRoot != gameObject) windowRoot.SetActive(false);
            if (dimmer != null) dimmer.alpha = 0f;
            if (!ownsPause) return;
            ownsPause = false;
            battle.ReleasePause(this);
            Time.timeScale = previousTimeScale;
            foreach (var input in disabledInput) if (input != null) input.enabled = true;
            disabledInput.Clear();
            tutorials.CancelPresentation();
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(previousSelection != null && previousSelection.activeInHierarchy ? previousSelection : null);
            previousSelection = null;
        }

        private void OnDisable() => ResetPresentation();
        private void OnDestroy()
        {
            ResetPresentation();
            if (tutorials != null) tutorials.ProfileReset -= ResetPresentation;
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        }
    }
}
