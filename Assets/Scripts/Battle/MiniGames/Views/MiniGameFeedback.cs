using LocalizationSupport;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Battle.MiniGames
{
    /// <summary>Presentation only. Gameplay clocks, input and rewards belong to the views.</summary>
    [DisallowMultipleComponent]
    public sealed class MiniGameFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public const float ResultDuration = 1.2f;
        [SerializeField] private TMP_FontAsset font;
        private MiniGameSigilGraphic _sigil;
        private TMP_Text _title, _hint, _counter;
        private CanvasGroup _group;
        private bool _speed, _activator, _ready, _resolved, _success, _perfect, _early;
        private float _age, _resultAge, _hit, _remaining = 1f;
        private int _presses, _required = 5;
        private bool _window;
        private bool _hovered;
        private float _clickScaleBonus, _visualScale = 1f, _scaleVelocity;
        private const float HoverScaleBonus = .03f;
        private const float ClickScaleStep = .04f;
        private const float MaxClickScaleBonus = .07f;

        private void OnEnable() => LocalizationSettings.SelectedLocaleChanged += LocaleChanged;
        private void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= LocaleChanged;
            _hovered = false;
        }
        private void LocaleChanged(Locale locale) { if (_ready) RefreshText(); }

        public void Initialize(bool speed, bool activator)
        {
            _speed = speed;
            _activator = activator;
            if (!_ready)
            {
                // Keep the existing input target and listeners, retire only the old artwork.
                foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                    graphic.enabled = false;
                var button = GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (button != null)
                {
                    // The old Target can bounce or move; the new visual owns its own feedback.
                    button.transform.SetParent(transform, false);
                    var image = button.GetComponent<UnityEngine.UI.Image>();
                    if (image != null)
                    {
                        image.enabled = true;
                        image.sprite = null;
                        image.color = Color.clear;
                        image.raycastTarget = true;
                        image.canvasRenderer.cullTransparentMesh = false;
                        button.targetGraphic = image;
                        button.transition = UnityEngine.UI.Selectable.Transition.None;
                    }
                    var rect = (RectTransform)button.transform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
                    rect.anchoredPosition = Vector2.zero;
                    rect.localScale = Vector3.one;
                    rect.sizeDelta = Vector2.one * (activator ? 86f : 194f);
                }
                var root = new GameObject("AnimatedSigil", typeof(RectTransform), typeof(CanvasGroup));
                root.layer = gameObject.layer;
                root.transform.SetParent(transform, false);
                var rootRect = (RectTransform)root.transform;
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.one * (activator ? 100f : 340f);
                _group = root.GetComponent<CanvasGroup>();
                _group.blocksRaycasts = false;
                var art = new GameObject("Geometry", typeof(RectTransform), typeof(CanvasRenderer), typeof(MiniGameSigilGraphic));
                art.layer = root.layer;
                art.transform.SetParent(root.transform, false);
                _sigil = art.GetComponent<MiniGameSigilGraphic>();
                _sigil.rectTransform.sizeDelta = rootRect.sizeDelta;
                _sigil.raycastTarget = false;
                if (!activator)
                {
                    _title = Label(root.transform, "Title", 146f, 17f);
                    _hint = Label(root.transform, "Instruction", -139f, 13f);
                    _counter = Label(root.transform, "Counter", 0f, speed ? 36f : 20f);
                }
                _ready = true;
            }
            _age = _resultAge = _hit = 0f;
            _resolved = _window = false;
            _remaining = 1f;
            _presses = 0;
            _clickScaleBonus = _scaleVelocity = 0f;
            _visualScale = 1f;
            // Scale only the artwork. Its sibling Button keeps a fixed hit area and position.
            _group.transform.localScale = Vector3.one;
            _group.alpha = 0f;
            if (_title != null) _title.color = new Color(.97f, .94f, .84f);
            RefreshText();
            Render();
        }

        private TMP_Text Label(Transform parent, string name, float y, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.font = font != null ? font : TMP_Settings.defaultFontAsset;
            text.fontSize = size;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(.97f, .94f, .84f);
            text.raycastTarget = false;
            text.rectTransform.sizeDelta = new Vector2(270f, 32f);
            text.rectTransform.anchoredPosition = new Vector2(0f, y);
            return text;
        }

        public void Timing(float ringRatio, bool window)
        {
            _remaining = ringRatio;
            if (_window != window) { _window = window; RefreshText(); }
            Render();
        }

        public void Rapid(float timeLeft, int presses, int required)
        {
            if (presses > _presses)
            {
                _hit = 1f;
                NotifyClick();
            }
            _remaining = Mathf.Clamp01(timeLeft);
            bool changed = presses != _presses || required != _required;
            _presses = presses;
            _required = Mathf.Max(1, required);
            if (changed) RefreshText();
            Render();
        }

        public void ActivatorTime(float remaining) { _remaining = Mathf.Clamp01(remaining); Render(); }

        public void OnPointerEnter(PointerEventData data) => _hovered = true;
        public void OnPointerExit(PointerEventData data) => _hovered = false;

        public void NotifyClick()
        {
            // Add to the current envelope; never reset scale or smoothing velocity on input.
            _clickScaleBonus = Mathf.Min(MaxClickScaleBonus, _clickScaleBonus + ClickScaleStep);
        }

        private void UpdateInteraction(float deltaTime)
        {
            if (deltaTime <= 0f) return;
            _clickScaleBonus *= Mathf.Exp(-5.5f * deltaTime);
            float targetScale = 1f + (_hovered && !_resolved ? HoverScaleBonus : 0f) + _clickScaleBonus;
            _visualScale = Mathf.SmoothDamp(_visualScale, targetScale, ref _scaleVelocity,
                .055f, Mathf.Infinity, deltaTime);
            _group.transform.localScale = Vector3.one * _visualScale;
        }

        public void Resolve(BattleMiniGameResult result, bool perfect = false, bool early = false)
        {
            _resolved = true;
            _success = result.IsSuccess;
            _perfect = perfect;
            _early = early;
            _resultAge = 0f;
            _hit = 1f;
            RefreshText();
            Render();
        }

        private void Update()
        {
            if (!_ready) return;
            float dt = Time.deltaTime;
            _age += dt;
            _hit = Mathf.Max(0f, _hit - dt * 5f);
            if (_resolved) _resultAge += dt;
            float appear = Mathf.Clamp01(_age / .18f);
            float exit = _resolved ? 1f - Mathf.SmoothStep(0f, 1f, (_resultAge - .95f) / .25f) : 1f;
            _group.alpha = appear * exit;
            UpdateInteraction(dt);
            Render();
        }

        private void RefreshText()
        {
            if (_activator || !_ready) return;
            _title.text = Text(_speed ? "speed" : "damage", _speed ? "ATTACK SPEED" : "DAMAGE");
            _hint.text = Text(_speed ? "rapid" : "align",
                _speed ? "CLICK RAPIDLY" : "CLICK WHEN RINGS ALIGN");
            _counter.text = _speed ? $"{_presses} / {_required}"
                : Text(_window ? "now" : "wait", _window ? "CLICK NOW" : "WAIT");
            _counter.color = !_speed && !_window ? new Color(.65f, .68f, .7f) : new Color(1f, .97f, .86f);
            if (!_resolved) return;
            _title.text = _success
                ? Text(_perfect ? "perfect" : "success", _perfect ? "PERFECT" : "SUCCESS")
                : Text(_speed ? "timeout" : (_early ? "early" : "late"), _speed ? "TIME'S UP" : (_early ? "TOO EARLY" : "TOO LATE"));
            _hint.text = _success
                ? Text(_speed ? "speedReward" : "damageReward", _speed ? "ATTACK SPEED BOOSTED" : "DAMAGE BOOSTED")
                : Text("noReward", "NO BONUS");
            _counter.text = "";
            _title.color = _success ? new Color(1f, .86f, .46f) : new Color(1f, .48f, .43f);
        }

        private static string Text(string key, string fallback) => GameLocalization.GetGameUI("minigame." + key, fallback);

        private void Render()
        {
            if (_sigil == null) return;
            _sigil.Show(_speed, _activator, _remaining, _presses, _required, _window,
                _age, _hit, _resolved, _success, _resultAge);
        }
    }
}
