using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    public class TooltipIconStrip : MonoBehaviour
    {
        [SerializeField] private RectTransform iconParent;
        [SerializeField] private TooltipIconView iconPrefab;

        private readonly List<TooltipIconView> _iconViews = new();

        private void Awake()
        {
            if (iconParent == null)
            {
                iconParent = transform as RectTransform;
            }
        }

        public void SetIcons(IReadOnlyList<TooltipIconData> icons)
        {
            int iconCount = icons != null ? icons.Count : 0;
            EnsureIconCount(iconCount);
            gameObject.SetActive(iconCount > 0);

            for (int i = 0; i < _iconViews.Count; i++)
            {
                bool shouldBeActive = i < iconCount;
                TooltipIconView iconView = _iconViews[i];
                iconView.gameObject.SetActive(shouldBeActive);

                if (shouldBeActive)
                {
                    iconView.SetIcon(icons[i]);
                }
            }
        }

        private void EnsureIconCount(int count)
        {
            if (iconPrefab == null || iconParent == null)
            {
                return;
            }

            for (int i = _iconViews.Count; i < count; i++)
            {
                TooltipIconView iconView = Instantiate(iconPrefab, iconParent);
                iconView.gameObject.SetActive(false);
                _iconViews.Add(iconView);
            }
        }
    }
}
