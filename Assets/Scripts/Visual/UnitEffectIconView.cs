using UnityEngine;
using UnityEngine.UI;

namespace Visual
{
    public class UnitEffectIconView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image borderTimerImage;

        public RectTransform RectTransform { get; private set; }

        private void Awake()
        {
            RectTransform = transform as RectTransform;
        }

        public void SetIcon(Sprite sprite)
        {
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
            }
        }

        public void SetTimerProgress(float normalizedTimeLeft)
        {
            if (borderTimerImage != null)
            {
                borderTimerImage.fillAmount = Mathf.Clamp01(normalizedTimeLeft);
            }
        }
    }
}
