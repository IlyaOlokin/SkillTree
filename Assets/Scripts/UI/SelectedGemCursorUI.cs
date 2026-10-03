using InventorySystem;
using SkillTree;
using TMPro;
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
        [SerializeField] private TMP_Text stackCountText;

        [Inject] private InventorySelectionState _selectionState;
        [Inject] private GemPlacementService _placement;
        private readonly BridgePlacementLine _bridgeLine = new();

        [SerializeField] private Camera UICamera;
        [SerializeField] private Canvas parentCanvas;
        [SerializeField] private RectTransform canvasRectTransform;

        private void Start()
        {
            if (_placement != null) _placement.OnPlacementChanged += RefreshState;
            if (_selectionState != null)
                _selectionState.OnSelectionChanged += RefreshState;

            RefreshState();
        }

        private void OnDestroy()
        {
            _bridgeLine.Dispose();
            if (_placement != null) _placement.OnPlacementChanged -= RefreshState;
            if (_selectionState != null)
                _selectionState.OnSelectionChanged -= RefreshState;
        }

        private void Update()
        {
            if (_placement != null && _placement.TryCancelPlacementInput()) return;
            if (_selectionState == null || !_selectionState.HasSelectedItem)
                return;

            if (Input.GetMouseButtonDown(1) && !IsPointerHandledElsewhere())
            {
                _placement.ClearSelection();
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

        private void LateUpdate()
        {
            _bridgeLine.Draw(_placement?.PendingBridgeSocket,
                iconImage != null ? iconImage.rectTransform : root, parentCanvas, canvasRectTransform, UICamera);
        }

        private void OnDisable() => _bridgeLine.Hide();

        private void RefreshState()
        {
            InventoryItem selectedItem = _selectionState != null ? _selectionState.SelectedItem : null;
            bool hasSelectedItem = selectedItem != null && !selectedItem.IsEmpty;

            if (root != null)
                root.gameObject.SetActive(hasSelectedItem);

            if (iconImage != null) iconImage.enabled = hasSelectedItem;

            if (!hasSelectedItem)
            {
                RefreshStackCount(null);
                return;
            }

            if (iconImage != null)
                iconImage.sprite = selectedItem.Icon;

            RefreshStackCount(selectedItem);
        }

        private void RefreshStackCount(InventoryItem selectedItem)
        {
            if (stackCountText == null)
                return;

            int stackCount = selectedItem?.StackCount ?? 0;
            bool secondEnd = _placement?.IsPlacingBridge == true;
            bool shouldShowStackCount = !secondEnd && stackCount > 1;
            stackCountText.gameObject.SetActive(shouldShowStackCount);
            stackCountText.text = shouldShowStackCount ? stackCount.ToString() : string.Empty;
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
