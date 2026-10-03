using System.Collections.Generic;
using UnityEngine;

namespace TooltipSystem
{
    public readonly struct TooltipIconData
    {
        public TooltipIconData(Sprite icon, Color frameColor)
        {
            Icon = icon;
            FrameColor = frameColor;
        }

        public Sprite Icon { get; }
        public Color FrameColor { get; }
    }

    public interface ITooltipIconProvider
    {
        IReadOnlyList<TooltipIconData> GetTooltipIcons();
    }
}
