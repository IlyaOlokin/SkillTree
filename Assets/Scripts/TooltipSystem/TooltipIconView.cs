using UnityEngine;
using UnityEngine.UI;

namespace TooltipSystem
{
    public class TooltipIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image frameImage;

        private void Reset()
        {
            iconImage = GetComponent<Image>();
        }

        public void SetIcon(TooltipIconData iconData)
        {
            if (iconImage != null)
            {
                iconImage.sprite = iconData.Icon;
                iconImage.enabled = iconData.Icon != null;
            }

            if (frameImage != null)
            {
                frameImage.color = iconData.FrameColor;
            }
        }
    }
}
