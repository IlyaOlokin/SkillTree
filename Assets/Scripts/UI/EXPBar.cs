using System;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;

public class EXPBar : MonoBehaviour
{
    [SerializeField] private GSlider slider;
    private UnitLevel _unitLevel;
    
    private void Start()
    {
        _unitLevel = PlayerUnit.Instance.UnitLevel;
        _unitLevel.OnExpChanged += UpdateSlider;
    }

    private void UpdateSlider()
    {
        slider.UpdateBar(_unitLevel.CurrentExp / _unitLevel.ExpToNextLevel);
    }
}
