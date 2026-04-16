using TMPro;
using LocalizationSupport;
using TooltipSystem;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

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
            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (bonusZone != null)
                bonusZone.OnAllocatedCountChanged -= UpdateText;

            if (LocalizationSettings.HasSettings)
                LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void UpdateText()
        {
            string bonusZoneText = GameLocalization.Format(
                "ui.bonusZone.description",
                "Gain [[0]] for every allocated node in this zone\nAllocated: [[1]]",
                bonusZone.GetCurrentModifierDescription(),
                bonusZone.AllocatedNodesCount);
            if (tooltipLinkedText != null)
            {
                tooltipLinkedText.SetText(bonusZoneText);
                return;
            }

            text.text = TooltipTextLinkFormatter.Format(bonusZoneText);
        }

        private void HandleLocaleChanged(Locale _)
        {
            UpdateText();
        }
    }

}
