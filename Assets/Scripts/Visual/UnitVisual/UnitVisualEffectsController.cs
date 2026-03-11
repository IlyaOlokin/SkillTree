using Battle;
using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Visual
{
    [Serializable]
    public class UnitVisualEffectsController
    {
        [SerializeField] private RectTransform effectIconsRoot;
        [SerializeField] private UnitEffectIconView effectIconPrefab;
        [SerializeField] private Vector2 effectsIconsStartOffset;
        [SerializeField] private Vector2 effectIconsStep = new Vector2(72f, 0f);
        [SerializeField] private EffectIconsConfig effectIconsConfig;

        private readonly Dictionary<ActiveEffect, UnitEffectIconView> _effectIcons = new Dictionary<ActiveEffect, UnitEffectIconView>();
        private readonly List<ActiveEffect> _iconsToRemove = new List<ActiveEffect>();

        public void UpdateEffectIcons(Unit unit)
        {
            if (unit == null || unit.effectController == null || effectIconsRoot == null || effectIconPrefab == null)
            {
                ClearAllEffectIcons();
                return;
            }

            var activeEffects = unit.effectController.Effects;

            _iconsToRemove.Clear();
            foreach (var pair in _effectIcons)
            {
                if (!activeEffects.Contains(pair.Key))
                {
                    _iconsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < _iconsToRemove.Count; i++)
            {
                RemoveEffectIcon(_iconsToRemove[i]);
            }

            for (int i = 0; i < activeEffects.Count; i++)
            {
                var activeEffect = activeEffects[i];
                if (!_effectIcons.TryGetValue(activeEffect, out var iconView))
                {
                    iconView = CreateEffectIcon(activeEffect);
                    if (iconView == null)
                    {
                        continue;
                    }

                    _effectIcons.Add(activeEffect, iconView);
                }

                UpdateIconTimer(activeEffect, iconView);
                PositionEffectIcon(iconView, i);
            }
        }

        public void ClearAllEffectIcons()
        {
            foreach (var pair in _effectIcons)
            {
                if (pair.Value != null)
                {
                    Object.Destroy(pair.Value.gameObject);
                }
            }

            _effectIcons.Clear();
            _iconsToRemove.Clear();
        }

        private UnitEffectIconView CreateEffectIcon(ActiveEffect activeEffect)
        {
            var iconView = Object.Instantiate(effectIconPrefab, effectIconsRoot);
            iconView.RectTransform.localScale = effectIconsRoot.localScale;
            iconView.SetIcon(ResolveEffectIcon(activeEffect.Effect));
            return iconView;
        }

        private void PositionEffectIcon(UnitEffectIconView iconView, int index)
        {
            if (iconView == null || iconView.RectTransform == null)
            {
                return;
            }

            iconView.RectTransform.anchoredPosition = effectsIconsStartOffset + effectIconsStep * index;
        }

        private static void UpdateIconTimer(ActiveEffect activeEffect, UnitEffectIconView iconView)
        {
            float duration = activeEffect.Effect.Duration;
            if (duration > 0f)
            {
                iconView.SetTimerProgress(activeEffect.TimeLeft / duration);
                return;
            }

            iconView.SetTimerProgress(1f);
        }

        private Sprite ResolveEffectIcon(BaseEffect effect)
        {
            if (effect == null || effectIconsConfig == null)
            {
                return null;
            }

            return effectIconsConfig.GetIcon(effect.VisualType);
        }

        private void RemoveEffectIcon(ActiveEffect activeEffect)
        {
            if (!_effectIcons.TryGetValue(activeEffect, out var iconView))
            {
                return;
            }

            if (iconView != null)
            {
                Object.Destroy(iconView.gameObject);
            }

            _effectIcons.Remove(activeEffect);
        }
    }
}
