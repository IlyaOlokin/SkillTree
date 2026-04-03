using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace TooltipSystem
{
    public class TooltipPointerHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Inject] private TooltipUI tooltipUI;
        [SerializeField] private MonoBehaviour tooltipSource;

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

            tooltipUI.DisplayTooltip(this, tooltipDescriptionProvider, eventData.position);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltipUI.HideTooltip(this);
        }
    }
}
