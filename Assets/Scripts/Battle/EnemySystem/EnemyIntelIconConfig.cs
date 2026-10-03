using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Intel Icon Config", fileName = "EnemyIntelIconConfig")]
    public class EnemyIntelIconConfig : ScriptableObject
    {
        [Serializable]
        private struct IconEntry
        {
            [SerializeField] private EnemyIntelFeatureType featureType;
            [SerializeField] private Sprite icon;

            public EnemyIntelFeatureType FeatureType => featureType;
            public Sprite Icon => icon;
        }

        [SerializeField] private Sprite defaultIcon;
        [SerializeField] private List<IconEntry> icons = new();

        private readonly Dictionary<EnemyIntelFeatureType, Sprite> _iconByFeature = new();
        private bool _isCacheBuilt;

        public Sprite GetIcon(EnemyIntelFeatureType featureType)
        {
            BuildCacheIfNeeded();

            if (_iconByFeature.TryGetValue(featureType, out Sprite icon) && icon != null)
            {
                return icon;
            }

            return defaultIcon;
        }

        public Sprite GetIcon(EnemyIntelFeature feature)
        {
            return GetIcon(feature.Type);
        }

        private void BuildCacheIfNeeded()
        {
            if (_isCacheBuilt)
            {
                return;
            }

            _iconByFeature.Clear();
            for (int i = 0; i < icons.Count; i++)
            {
                IconEntry entry = icons[i];
                if (entry.Icon == null)
                {
                    continue;
                }

                _iconByFeature[entry.FeatureType] = entry.Icon;
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
