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
    
    void Start()
    {
        _player = PlayerUnit.Instance;
        _player.OnStatsRecalculated += UpdateTexts;
        UpdateTexts();
    }
   
    void UpdateTexts()
    {
        if (statTexts.Count != stats.Count) Debug.LogWarning("Stat text count mismatch");
        var minLength = Math.Min(statTexts.Count, stats.Count);
        for (int i = 0; i < minLength; i++)
        {
            statTexts[i].text = stats[i].ToPrettyString() + ": " + Math.Round(_player.baseUnitModifiers.StatValues[stats[i]]);
        }
    }
}
