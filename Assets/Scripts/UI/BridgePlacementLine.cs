using System;
using SkillTree;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    internal sealed class BridgePlacementLine : IDisposable
    {
        private RectTransform _bridgeLine;

        public void Draw(SocketNode first, RectTransform cursor, Canvas canvas, RectTransform canvasRect, Camera uiCamera)
        {
            Camera worldCamera = Camera.main;
            bool visible = first != null && worldCamera != null && canvasRect != null
                && cursor != null && cursor.gameObject.activeInHierarchy;
            if (_bridgeLine != null) _bridgeLine.gameObject.SetActive(visible);
            if (!visible) return;

            Vector3 screenStart = worldCamera.WorldToScreenPoint(first.transform.position);
            if (screenStart.z <= 0f)
            {
                if (_bridgeLine != null) _bridgeLine.gameObject.SetActive(false);
                return;
            }
            Camera canvasCamera = canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null : uiCamera;
            Vector2 screenEnd = RectTransformUtility.WorldToScreenPoint(canvasCamera,
                cursor.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenStart, canvasCamera, out Vector2 start)
                || !RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenEnd, canvasCamera, out Vector2 end)) return;

            if (_bridgeLine == null)
            {
                var line = new GameObject("Bridge placement line", typeof(RectTransform), typeof(Image));
                _bridgeLine = line.GetComponent<RectTransform>();
                _bridgeLine.SetParent(canvasRect, false);
                _bridgeLine.SetAsFirstSibling();
                _bridgeLine.anchorMin = _bridgeLine.anchorMax = canvasRect.pivot;
                _bridgeLine.pivot = new Vector2(0f, 0.5f);
                Image graphic = line.GetComponent<Image>();
                graphic.raycastTarget = false;
                graphic.color = new Color(0.5f, 0.5f, 0.5f, 0.4f);
            }
            Vector2 direction = end - start;
            _bridgeLine.anchoredPosition = start;
            _bridgeLine.sizeDelta = new Vector2(direction.magnitude, 3f);
            _bridgeLine.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
        }

        public void Hide()
        {
            if (_bridgeLine != null) _bridgeLine.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            if (_bridgeLine != null) UnityEngine.Object.Destroy(_bridgeLine.gameObject);
        }
    }
}
