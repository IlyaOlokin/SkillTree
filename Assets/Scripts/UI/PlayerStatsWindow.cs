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
        

        foreach (var statText in statTexts)
        {
            if (!_player.BaseUnitModifiers.TryGetStatValue(statText.stat, out float rawValue))
                continue;
            
            bool isPercent = StatTypeDisplayRules.IsPercentStat(statText.stat);
            float displayValue = CalculateDisplayValue(statText.stat, rawValue, isPercent);
            string prefix = GetStatPrefix(statText.stat, rawValue);
            string suffix = GetStatSuffix(statText.stat, rawValue);
            string label = statText.needToOverrideText
                ? GameLocalization.LocalizeValueOrKey(GameLocalization.ContentTable, statText.overrideText)
                : GameLocalization.LocalizeEnum(statText.stat);
            statText.labelText.text = $"{label}:";
            statText.valueText.text = FormatStatValue(displayValue, isPercent, prefix, suffix, GetStatDecimalPlaces(statText.stat));
        }
    }
    
    private string FormatStatValue(float value, bool isPercent, string prefix = "", string suffix = "", int decimalPlaces = 1)
    {
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

    private int GetStatDecimalPlaces(StatType stat)
    {
        return stat == StatType.AttackSpeed ? 2 : 1;
    }
    
    private float CalculateDisplayValue(StatType stat, float rawValue, bool isPercent)
    {
        float normalizedValue = isPercent ? rawValue * 100f : rawValue;

        switch (stat)
        {
            case StatType.ElementalResistance:
                return Mathf.Min(
                    rawValue,
                    _player.BaseUnitModifiers.GetStatValue(StatType.MaxElementalResistance)) * (isPercent ? 100f : 1f);
            case StatType.FireResistance:
                return Mathf.Min(
                    rawValue,
                    _player.BaseUnitModifiers.GetStatValue(StatType.MaxFireResistance)) * (isPercent ? 100f : 1f);
            case StatType.ColdResistance:
                return Mathf.Min(
                    rawValue,
                    _player.BaseUnitModifiers.GetStatValue(StatType.MaxColdResistance)) * (isPercent ? 100f : 1f);
            case StatType.LightningResistance:
                return Mathf.Min(
                    rawValue,
                    _player.BaseUnitModifiers.GetStatValue(StatType.MaxLightningResistance)) * (isPercent ? 100f : 1f);
            case StatType.BarrierRegenerationSpeed:
                return Barrier.BarrierCooldown / rawValue;
            
            default:
                return normalizedValue;
        }
    }

    private void UpdateDamageTexts()
    {
        float damage = CalculateHitDamage();
        float attackSpeed = _player.BaseUnitModifiers.GetStatValue(StatType.AttackSpeed);
        float critChance = _player.BaseUnitModifiers.GetStatValue(StatType.CritChance);
        float critDamageBonus = _player.BaseUnitModifiers.GetStatValue(StatType.CritDamageBonus);
        DPSText.text = $"{FormatStatValue(damage * attackSpeed * (1 + critChance * critDamageBonus), false)}";
        DamageText.text = $"{FormatStatValue(damage, false)}";
        AttackSpeedText.text = $"{FormatStatValue(attackSpeed, false, decimalPlaces: GetStatDecimalPlaces(StatType.AttackSpeed))}";
        CritChanceText.text = $"{FormatStatValue(CalculateDisplayValue(StatType.CritChance, critChance, true), true)}";
        CritDamageBonusText.text = $"{FormatStatValue(CalculateDisplayValue(StatType.CritDamageBonus, critDamageBonus, true), true)}";
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
        switch (stat)
        {
            case StatType.ElementalResistance:
                return GetUncappedResistanceSuffix(rawValue, StatType.MaxElementalResistance);
            case StatType.FireResistance:
                return GetUncappedResistanceSuffix(rawValue, StatType.MaxFireResistance);
            case StatType.ColdResistance:
                return GetUncappedResistanceSuffix(rawValue, StatType.MaxColdResistance);
            case StatType.LightningResistance:
                return GetUncappedResistanceSuffix(rawValue, StatType.MaxLightningResistance);
            case StatType.Armor:
                return GetEstimatedArmorMitigationSuffix(rawValue);
            case StatType.Evasion:
                return GetEstimatedEvasionChanceSuffix(rawValue);
            case StatType.BarrierRegenerationSpeed: 
                return GameLocalization.Get("ui.common.secondsShort", "s");
            default:
                return string.Empty;
        }
    }

    private string GetStatPrefix(StatType stat, float rawValue)
    {
        switch (stat)
        {
            case StatType.IgniteChance:
                return "+";
            case StatType.ChillChance:
                return "+";
            case StatType.OverchargeChance:
                return "+";
            case StatType.BleedPower:
                return "+";
            case StatType.IgnitePower:
                return "+";
            case StatType.ChillPower:
                return "+";
            case StatType.OverchargePower:
                return "+";
            case StatType.ElementalResistancePenetration:
                return "+";
            case StatType.FireResistancePenetration:
                return "+";
            case StatType.ColdResistancePenetration:
                return "+";
            case StatType.LightningResistancePenetration:
                return "+";
            
            default:
                return string.Empty;
        }
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
}
