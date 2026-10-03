using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Battle.MiniGames
{
    internal static class MiniGameTweenUtility
    {
        public static Tween EmptyTween(bool useUnscaledTime)
        {
            return DOVirtual.DelayedCall(0f, () => { }, useUnscaledTime);
        }

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

        public static Tween BuildPulseTween(
            RectTransform target,
            Vector3 initialScale,
            float scaleMultiplier,
            float scaleInDuration,
            float scaleOutDuration,
            int count,
            Ease scaleInEase,
            Ease scaleOutEase,
            bool useUnscaledTime)
        {
            if (target == null)
            {
                return EmptyTween(useUnscaledTime);
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
            for (int i = 0; i < Mathf.Max(1, count); i++)
            {
                sequence.Append(target
                    .DOScale(initialScale * Mathf.Max(0f, scaleMultiplier), scaleInDuration)
                    .SetEase(scaleInEase));
                sequence.Append(target
                    .DOScale(initialScale, scaleOutDuration)
                    .SetEase(scaleOutEase));
            }

            return sequence;
        }

        public static Tween BuildMoveTween(
            RectTransform target,
            Vector2 targetPosition,
            float duration,
            Ease ease,
            bool useUnscaledTime)
        {
            if (target == null)
            {
                return EmptyTween(useUnscaledTime);
            }

            return target
                .DOAnchorPos(targetPosition, duration)
                .SetEase(ease)
                .SetUpdate(useUnscaledTime);
        }

        public static Tween BuildResultIconTween(
            GameObject iconRoot,
            RectTransform iconTransform,
            float showDuration,
            float holdDuration,
            float hideDuration,
            Ease showEase,
            Ease hideEase,
            bool useUnscaledTime)
        {
            if (iconRoot == null || iconTransform == null)
            {
                return EmptyTween(useUnscaledTime);
            }

            iconRoot.SetActive(true);
            iconTransform.localScale = Vector3.zero;

            Sequence sequence = DOTween.Sequence().SetUpdate(useUnscaledTime);
            sequence.Append(iconTransform
                .DOScale(Vector3.one, showDuration)
                .SetEase(showEase));
            sequence.AppendInterval(holdDuration);
            sequence.Append(iconTransform
                .DOScale(Vector3.zero, hideDuration)
                .SetEase(hideEase));
            sequence.OnComplete(() => iconRoot.SetActive(false));
            return sequence;
        }
    }
}
