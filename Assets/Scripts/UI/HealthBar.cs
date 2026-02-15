using System;
using Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private GSlider slider;

    private void Awake()
    {
        health.OnHealthChanged += UpdateHealthBar;
        health.OnMaximumHealthChanged += UpdateHealthBar;
    }

    private void Start()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        slider.UpdateBar(health.CurrentHealth / health.MaxHealth);
        slider.UpdateText(Math.Ceiling(health.CurrentHealth) + "/" + Math.Ceiling(health.MaxHealth));
    }
}
