using TMPro;
using UnityEngine;

namespace MenuTree
{
    public class MenuLimitedZoneVisual : MonoBehaviour
    {
        [SerializeField] private MenuLimitedZone limitedZone;
        [SerializeField] private TMP_Text text;

        private void Awake()
        {
            if (limitedZone != null)
                limitedZone.OnAllocatedCountChanged += UpdateText;

            UpdateText();
        }

        private void OnDestroy()
        {
            if (limitedZone != null)
                limitedZone.OnAllocatedCountChanged -= UpdateText;
        }

        private void UpdateText()
        {
            if (limitedZone == null || text == null)
                return;

            text.text = limitedZone.CurrentAllocatedCount + "/" + limitedZone.MaxAllocatedNode;
        }
    }
}
