using System;
using Battle;
using UnityEngine;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private MysticHealth mysticHealth;
    [SerializeField] private GSlider healthSlider;
    [SerializeField] private GSlider profanedHealthSlider;
    [SerializeField] private GSlider hallowedHealthSlider;
    [SerializeField] private GSlider mysticHealthSlider;
    [SerializeField] private MysticColorsConfig mysticColorsConfig;
    [SerializeField] private ProceduralMagicRootUI mysticRoot;

    private void Awake()
    {
        if (mysticRoot == null) mysticRoot = GetComponent<ProceduralMagicRootUI>();
        if (mysticRoot == null && mysticHealthSlider != null)
            mysticRoot = mysticHealthSlider.GetComponentInChildren<ProceduralMagicRootUI>(true);

        if (health != null)
        {
            health.OnHealthChanged += UpdateHealthBar;
            health.OnMaximumHealthChanged += UpdateHealthBar;
            health.OnProfanedHealthChanged += UpdateProfanedHealthBar;
            health.OnHallowedHealthChanged += UpdateHallowedHealthBar;
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
            health.OnHallowedHealthChanged -= UpdateHallowedHealthBar;
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
        UpdateHallowedHealthBar();
        if (mysticHealth != null)
            UpdateMysticHealthBar(mysticHealth.LightAbsorption, mysticHealth.DarknessAbsorption, mysticHealth.TotalAbsorption);
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
        profanedHealthSlider.SetMirrored(health.IsProfanedHealthOnHighSide);
    }

    private void UpdateHallowedHealthBar()
    {
        if (hallowedHealthSlider == null || health == null)
        {
            return;
        }

        hallowedHealthSlider.UpdateBar(health.HallowedHealthPercent01);
        hallowedHealthSlider.SetMirrored(health.IsHallowedHealthOnHighSide);
    }

    private void UpdateMysticHealthBar(float lightAbsorption, float darknessAbsorption, float totalAbsorption)
    {
        if (mysticHealthSlider == null || mysticHealth == null) return;

        if (mysticRoot != null && mysticRoot.isActiveAndEnabled)
        {
            if (lightAbsorption > 0f) mysticRoot.ApplyLightPreset();
            else if (darknessAbsorption > 0f) mysticRoot.ApplyDarknessPreset();
            // The shader owns the color; tinting the Image too would multiply it twice.
            mysticHealthSlider.SetFillColor(Color.white);
        }
        else if (lightAbsorption > 0f && mysticColorsConfig != null)
        {
            mysticHealthSlider.SetFillColor(mysticColorsConfig.LightColor);
        }
        else if (darknessAbsorption > 0f && mysticColorsConfig != null)
        {
            mysticHealthSlider.SetFillColor(mysticColorsConfig.DarknessColor);
        }

        mysticHealthSlider.UpdateBar(mysticHealth.TotalAbsorptionPercent01);
    }
}
