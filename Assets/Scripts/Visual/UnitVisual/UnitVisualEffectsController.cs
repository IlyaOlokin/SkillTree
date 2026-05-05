using Battle;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TooltipSystem;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Visual
{
    [Serializable]
    public class UnitVisualEffectsController
    {
        private class EffectIconGroup
        {
            public readonly string Key;
            public readonly List<ActiveEffect> ActiveEffects = new List<ActiveEffect>();
            public UnitEffectIconView IconView;

            public EffectIconGroup(string key)
            {
                Key = key;
            }
        }

        [SerializeField] private RectTransform effectIconsRoot;
        [SerializeField] private UnitEffectIconView effectIconPrefab;
        [SerializeField] private Vector2 effectsIconsStartOffset;
        [SerializeField] private Vector2 effectIconsStep = new Vector2(72f, 0f);
        [SerializeField] private EffectIconsConfig effectIconsConfig;

        private readonly Dictionary<string, EffectIconGroup> _effectIconGroups = new Dictionary<string, EffectIconGroup>();
        private readonly List<EffectIconGroup> _orderedIconGroups = new List<EffectIconGroup>();
        private readonly List<string> _iconsToRemove = new List<string>();
        private TooltipUI _tooltipUI;

        public void Initialize(TooltipUI tooltipUI)
        {
            _tooltipUI = tooltipUI;
        }

        public void UpdateEffectIcons(Unit unit)
        {
            if (unit == null || unit.effectController == null || effectIconsRoot == null || effectIconPrefab == null)
            {
                ClearAllEffectIcons();
                return;
            }

            var activeEffects = unit.effectController.Effects;

            foreach (var pair in _effectIconGroups)
            {
                pair.Value.ActiveEffects.Clear();
            }

            _orderedIconGroups.Clear();

            for (int i = 0; i < activeEffects.Count; i++)
            {
                var activeEffect = activeEffects[i];
                string key = GetEffectIconGroupKey(activeEffect);
                if (!_effectIconGroups.TryGetValue(key, out var iconGroup))
                {
                    iconGroup = new EffectIconGroup(key);
                    iconGroup.IconView = CreateEffectIcon();
                    if (iconGroup.IconView == null)
                    {
                        continue;
                    }

                    _effectIconGroups.Add(key, iconGroup);
                }

                if (iconGroup.ActiveEffects.Count == 0)
                {
                    _orderedIconGroups.Add(iconGroup);
                }

                iconGroup.ActiveEffects.Add(activeEffect);
            }

            _iconsToRemove.Clear();
            foreach (var pair in _effectIconGroups)
            {
                if (pair.Value.ActiveEffects.Count == 0)
                {
                    _iconsToRemove.Add(pair.Key);
                }
            }

            for (int i = 0; i < _iconsToRemove.Count; i++)
            {
                RemoveEffectIcon(_iconsToRemove[i]);
            }

            for (int i = 0; i < _orderedIconGroups.Count; i++)
            {
                var iconGroup = _orderedIconGroups[i];
                UpdateEffectIcon(iconGroup);
                PositionEffectIcon(iconGroup.IconView, i);
            }
        }

        public void ClearAllEffectIcons()
        {
            foreach (var pair in _effectIconGroups)
            {
                if (pair.Value.IconView != null)
                {
                    Object.Destroy(pair.Value.IconView.gameObject);
                }
            }

            _effectIconGroups.Clear();
            _orderedIconGroups.Clear();
            _iconsToRemove.Clear();
        }

        private UnitEffectIconView CreateEffectIcon()
        {
            var iconView = Object.Instantiate(effectIconPrefab, effectIconsRoot);
            iconView.Initialize(_tooltipUI);
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

        private void UpdateEffectIcon(EffectIconGroup iconGroup)
        {
            if (iconGroup == null || iconGroup.IconView == null || iconGroup.ActiveEffects.Count == 0)
            {
                return;
            }

            BaseEffect effect = iconGroup.ActiveEffects[0].Effect;
            iconGroup.IconView.SetIcon(ResolveEffectIcon(effect));
            iconGroup.IconView.SetEffects(iconGroup.ActiveEffects);
            iconGroup.IconView.SetValueText(effect != null ? effect.GetIconText(iconGroup.ActiveEffects) : string.Empty);
            iconGroup.IconView.SetTimerProgress(effect != null ? effect.GetIconTimerProgress(iconGroup.ActiveEffects) : 1f);
        }

        private Sprite ResolveEffectIcon(BaseEffect effect)
        {
            if (effect == null || effectIconsConfig == null)
            {
                return null;
            }

            return effectIconsConfig.GetIcon(effect.VisualType);
        }

        private static string GetEffectIconGroupKey(ActiveEffect activeEffect)
        {
            if (activeEffect?.Effect == null)
            {
                return "null";
            }

            BaseEffect effect = activeEffect.Effect;
            if (effect.CanDisplayMultipleIcons)
            {
                return $"effect:{RuntimeHelpers.GetHashCode(activeEffect)}";
            }

            return $"group:{effect.GetIconDisplayKey()}";
        }

        private void RemoveEffectIcon(string key)
        {
            if (!_effectIconGroups.TryGetValue(key, out var iconGroup))
            {
                return;
            }

            if (iconGroup.IconView != null)
            {
                Object.Destroy(iconGroup.IconView.gameObject);
            }

            _effectIconGroups.Remove(key);
        }
    }
}
