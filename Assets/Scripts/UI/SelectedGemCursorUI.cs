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
        [SerializeField] private TMP_Text nameText;

        [Inject] private InventorySelectionState _selectionState;

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
            if (root == null || _selectionState == null || !_selectionState.HasSelectedGem)
                return;

            root.position = (Vector2)Input.mousePosition + screenOffset;
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

            if (nameText != null)
                nameText.text = selectedGem.DisplayName;
        }
    }
}
