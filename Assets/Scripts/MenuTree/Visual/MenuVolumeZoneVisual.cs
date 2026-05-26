using LocalizationSupport;
using TMPro;
using TooltipSystem;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace MenuTree
{
    public class MenuVolumeZoneVisual : MonoBehaviour
    {
        [SerializeField] private MenuVolumeZone volumeZone;
        [SerializeField] private TMP_Text text;

        private TooltipLinkedText tooltipLinkedText;

        private void Awake()
        {
            if (text != null)
            {
                tooltipLinkedText = text.GetComponent<TooltipLinkedText>();
                if (tooltipLinkedText == null)
                    tooltipLinkedText = text.gameObject.AddComponent<TooltipLinkedText>();
            }

            if (volumeZone != null)
                volumeZone.OnAllocatedCountChanged += UpdateText;

            LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
            UpdateText();
        }

        private void OnDestroy()
        {
            if (volumeZone != null)
                volumeZone.OnAllocatedCountChanged -= UpdateText;

            if (LocalizationSettings.HasSettings)
                LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        private void UpdateText()
        {
            if (volumeZone == null || text == null)
                return;

            string volumeText = GameLocalization.FormatGameUI(
                "ui.volumeZone.description",
                "Gain [[0]] [[1]] for every allocated node in this zone\nCurrent [[1]]: [[2]]",
                FormatPercent(MenuVolumeZone.VolumePerAllocatedNode),
                GetVolumeTargetName(),
                FormatPercent(volumeZone.CurrentVolume));

            if (tooltipLinkedText != null)
            {
                tooltipLinkedText.SetText(volumeText);
                return;
            }

            text.text = TooltipTextLinkFormatter.Format(volumeText);
        }

        private static string FormatPercent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private string GetVolumeTargetName()
        {
            return volumeZone.VolumeTarget switch
            {
                MenuVolumeTarget.Master => "Master Volume",
                MenuVolumeTarget.Sfx => "SFX Volume",
                MenuVolumeTarget.Music => "Music Volume",
                _ => "volume"
            };
        }

        private void HandleLocaleChanged(Locale _)
        {
            UpdateText();
        }
    }
}
