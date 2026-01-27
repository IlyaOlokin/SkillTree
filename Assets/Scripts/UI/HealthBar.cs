using System;
using Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private TMP_Text text;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        health.OnHealthChanged += UpdateHealthBar;
        health.OnMaximumHealthChanged += UpdateHealthBar;
    }

    private void Start()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        slider.value = health.CurrentHealth / health.MaxHealth;
        text.text = health.CurrentHealth + "/" + health.MaxHealth;
    }
}
