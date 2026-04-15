using Gems;
using InventorySystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class SelectedGemCursorUI : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Vector2 screenOffset = new(18f, -18f);
        [SerializeField] private Image iconImage;

        [Inject] private InventorySelectionState _selectionState;

        [SerializeField] private Camera UICamera;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private RectTransform canvasRectTransform;

        private void Start()
        {
            if (_selectionState != null)
                _selectionState.OnSelectionChanged += RefreshState;

            RefreshState();
        }

        private void OnDestroy()
        {
            if (_selectionState != null)
                _selectionState.OnSelectionChanged -= RefreshState;
        }

        private void Update()
        {
            if (_selectionState == null || !_selectionState.HasSelectedGem)
                return;
            
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    (Vector2)Input.mousePosition + screenOffset,
                    UICamera,
                    out Vector2 localPoint))
                return;

            root.anchoredPosition = localPoint;
        }

        private void LateUpdate()
        {
            if (_selectionState == null || !_selectionState.HasSelectedGem)
                return;

            if (!Input.GetMouseButtonDown(1))
                return;

            _selectionState.ClearSelection();
        }

        private void RefreshState()
        {
            GemInstance selectedGem = _selectionState != null ? _selectionState.SelectedGem : null;
            bool hasGem = selectedGem != null;

            if (root != null)
                root.gameObject.SetActive(hasGem);

            if (!hasGem)
                return;

            if (iconImage != null)
                iconImage.sprite = selectedGem.Icon;
        }
    }
}
