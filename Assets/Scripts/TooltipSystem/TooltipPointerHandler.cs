using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace TooltipSystem
{
    public class TooltipPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Inject(Optional = true)] [SerializeField] private TooltipUI tooltipUI;
        [SerializeField] private MonoBehaviour tooltipSource;
        [SerializeField] private TooltipCanvasTarget canvasTarget;

        private ITooltipDescriptionProvider tooltipDescriptionProvider;

        private void Awake()
        {
            ResolveTooltipUI();
            tooltipDescriptionProvider = TooltipDescriptionProviderResolver.Resolve(gameObject, tooltipSource);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ResolveTooltipUI();
            if (tooltipUI == null || tooltipDescriptionProvider == null)
            {
                return;
            }

            tooltipUI.DisplayTooltip(this, tooltipDescriptionProvider, eventData.position, canvasTarget);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            ResolveTooltipUI();
            tooltipUI?.RequestHideTooltip(this);
        }

        private void OnDisable()
        {
            ResolveTooltipUI();
            tooltipUI?.HideTooltip(this);
        }

        private void ResolveTooltipUI()
        {
            if (tooltipUI != null)
                return;

            tooltipUI = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
        }
    }
}
