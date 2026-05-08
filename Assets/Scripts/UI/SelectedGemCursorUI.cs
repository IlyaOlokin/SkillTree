using InventorySystem;
using SkillTree;
using UnityEngine;
using UnityEngine.EventSystems;
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
            if (_selectionState == null || !_selectionState.HasSelectedItem)
                return;

            if (Input.GetMouseButtonDown(1) && !IsPointerHandledElsewhere())
            {
                _selectionState.ClearSelection();
                return;
            }
            
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRectTransform,
                    (Vector2)Input.mousePosition + screenOffset,
                    UICamera,
                    out Vector2 localPoint))
                return;

            root.anchoredPosition = localPoint;
        }

        private void RefreshState()
        {
            InventoryItem selectedItem = _selectionState != null ? _selectionState.SelectedItem : null;
            bool hasSelectedItem = selectedItem != null && !selectedItem.IsEmpty;

            if (root != null)
                root.gameObject.SetActive(hasSelectedItem);

            if (!hasSelectedItem)
                return;

            if (iconImage != null)
                iconImage.sprite = selectedItem.Icon;
        }

        private static bool IsPointerHandledElsewhere()
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return true;

            Camera worldCamera = Camera.main;
            if (worldCamera == null)
                return false;

            Ray ray = worldCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.GetComponentInParent<Node>() != null)
                return true;

            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            return hit2D.collider != null && hit2D.collider.GetComponentInParent<Node>() != null;
        }
    }
}
