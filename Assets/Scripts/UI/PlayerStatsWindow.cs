using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Battle;
using LocalizationSupport;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Zenject;

public class PlayerStatsWindow : MonoBehaviour
{
    [Inject] private PlayerUnit _player;
    [Inject(Optional = true)] private EnemySpawner _enemySpawner;
    [SerializeField] private TMP_Text lvlText;
    [SerializeField] private TMP_Text DPSText;
    [SerializeField] private TMP_Text DamageText;
    [SerializeField] private TMP_Text AttackSpeedText;
    [SerializeField] private TMP_Text CritChanceText;
    [SerializeField] private TMP_Text CritDamageBonusText;
    [SerializeField] private List<StatText> statTexts;
    
    [Header("Mystic")]
    [SerializeField] private TMP_Text MysticLabelText;
    [SerializeField] private TMP_Text MysticValueText;
    [SerializeField] private MysticColorsConfig mysticColorsConfig;
    private bool _isSubscribed;
    [Header("Scene Layout")]
    [SerializeField] private List<PlayerStatsSection> sections = new();
    [SerializeField] private List<PlayerStatsHeading> subsectionHeadings = new();
    [SerializeField] private bool previewAllSections = true;
    [SerializeField] private GameObject _parryGroup;
    [SerializeField] private RectTransform _layoutContent;
    [SerializeField] private UnityEngine.UI.Toggle showAllStatsToggle;
    [SerializeField] private TMP_Text showAllStatsLabel;

    private void Awake()
    {
        ApplySectionVisibility(false);
    }
    
    private void Start()
    {
        Subscribe();
        RefreshTexts();
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshTexts();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RefreshEditorPreview;
#endif
    }

    private void Subscribe()
    {
        if (_isSubscribed || _player == null)
            return;

        _player.OnStatsRecalculated += UpdateTexts;
        if (_player.UnitLevel != null)
        {
            _player.UnitLevel.OnLevelUp += UpdateLevelText;
        }

        if (_enemySpawner != null)
        {
            _enemySpawner.OnWaveCleared += UpdateTexts;
            _enemySpawner.OnLevelChanged += UpdateTexts;
            _enemySpawner.OnLocationChanged += UpdateTexts;
            _enemySpawner.OnBattleActivityChanged += HandleBattleActivityChanged;
        }

        LocalizationSettings.SelectedLocaleChanged += HandleLocaleChanged;
        _isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_isSubscribed)
            return;

        if (_player != null)
        {
            _player.OnStatsRecalculated -= UpdateTexts;
            if (_player.UnitLevel != null)
            {
                _player.UnitLevel.OnLevelUp -= UpdateLevelText;
            }
        }

        if (_enemySpawner != null)
        {
            _enemySpawner.OnWaveCleared -= UpdateTexts;
            _enemySpawner.OnLevelChanged -= UpdateTexts;
            _enemySpawner.OnLocationChanged -= UpdateTexts;
            _enemySpawner.OnBattleActivityChanged -= HandleBattleActivityChanged;
        }

        if (LocalizationSettings.HasSettings)
        {
            LocalizationSettings.SelectedLocaleChanged -= HandleLocaleChanged;
        }

        _isSubscribed = false;
    }

    private void RefreshTexts()
    {
        if (_player == null || _player.BaseUnitModifiers == null || _player.UnitLevel == null)
            return;

        _player.RequestModRecalculation();
        UpdateTexts();
        UpdateLevelText(_player.UnitLevel.Level);
    }

    private void UpdateLevelText(int level)
    {
        if (_player == null || _player.UnitLevel == null)
            return;

        lvlText.text = GameLocalization.Format(
            "ui.playerStats.level",
            "Level: [[0]]",
            _player.UnitLevel.Level);
    }
   
    private void UpdateTexts()
    {
        if (_player == null || _player.BaseUnitModifiers == null)
            return;

        UpdateDamageTexts();
        UpdateMysticTexts();
        RefreshSectionTitles();
        

        foreach (var statText in statTexts)
        {
            _player.BaseUnitModifiers.TryGetStatValue(statText.stat, out float rawValue);
            
            bool isPercent = StatTypeDisplayRules.IsPercentStat(statText.stat);
            float displayValue = StatTypeDisplayRules.GetDisplayValue(statText.stat, rawValue, _player.BaseUnitModifiers.GetStatValue);
            string prefix = GetStatPrefix(statText.stat);
            string suffix = GetStatSuffix(statText.stat, rawValue);
            string label = statText.needToOverrideText
                ? GameLocalization.LocalizeValueOrKey(GameLocalization.ContentTable, statText.overrideText)
                : GameLocalization.LocalizeEnum(statText.stat);
            statText.labelText.text = $"{label}:";
            statText.valueText.text = FormatStatValue(displayValue, isPercent, prefix, suffix, StatTypeDisplayRules.GetDecimalPlaces(statText.stat));
        }
        UpdateConditionalVisibility(_player.BaseUnitModifiers.GetStatValue);
    }
    
    private string FormatStatValue(float value, bool isPercent, string prefix = "", string suffix = "", int decimalPlaces = 1)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) return "—";
        bool isDoubleDigit = Mathf.Abs(value) >= 10f;
        float multiplier = Mathf.Pow(10f, decimalPlaces);
        float roundedValue = isDoubleDigit && decimalPlaces <= 1
            ? Mathf.Round(value)
            : Mathf.Round(value * multiplier) / multiplier;

        string numberText = roundedValue.ToString(
            isDoubleDigit && decimalPlaces <= 1 ? "0" : $"0.{new string('#', decimalPlaces)}",
            CultureInfo.InvariantCulture);

        return isPercent ? $"{prefix}{numberText}%{suffix}" : $"{prefix}{numberText}{suffix}";
    }

    private void UpdateDamageTexts()
    {
        float damage = CalculateHitDamage();
        float attackSpeed = _player.BaseUnitModifiers.GetStatValue(StatType.AttackSpeed);
        float critChance = _player.BaseUnitModifiers.GetStatValue(StatType.CritChance);
        float critDamageBonus = _player.BaseUnitModifiers.GetStatValue(StatType.CritDamageBonus);
        bool allowsMultiCrit = false;
        foreach (var mod in _player.GetAllModifiers())
        {
            if (mod.Modifier is SkillTree.MultiCritModifier
                && mod.IsInPriority(ModifierPriority.OnAttack) && mod.IsApplicable(_player))
            {
                allowsMultiCrit = true;
                break;
            }
        }
        float expectedCritLayers = DamageCalculator.GetExpectedCriticalLayerCount(critChance, allowsMultiCrit);
        DPSText.text = $"{FormatStatValue(damage * Mathf.Max(0f, attackSpeed) * (1 + expectedCritLayers * critDamageBonus), false)}";
        DamageText.text = $"{FormatStatValue(damage, false)}";
        AttackSpeedText.text = $"{FormatStatValue(attackSpeed, false, decimalPlaces: StatTypeDisplayRules.GetDecimalPlaces(StatType.AttackSpeed))}";
        CritChanceText.text = $"{FormatStatValue(StatTypeDisplayRules.GetDisplayValue(StatType.CritChance, critChance), true)}";
        CritDamageBonusText.text = $"{FormatStatValue(StatTypeDisplayRules.GetDisplayValue(StatType.CritDamageBonus, critDamageBonus), true)}";
    }

    private float CalculateHitDamage()
    {
        float physicalDamage = _player.BaseUnitModifiers.GetStatValue(StatType.PhysicalDamage);
        float fireDamage = _player.BaseUnitModifiers.GetStatValue(StatType.FireDamage);
        float coldDamage = _player.BaseUnitModifiers.GetStatValue(StatType.ColdDamage);
        float lightningDamage = _player.BaseUnitModifiers.GetStatValue(StatType.LightningDamage);

        return physicalDamage + fireDamage + coldDamage + lightningDamage;
    }

    private void UpdateMysticTexts()
    {
        float lightDamage = _player.BaseUnitModifiers.GetStatValue(StatType.LightDamage);
        float darknessDamage = _player.BaseUnitModifiers.GetStatValue(StatType.DarknessDamage);
        float signedMysticDamage = lightDamage - darknessDamage;
        
        if (MysticLabelText != null)
        {
            MysticLabelText.text = GameLocalization.Get("ui.playerStats.mystic", "Mystic");
        }

        if (MysticValueText != null)
        {
            MysticValueText.text = FormatStatValue(Mathf.Abs(signedMysticDamage), false);
        }

        Color textColor = GetMysticColor(signedMysticDamage);
        if (MysticLabelText != null)
        {
            MysticLabelText.color = textColor;
        }

        if (MysticValueText != null)
        {
            MysticValueText.color = textColor;
        }
    }

    private Color GetMysticColor(float signedMysticDamage)
    {
        if (signedMysticDamage > 0f)
        {
            return mysticColorsConfig.LightColor;
        }

        if (signedMysticDamage < 0f)
        {
            return mysticColorsConfig.DarknessColor;
        }

        return mysticColorsConfig.NeutralColor;
    }
    
    private string GetStatSuffix(StatType stat, float rawValue)
    {
        if (StatTypeDisplayRules.TryGetMaximumResistanceStat(stat, out StatType maximumResistanceStat))
        {
            return GetUncappedResistanceSuffix(rawValue, maximumResistanceStat);
        }

        switch (stat)
        {
            case StatType.Armor:
                return GetEstimatedArmorMitigationSuffix(rawValue);
            case StatType.Evasion:
                return GetEstimatedEvasionChanceSuffix(rawValue);
            case StatType.Accuracy:
                return GetEstimatedAccuracyHitChanceSuffix(rawValue);
            case StatType.BarrierRegenerationSpeed: 
                return GameLocalization.Get("ui.common.secondsShort", "s");
            default:
                return string.Empty;
        }
    }

    private string GetStatPrefix(StatType stat)
    {
        return StatTypeDisplayRules.UsesExplicitPositivePrefix(stat) ? "+" : string.Empty;
    }

    private string GetUncappedResistanceSuffix(float rawValue, StatType maxResistanceStat)
    {
        float maxValue = _player.BaseUnitModifiers.GetStatValue(maxResistanceStat);
        if (rawValue <= maxValue)
            return string.Empty;

        rawValue *= 100f;
        return GameLocalization.Format(
            "ui.playerStats.uncappedValueSuffix",
            " ([[0]])",
            FormatStatValue(rawValue, true));
    }

    private string GetEstimatedArmorMitigationSuffix(float armor)
    {
        if (_enemySpawner == null ||
            _enemySpawner.TryGetActiveWaveNormalEnemyEstimates(out _, out float physicalDamage) == false)
            return string.Empty;

        float mitigationPercent = Armor.CalculatePhysicalMitigation(armor, physicalDamage) * 100f;
        return FormatEstimatedPercentSuffix(mitigationPercent);
    }

    private string GetEstimatedEvasionChanceSuffix(float evasion)
    {
        if (_enemySpawner == null ||
            _enemySpawner.TryGetActiveWaveNormalEnemyEstimates(out float accuracy, out _) == false)
            return string.Empty;

        float dodgeChancePercent = Evasion.CalculateDodgeChance(evasion, accuracy) * 100f;
        return FormatEstimatedPercentSuffix(dodgeChancePercent);
    }

    private string GetEstimatedAccuracyHitChanceSuffix(float accuracy)
    {
        if (_enemySpawner == null ||
            _enemySpawner.TryGetActiveWaveNormalEnemyBaseEvasionEstimate(out float evasion) == false)
            return string.Empty;

        float hitChancePercent = Evasion.CalculateHitChance(accuracy, evasion) * 100f;
        return FormatEstimatedPercentSuffix(hitChancePercent);
    }

    private string FormatEstimatedPercentSuffix(float value)
    {
        return GameLocalization.Format(
            "ui.playerStats.estimatedPercentSuffix",
            " ([[0]])",
            FormatStatValue(value, true));
    }

    private void HandleLocaleChanged(Locale _)
    {
        RefreshTexts();
    }

    public void ToggleSection(int index)
    {
        if (index < 0 || index >= sections.Count)
            return;
        var section = sections[index];
        section.expanded = !section.expanded;
        ApplySectionVisibility(false);
        var scroll = _layoutContent != null
            ? _layoutContent.GetComponentInParent<UnityEngine.UI.ScrollRect>() : null;
        if (scroll != null)
            scroll.StopMovement();
    }

    public void SetShowAllStats(bool show)
    {
        if (showAllStatsToggle != null)
            showAllStatsToggle.SetIsOnWithoutNotify(show);
        if (_player != null && _player.BaseUnitModifiers != null)
            UpdateConditionalVisibility(_player.BaseUnitModifiers.GetStatValue);
    }

    private void ApplySectionVisibility(bool previewAll)
    {
        foreach (var section in sections)
        {
            bool expanded = previewAll || section.expanded;
            if (section.body != null)
                section.body.SetActive(expanded);
            if (section.arrowText != null)
                section.arrowText.text = expanded ? "▼" : "▶";
        }
        if (previewAll)
        {
            foreach (var stat in statTexts)
                if (stat.hideWhenDefault && stat.rowRoot != null)
                    stat.rowRoot.SetActive(true);
            foreach (var section in sections)
            {
                if (section.root != null) section.root.SetActive(true);
                if (section.separator != null) section.separator.SetActive(true);
            }
        }
        else if (_player != null && _player.BaseUnitModifiers != null)
            UpdateConditionalVisibility(_player.BaseUnitModifiers.GetStatValue);
        if (_layoutContent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutContent);
    }

    private void UpdateConditionalVisibility(Func<StatType, float> getValue)
    {
        bool showAll = showAllStatsToggle != null && showAllStatsToggle.isOn;
        bool changed = false;
        void SetVisible(GameObject target, bool visible)
        {
            if (target == null || target.activeSelf == visible) return;
            target.SetActive(visible);
            changed = true;
        }

        SetVisible(_parryGroup, showAll || getValue(StatType.ParryChance) > 0f);
        foreach (var stat in statTexts)
            if (stat.hideWhenDefault)
                SetVisible(stat.rowRoot, showAll || !Mathf.Approximately(getValue(stat.stat), 0f));
        foreach (var section in sections)
        {
            if (!section.hideWhenDefault) continue;
            bool visible = showAll;
            foreach (var stat in section.visibilityStats)
                if (!Mathf.Approximately(getValue(stat), section.defaultValue))
                {
                    visible = true;
                    break;
                }
            SetVisible(section.root, visible);
            SetVisible(section.separator, visible);
        }
        for (int i = 0; i < sections.Count; i++)
        {
            var parent = sections[i];
            if (!parent.hideWhenChildrenEmpty) continue;
            bool visible = false;
            foreach (var child in sections)
                if (child.parentSectionIndex == i && child.root != null && child.root.activeSelf)
                {
                    visible = true;
                    break;
                }
            // A section may also contain rows outside its nested dropdowns.
            if (!visible && parent.body != null)
                foreach (var stat in statTexts)
                    if (stat.rowRoot != null && stat.rowRoot.transform.parent == parent.body.transform
                        && stat.rowRoot.activeSelf)
                    {
                        visible = true;
                        break;
                    }
            SetVisible(parent.root, visible);
            SetVisible(parent.separator, visible);
        }
        if (changed && _layoutContent != null)
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_layoutContent);
    }

    private void RefreshSectionTitles()
    {
        if (showAllStatsLabel != null)
            showAllStatsLabel.text = GameLocalization.Get("ui.playerStats.showAllStats", "Show all stats");
        foreach (var section in sections)
            if (section.titleText != null)
                section.titleText.text = GameLocalization.Get(section.localizationKey, section.fallback);
        foreach (var heading in subsectionHeadings)
            if (heading.titleText != null)
                heading.titleText.text = GameLocalization.Get(heading.localizationKey, heading.fallback);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall -= RefreshEditorPreview;
        UnityEditor.EditorApplication.delayCall += RefreshEditorPreview;
    }

    private void RefreshEditorPreview()
    {
        UnityEditor.EditorApplication.delayCall -= RefreshEditorPreview;
        if (this == null || UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        ApplySectionVisibility(previewAllSections);
        UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    private void HandleBattleActivityChanged(bool _)
    {
        UpdateTexts();
    }
}

[Serializable]
public class StatText
{
    public TMP_Text labelText;
    public TMP_Text valueText;
    public StatType stat;
    public string overrideText;
    public bool needToOverrideText;
    public GameObject rowRoot;
    public bool hideWhenDefault;
}

[Serializable]
public class PlayerStatsHeading
{
    public string localizationKey;
    public string fallback;
    public TMP_Text titleText;
}

[Serializable]
public class PlayerStatsSection : PlayerStatsHeading
{
    public GameObject root;
    public GameObject separator;
    public GameObject body;
    public TMP_Text arrowText;
    public bool expanded;
    public bool hideWhenDefault;
    public float defaultValue;
    public List<StatType> visibilityStats = new();
    public int parentSectionIndex = -1;
    public bool hideWhenChildrenEmpty;
}
