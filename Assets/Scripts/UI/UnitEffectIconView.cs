using System;
using System.Collections.Generic;
using Battle;
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

        public RectTransform RectTransform { get; private set; }
        
        private TooltipUI _tooltipUI;
        private BaseEffect _effect;

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

        public void SetEffect(BaseEffect effect)
        {
            _effect = effect;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            return _effect != null
                ? _effect.GetTooltipDescriptions()
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
            if (_tooltipUI == null || _effect == null)
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
    }
}
