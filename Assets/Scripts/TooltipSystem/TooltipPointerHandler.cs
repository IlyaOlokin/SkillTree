using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace TooltipSystem
{
    public class TooltipPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Inject] private TooltipUI tooltipUI;
        [SerializeField] private MonoBehaviour tooltipSource;
        [SerializeField] private TooltipCanvasTarget canvasTarget;

        private ITooltipDescriptionProvider tooltipDescriptionProvider;

        private void Awake()
        {
            tooltipDescriptionProvider = TooltipDescriptionProviderResolver.Resolve(gameObject, tooltipSource);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (tooltipDescriptionProvider == null)
            {
                return;
            }

            tooltipUI.DisplayTooltip(this, tooltipDescriptionProvider, eventData.position, canvasTarget);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipUI.RequestHideTooltip(this);
        }

        private void OnDisable()
        {
            tooltipUI.HideTooltip(this);
        }
    }
}
