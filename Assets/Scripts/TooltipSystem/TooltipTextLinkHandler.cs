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

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            textComponent.raycastTarget = true;
        }

        public void Initialize(TooltipUI ownerTooltipUI, int level)
        {
            tooltipUI = ownerTooltipUI;
            tooltipLevel = level;
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
            currentHoveredLinkId = null;
        }

        private void TryLogHoveredLink(PointerEventData eventData)
        {
            Camera eventCamera = GetEventCamera();
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(textComponent, eventData.position, eventCamera);
            if (linkIndex < 0)
            {
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
    }
}
