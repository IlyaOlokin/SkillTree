using LocalizationSupport;
using System;
using System.Collections.Generic;
using TooltipSystem;
using UnityEngine;

namespace Battle
{
    public class EnemyIntelSource : MonoBehaviour, ITooltipDescriptionProvider, ITooltipIconProvider
    {
        [SerializeField] private EnemyUnit unit;
        [SerializeField] private EnemyIntelIconConfig iconConfig;
        [SerializeField] private Color strengthFrameColor = new Color(0.95f, 0.78f, 0.28f, 1f);
        [SerializeField] private Color weaknessFrameColor = new Color(0.42f, 0.72f, 1f, 1f);

        private readonly List<TooltipIconData> _tooltipIcons = new();
        private readonly List<string> _tooltipDescriptions = new();

        public EnemyUnit Unit => unit;

        private void Awake()
        {
            if (unit == null)
            {
                unit = GetComponentInParent<EnemyUnit>();
            }
        }

        public EnemyIntelData GetIntel()
        {
            return EnemyIntelProvider.Build(unit);
        }

        public string GetTooltipTitle()
        {
            return string.Empty;
        }

        public bool ShouldShowTooltipTitle()
        {
            return false;
        }

        public IReadOnlyList<string> GetTooltipDescriptions()
        {
            EnemyIntelData data = GetIntel();
            IReadOnlyList<string> affixKeys = data.AffixLocalizationKeys;
            if (affixKeys == null || affixKeys.Count == 0)
            {
                return Array.Empty<string>();
            }

            _tooltipDescriptions.Clear();
            for (int i = 0; i < affixKeys.Count; i++)
            {
                string localizedName = GetLocalizedAffixName(affixKeys[i]);
                if (!string.IsNullOrWhiteSpace(localizedName))
                {
                    _tooltipDescriptions.Add(localizedName);
                }
            }

            return _tooltipDescriptions;
        }

        public IReadOnlyList<TooltipIconData> GetTooltipIcons()
        {
            EnemyIntelData data = GetIntel();
            IReadOnlyList<EnemyIntelFeature> features = data.Features;
            if (features == null || features.Count == 0)
            {
                return Array.Empty<TooltipIconData>();
            }

            _tooltipIcons.Clear();
            for (int i = 0; i < features.Count; i++)
            {
                EnemyIntelFeature feature = features[i];
                Sprite icon = iconConfig != null ? iconConfig.GetIcon(feature) : null;
                _tooltipIcons.Add(new TooltipIconData(icon, GetFrameColor(feature)));
            }

            return _tooltipIcons;
        }

        public string GetLocalizedAffixName(string localizationKey)
        {
            return GameLocalization.GetEnemy(
                localizationKey,
                GameLocalization.HumanizeIdentifier(localizationKey));
        }

        private Color GetFrameColor(EnemyIntelFeature feature)
        {
            return feature.Disposition == EnemyIntelFeatureDisposition.Weakness
                ? weaknessFrameColor
                : strengthFrameColor;
        }
    }
}
