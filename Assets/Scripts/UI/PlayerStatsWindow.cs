using System;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using TMPro;

public class PlayerStatsWindow : MonoBehaviour
{
    private PlayerUnit _player;
    [SerializeField] List<TMP_Text> statTexts;
    [SerializeField] List<StatType> stats;
    [SerializeField] List<StatType> percentStats;
    
    void Start()
    {
        _player = PlayerUnit.Instance;
        _player.OnStatsRecalculated += UpdateTexts;
        UpdateTexts();
    }
   
    void UpdateTexts()
    {
        if (statTexts.Count != stats.Count) 
            Debug.LogWarning("Stat text count mismatch");
        int min = Mathf.Min(stats.Count, statTexts.Count);

        var statValues = _player.baseUnitModifiers.StatValues;

        for (int i = 0; i < min; i++)
        {
            var stat = stats[i];

            if (!statValues.TryGetValue(stat, out float rawValue))
                continue;

            bool isPercent = percentStats.Contains(stat);

            float displayValue = isPercent
                ? rawValue * 100f
                : rawValue;

            int rounded = Mathf.RoundToInt(displayValue);

            statTexts[i].text =
                $"<b>{stat.ToPrettyString()}:</b> {(isPercent ? rounded + "%" : rounded.ToString())}";
        }
    }
}