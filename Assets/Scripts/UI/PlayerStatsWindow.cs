using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using Battle;
using TMPro;
using Zenject;

public class PlayerStatsWindow : MonoBehaviour
{
    [Inject] private PlayerUnit _player;
    [SerializeField] private TMP_Text lvlText;
    [SerializeField] private TMP_Text DPSText;
    [SerializeField] private TMP_Text DamageText;
    [SerializeField] private TMP_Text AttackSpeedText;
    [SerializeField] private TMP_Text CritChanceText;
    [SerializeField] private TMP_Text CritDamageBonusText;
    [SerializeField] private List<StatText> statTexts;
    [SerializeField] private List<StatType> percentStats;
    
    void Start()
    {
        _player.OnStatsRecalculated += UpdateTexts;
        _player.UnitLevel.OnLevelUp += UpdateLevelText;
        UpdateTexts();
        UpdateLevelText(1);
    }

    private void OnDestroy()
    {
        if (_player != null)
        {
            _player.OnStatsRecalculated -= UpdateTexts;
            _player.UnitLevel.OnLevelUp -= UpdateLevelText;
        }
    }

    private void UpdateLevelText(int level)
    {
        lvlText.text = $"Level: {_player.UnitLevel.Level}";
    }
   
    void UpdateTexts()
    {

        UpdateDamageTexts();
        

        foreach (var statText in statTexts)
        {
            if (!_player.BaseUnitModifiers.TryGetStatValue(statText.stat, out float rawValue))
                continue;
            
            bool isPercent = percentStats.Contains(statText.stat);
            float displayValue = CalculateDisplayValue(statText.stat, rawValue, isPercent);
            string suffix = GetStatSuffix(statText.stat);
            var label = statText.needToOverrideText ? statText.overrideText : statText.stat.ToPrettyString();
            statText.labelText.text = $"{label}:";
            statText.valueText.text = FormatStatValue(displayValue, isPercent, suffix);
        }
    }
    
    private string FormatStatValue(float value, bool isPercent, string suffix = "")
    {
        bool isDoubleDigit = Mathf.Abs(value) >= 10f;
        float roundedValue = isDoubleDigit
            ? Mathf.Round(value)
            : Mathf.Round(value * 10f) / 10f;

        string numberText = roundedValue.ToString(
            isDoubleDigit ? "0" : "0.#",
            CultureInfo.InvariantCulture);

        return isPercent ? $"{numberText}%{suffix}" : $"{numberText}{suffix}";
    }
    
    private float CalculateDisplayValue(StatType stat, float rawValue, bool isPercent)
    {
        float normalizedValue = isPercent ? rawValue * 100f : rawValue;

        switch (stat)
        {
            case StatType.BarrierRegenerationSpeed:
                return EnergyBarrier.BarrierCooldown / rawValue;
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
        AttackSpeedText.text = $"{FormatStatValue(attackSpeed, false)}";
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
    
    private string GetStatSuffix(StatType stat)
    {
        switch (stat)
        {
            case StatType.BarrierRegenerationSpeed: 
                return "s";
            default:
                return string.Empty;
        }
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
