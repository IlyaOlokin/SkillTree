using System;
using System.Collections.Generic;
using UnityEngine;
using Battle;
using TMPro;
using Zenject;

public class PlayerStatsWindow : MonoBehaviour
{
    [Inject] private PlayerUnit _player;
    [SerializeField] List<TMP_Text> statTexts;
    [SerializeField] List<StatType> stats;
    [SerializeField] List<StatType> percentStats;
    
    void Start()
    {
        _player.OnStatsRecalculated += UpdateTexts;
        UpdateTexts();
    }

    private void OnDestroy()
    {
        if (_player != null)
            _player.OnStatsRecalculated -= UpdateTexts;
    }
   
    void UpdateTexts()
    {
        if (statTexts.Count != stats.Count) 
            Debug.LogWarning("Stat text count mismatch");
        int min = Mathf.Min(stats.Count, statTexts.Count);

        var statValues = _player.BaseUnitModifiers.StatValues;

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
