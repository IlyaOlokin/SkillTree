using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TooltipSystem
{
    [RequireComponent(typeof(TMP_Text))]
    public class TooltipTextLinkHandler : MonoBehaviour, IPointerEnterHandler, IPointerMoveHandler, IPointerExitHandler
    {
        private TMP_Text textComponent;
        private string currentHoveredLinkId;
        private TooltipUI tooltipUI;
        private int tooltipLevel;
        private bool showAsRootTooltip;
        private TooltipCanvasTarget canvasTarget = TooltipCanvasTarget.SkillTree;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            textComponent.raycastTarget = true;
        }

        public void Initialize(
            TooltipUI ownerTooltipUI,
            int level,
            bool useRootTooltip = false,
            TooltipCanvasTarget targetCanvas = TooltipCanvasTarget.SkillTree)
        {
            tooltipUI = ownerTooltipUI;
            tooltipLevel = level;
            showAsRootTooltip = useRootTooltip;
            canvasTarget = targetCanvas;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            TryLogHoveredLink(eventData);
        }

        public void OnPointerMove(PointerEventData eventData)
        {
            TryLogHoveredLink(eventData);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (showAsRootTooltip)
            {
                HideCurrentTooltip();
            }

            currentHoveredLinkId = null;
        }

        private void OnDisable()
        {
            HideCurrentTooltip();
            currentHoveredLinkId = null;
        }

        private void TryLogHoveredLink(PointerEventData eventData)
        {
            ResolveTooltipUI();
            Camera eventCamera = GetEventCamera();
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, eventData.position, eventCamera);
            if (linkIndex < 0)
            {
                if (showAsRootTooltip)
                {
                    HideCurrentTooltip();
                }

                currentHoveredLinkId = null;
                return;
            }

            TMP_LinkInfo linkInfo = textComponent.textInfo.linkInfo[linkIndex];
            string linkId = linkInfo.GetLinkID();
            if (string.IsNullOrEmpty(linkId) || linkId == currentHoveredLinkId)
            {
                return;
            }

            currentHoveredLinkId = linkId;
            if (showAsRootTooltip)
            {
                tooltipUI?.DisplayLinkedTooltipAsRoot(this, linkId, eventData.position, canvasTarget);
                return;
            }

            tooltipUI?.DisplayLinkedTooltip(tooltipLevel + 1, linkId, eventData.position);
        }

        private Camera GetEventCamera()
        {
            Canvas canvas = textComponent.canvas;
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        private void HideCurrentTooltip()
        {
            if (tooltipUI == null || string.IsNullOrEmpty(currentHoveredLinkId))
            {
                return;
            }

            if (showAsRootTooltip)
            {
                tooltipUI.RequestHideTooltip(this);
                return;
            }

            tooltipUI.HideLinkedTooltipsFrom(tooltipLevel + 1);
        }

        private void ResolveTooltipUI()
        {
            if (tooltipUI != null)
            {
                return;
            }

            tooltipUI = FindFirstObjectByType<TooltipUI>(FindObjectsInactive.Include);
        }
    }
}
