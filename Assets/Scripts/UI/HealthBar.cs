using System;
using Battle;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private MysticHealth mysticHealth;
    [SerializeField] private GSlider healthSlider;
    [SerializeField] private GSlider profanedHealthSlider;
    [SerializeField] private GSlider mysticHealthSlider;
    [SerializeField] private MysticColorsConfig mysticColorsConfig;

    private void Awake()
    {
        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            health.OnMaximumHealthChanged += UpdateHealthBar;
            health.OnProfanedHealthChanged += UpdateProfanedHealthBar;
        }

        if (mysticHealth != null)
        {
            mysticHealth.OnAbsorptionChanged += UpdateMysticHealthBar;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= UpdateHealthBar;
            health.OnMaximumHealthChanged -= UpdateHealthBar;
            health.OnProfanedHealthChanged -= UpdateProfanedHealthBar;
        }

        if (mysticHealth != null)
        {
            mysticHealth.OnAbsorptionChanged -= UpdateMysticHealthBar;
        }
    }

    private void Start()
    {
        UpdateHealthBar();
        UpdateProfanedHealthBar();
        UpdateMysticHealthBar(0f, 0f, mysticHealth != null ? mysticHealth.TotalAbsorption : 0f);
    }

    private void UpdateHealthBar()
    {
        healthSlider.UpdateBar(health.CurrentHealth01);
        healthSlider.UpdateText(Math.Ceiling(health.CurrentHealth) + "/" + Math.Ceiling(health.MaxHealth));
    }

    private void UpdateProfanedHealthBar()
    {
        if (profanedHealthSlider == null || health == null)
        {
            return;
        }

        profanedHealthSlider.UpdateBar(health.ProfanedHealthPercent01);
    }

    private void UpdateMysticHealthBar(float lightAbsorption, float darknessAbsorption, float totalAbsorption)
    {
        if (lightAbsorption > 0f)
        {
            mysticHealthSlider.SetFillColor(mysticColorsConfig.LightColor);
        }
        else if (darknessAbsorption > 0f)
        {
            mysticHealthSlider.SetFillColor(mysticColorsConfig.DarknessColor);
        }

        mysticHealthSlider.UpdateBar(mysticHealth.TotalAbsorptionPercent01);
    }
}
