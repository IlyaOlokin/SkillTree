using TMPro;
using UnityEngine;

namespace TooltipSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TMP_Text))]
    [RequireComponent(typeof(TooltipTextLinkHandler))]
    public class TooltipLinkedText : MonoBehaviour
    {
        [SerializeField] private TooltipCanvasTarget canvasTarget = TooltipCanvasTarget.SkillTree;
        [SerializeField] private bool formatCurrentTextOnAwake = true;

        private TMP_Text textComponent;
        private TooltipTextLinkHandler linkHandler;
        private string rawText;

        public string RawText => rawText;
        public TMP_Text TextComponent => textComponent;

        private void Awake()
        {
            textComponent = GetComponent<TMP_Text>();
            linkHandler = GetComponent<TooltipTextLinkHandler>();

            textComponent.raycastTarget = true;
            linkHandler.Initialize(ResolveTooltipUI(), 0, true, canvasTarget);

            if (formatCurrentTextOnAwake)
            {
                SetText(textComponent.text);
            }
        }

        public void SetText(string value)
        {
            rawText = value ?? string.Empty;
            EnsureComponents();
            textComponent.text = TooltipTextLinkFormatter.Format(rawText);
        }

        private void EnsureComponents()
        {
            textComponent ??= GetComponent<TMP_Text>();
            linkHandler ??= GetComponent<TooltipTextLinkHandler>();
        }

        private static TooltipUI ResolveTooltipUI()
        {
            return FindFirstObjectByType<TooltipUI>(FindObjectsInactive.Include);
        }
    }
}
