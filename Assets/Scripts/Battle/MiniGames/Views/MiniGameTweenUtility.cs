using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    internal static class MiniGameTweenUtility
    {
        public static void Kill(ref Tween tween)
        {
            if (tween == null)
            {
                return;
            }

            tween.Kill();
            tween = null;
        }

        public static void Kill(ref Sequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            sequence.Kill();
            sequence = null;
        }

        public static void SetAlpha(IReadOnlyList<Graphic> graphics, float alpha)
        {
            if (graphics == null)
            {
                return;
            }

            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color color = graphic.color;
                color.a = alpha;
                graphic.color = color;
            }
        }

        public static Sequence FadeTo(IReadOnlyList<Graphic> graphics, float alpha, float duration)
        {
            Sequence sequence = DOTween.Sequence();

            if (graphics == null)
            {
                return sequence;
            }

            for (int i = 0; i < graphics.Count; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                sequence.Join(graphic.DOFade(alpha, duration));
            }

            return sequence;
        }
    }
}
