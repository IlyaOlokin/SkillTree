using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace TooltipSystem
{
    public class TooltipHoverHandler : MonoBehaviour
    {
        [Inject(Optional = true)] [SerializeField] private TooltipUI tooltipUI;
        [SerializeField] private MonoBehaviour tooltipSource;

        private ITooltipDescriptionProvider tooltipDescriptionProvider;

        private void Awake()
        {
            ResolveTooltipUI();
            tooltipDescriptionProvider = ResolveTooltipDescriptionProvider();
        }

        private void OnMouseEnter()
        {
            ResolveTooltipUI();
            if (tooltipUI == null || tooltipDescriptionProvider == null || IsPointerOverUI())
            {
                return;
            }

            tooltipUI.DisplayTooltip(this, tooltipDescriptionProvider, transform.position);
        }

        private void OnMouseExit()
        {
            ResolveTooltipUI();
            tooltipUI?.RequestHideTooltip(this);
        }

        private ITooltipDescriptionProvider ResolveTooltipDescriptionProvider()
        {
            return TooltipDescriptionProviderResolver.Resolve(gameObject, tooltipSource);
        }

        private static bool IsPointerOverUI()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private void ResolveTooltipUI()
        {
            if (tooltipUI != null)
                return;

            tooltipUI = FindAnyObjectByType<TooltipUI>(FindObjectsInactive.Include);
        }
    }
}
