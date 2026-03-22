using System;
using Battle;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public class EXPBar : MonoBehaviour
{
    [Inject] private UnitLevel _unitLevel;
    [SerializeField] private GSlider slider;
    
    private void Start()
    {
        _unitLevel.OnExpChanged += UpdateSlider;
        UpdateSlider();
    }

    private void OnDestroy()
    {
        if (_unitLevel != null)
            _unitLevel.OnExpChanged -= UpdateSlider;
    }

    private void UpdateSlider()
    {
        if (_unitLevel.ExpToNextLevel <= 0d)
        {
            slider.UpdateBar(0f);
            return;
        }

        slider.UpdateBar((float)(_unitLevel.CurrentExp / _unitLevel.ExpToNextLevel));
    }
}
