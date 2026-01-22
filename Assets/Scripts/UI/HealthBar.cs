using System;
using Battle;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
        health.OnHealthChange += UpdateHealthBar;
    }

    private void Start()
    {
        UpdateHealthBar(health.CurrentHealth);
    }

    private void UpdateHealthBar(float delta)
    {
        slider.value = health.CurrentHealth / health.MaxHealth;
    }
}
