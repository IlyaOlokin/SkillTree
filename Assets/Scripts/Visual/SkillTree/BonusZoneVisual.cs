using TMPro;
using TooltipSystem;
using UnityEngine;

namespace SkillTree
{
    public class BonusZoneVisual : MonoBehaviour
    {
        [SerializeField] private BonusZone bonusZone;
        [SerializeField] private TMP_Text text;

        private TooltipLinkedText tooltipLinkedText;

        private void Awake()
        {
            if (text != null)
            {
                tooltipLinkedText = text.GetComponent<TooltipLinkedText>();
                if (tooltipLinkedText == null)
                {
                    tooltipLinkedText = text.gameObject.AddComponent<TooltipLinkedText>();
                }
            }

            bonusZone.OnAllocatedCountChanged += UpdateText;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (bonusZone != null)
                bonusZone.OnAllocatedCountChanged -= UpdateText;
        }

        private void UpdateText()
        {
            string bonusZoneText =
                $"Gain {bonusZone.GetCurrentModifierDescription()} for every allocated node in this zone\nAllocated: {bonusZone.AllocatedNodesCount}";
            if (tooltipLinkedText != null)
            {
                tooltipLinkedText.SetText(bonusZoneText);
                return;
            }

            text.text = TooltipTextLinkFormatter.Format(bonusZoneText);
        }
    }

}
