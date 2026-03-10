using Battle;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Visual
{
    [CreateAssetMenu(menuName = "Visual/Effect Icons Config", fileName = "EffectIconsConfig")]
    public class EffectIconsConfig : ScriptableObject
    {
        [Serializable]
        private struct EffectIconEntry
        {
            [SerializeField] private EffectVisualType effectType;
            [SerializeField] private Sprite icon;

            public EffectVisualType EffectType => effectType;
            public Sprite Icon => icon;
        }

        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private List<EffectIconEntry> icons = new List<EffectIconEntry>();

        private readonly Dictionary<EffectVisualType, Sprite> _iconByType = new Dictionary<EffectVisualType, Sprite>();
        private bool _isCacheBuilt;

        public Sprite GetIcon(EffectVisualType effectType)
        {
            BuildCacheIfNeeded();

            if (effectType != EffectVisualType.None &&
                _iconByType.TryGetValue(effectType, out var icon) &&
                icon != null)
            {
                return icon;
            }

            return defaultIcon;
        }

        private void BuildCacheIfNeeded()
        {
            if (_isCacheBuilt)
            {
                return;
            }

            _iconByType.Clear();

            for (int i = 0; i < icons.Count; i++)
            {
                var entry = icons[i];
                if (entry.EffectType == EffectVisualType.None || entry.Icon == null)
                {
                    continue;
                }

                _iconByType[entry.EffectType] = entry.Icon;
            }

            _isCacheBuilt = true;
        }

        private void OnEnable()
        {
            _isCacheBuilt = false;
        }

        private void OnValidate()
        {
            _isCacheBuilt = false;
        }
    }
}
