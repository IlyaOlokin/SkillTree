using System;
using System.Collections.Generic;
using Battle;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Visual
{
    public class UnitEffectIconView : MonoBehaviour, ITooltipDescriptionProvider, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image borderTimerImage;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private Image valueTextBackground;

        public RectTransform RectTransform { get; private set; }
        
        private TooltipUI _tooltipUI;
        private IReadOnlyList<ActiveEffect> _activeEffects = Array.Empty<ActiveEffect>();

        private void Awake()
        {
            RectTransform = transform as RectTransform;
        }

        public void Initialize(TooltipUI tooltipUI)
        {
            _tooltipUI = tooltipUI;
        }

        private void OnDisable()
        {
            if (_tooltipUI != null)
            {
                _tooltipUI.HideTooltip(this);
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
            }
        }

        public void SetTimerProgress(float normalizedTimeLeft)
        {
            if (borderTimerImage != null)
            {
                borderTimerImage.fillAmount = Mathf.Clamp01(normalizedTimeLeft);
            }
        }

        public void SetValueText(string text)
        {
            bool hasText = !string.IsNullOrWhiteSpace(text);
            if (valueTextBackground != null)
            {
                valueTextBackground.enabled = hasText;
            }

            if (valueText == null)
            {
                return;
            }

            valueText.text = hasText ? text : string.Empty;
        }

        public void SetEffects(IReadOnlyList<ActiveEffect> activeEffects)
        {
            _activeEffects = activeEffects ?? Array.Empty<ActiveEffect>();
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return TryGetPrimaryEffect(out var effect)
                ? effect.GetTooltipDescriptions()
                : Array.Empty<string>();
        }

        public string GetTooltipTitle()
        {
            return string.Empty;
        }

        public bool ShouldShowTooltipTitle()
        {
            return false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_tooltipUI == null || !TryGetPrimaryEffect(out _))
            {
                return;
            }

            _tooltipUI.DisplayTooltip(this, this, eventData.position, TooltipCanvasTarget.Battle);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_tooltipUI == null)
            {
                return;
            }

            _tooltipUI.RequestHideTooltip(this);
        }

        private bool TryGetPrimaryEffect(out BaseEffect effect)
        {
            if (_activeEffects != null && _activeEffects.Count > 0)
            {
                effect = _activeEffects[0].Effect;
                return effect != null;
            }

            effect = null;
            return false;
        }
    }
}
