using System;
using Battle;
using TMPro;
using UnityEngine;

public class EnemyDataText : MonoBehaviour
{
    [SerializeField] private EnemyUnit unit;
    [SerializeField] private TMP_Text text;

    private void Awake()
    {
        unit.OnInitialized += UpdateText;
    }

    private void UpdateText()
    {
        text.text = Math.Round(unit.SpawnData.Power) + "\n" + unit.SpawnData.Rarity;
    }
}
