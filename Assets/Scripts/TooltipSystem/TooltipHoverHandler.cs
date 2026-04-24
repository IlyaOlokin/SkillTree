using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace TooltipSystem
{
    public class TooltipHoverHandler : MonoBehaviour
    {
        [Inject] private TooltipUI tooltipUI;
        [SerializeField] private MonoBehaviour tooltipSource;

        private ITooltipDescriptionProvider tooltipDescriptionProvider;

        private void Awake()
        {
            tooltipDescriptionProvider = ResolveTooltipDescriptionProvider();
        }

        private void OnMouseEnter()
        {
            if (tooltipDescriptionProvider == null || IsPointerOverUI())
            {
                return;
            }

            tooltipUI.DisplayTooltip(this, tooltipDescriptionProvider, transform.position);
        }

        private void OnMouseExit()
        {
            tooltipUI.RequestHideTooltip(this);
        }

        private ITooltipDescriptionProvider ResolveTooltipDescriptionProvider()
        {
            return TooltipDescriptionProviderResolver.Resolve(gameObject, tooltipSource);
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }
    }
}
