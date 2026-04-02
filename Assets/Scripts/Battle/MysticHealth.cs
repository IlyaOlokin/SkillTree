using System;
using UnityEngine;

namespace Battle
{
    public enum MysticAbsorptionType
    {
        None = 0,
        Light = 1,
        Darkness = 2
    }

    public class MysticHealth : MonoBehaviour, IUnitComponent
    {
        private const float StackStepPercent01 = 0.05f;

        private Unit _owner;
        private float _absorptionSigned;
        private MysticAbsorptionType _lastType = MysticAbsorptionType.None;
        private int _lastStacks;

        public float AbsorptionSigned => _absorptionSigned;
        public float LightAbsorption => Mathf.Max(0f, _absorptionSigned);
        public float DarknessAbsorption => Mathf.Max(0f, -_absorptionSigned);
        public float TotalAbsorption => Mathf.Abs(_absorptionSigned);

        public float LightAbsorptionPercent01 => ToPercent01(LightAbsorption);
        public float DarknessAbsorptionPercent01 => ToPercent01(DarknessAbsorption);
        public float TotalAbsorptionPercent01 => ToPercent01(TotalAbsorption);

        public event Action<float, float, float> OnAbsorptionChanged;
        public event Action<MysticAbsorptionType> OnAbsorptionTypeChanged;
        public event Action<MysticAbsorptionType, int> OnAbsorptionStacksChanged;

        public void Init(Unit owner)
        {
            _owner = owner;
            _owner.OnStatsRecalculated += HandleOwnerStatsRecalculated;
            OnAbsorptionStacksChanged += HandleAbsorptionStacksChanged;
            ClampToMaxHealth();
        }

        private void OnDestroy()
        {
            if (_owner != null)
            {
                _owner.OnStatsRecalculated -= HandleOwnerStatsRecalculated;
            }

            OnAbsorptionStacksChanged -= HandleAbsorptionStacksChanged;
        }

        public void CombatTick(float deltaTime)
        {
            float cleansePerSecond = Mathf.Max(0f, _owner.BaseUnitModifiers.GetStatValue(StatType.MysticCleansePerSecond));
            TickCleanse(deltaTime, cleansePerSecond);
        }

        public void ApplyMysticDamage(float lightDamage, float darknessDamage)
        {
            float deltaSigned = lightDamage - darknessDamage;
            if (Mathf.Approximately(deltaSigned, 0f))
            {
                return;
            }

            SetAbsorptionSigned(_absorptionSigned + deltaSigned);
        }

        public void ApplyMysticDamageAsAbsorption(DamageInstance damageInstance)
        {
            if (damageInstance == null)
            {
                return;
            }

            float lightDamage = damageInstance.Damage[DamageType.Light];
            float darknessDamage = damageInstance.Damage[DamageType.Darkness];
            if (lightDamage <= 0f && darknessDamage <= 0f)
            {
                return;
            }

            ApplyMysticDamage(lightDamage, darknessDamage);
        }

        public void TickCleanse(float deltaTime, float cleansePercentPerSecond)
        {
            if (deltaTime <= 0f || cleansePercentPerSecond <= 0f || Mathf.Approximately(_absorptionSigned, 0f))
            {
                return;
            }

            float step = cleansePercentPerSecond * _owner.health.MaxHealth * deltaTime;
            float nextValue = _absorptionSigned;

            if (_absorptionSigned > 0f)
            {
                nextValue = Mathf.Max(0f, _absorptionSigned - step);
            }
            else
            {
                nextValue = Mathf.Min(0f, _absorptionSigned + step);
            }

            SetAbsorptionSigned(nextValue);
        }

        public float GetDeathThreshold()
        {
            return TotalAbsorption;
        }

        public bool IsHealthBelowDeathThreshold(float currentHealth, float maxHealth)
        {
            return currentHealth <= GetDeathThreshold();
        }

        private void SetAbsorptionSigned(float value)
        {
            float clamped = ClampByCurrentMaxHealth(value);
            if (Mathf.Approximately(clamped, _absorptionSigned))
            {
                return;
            }

            _absorptionSigned = clamped;
            NotifyIfStateChanged(force: false);
        }

        private void NotifyIfStateChanged(bool force)
        {
            OnAbsorptionChanged?.Invoke(LightAbsorption, DarknessAbsorption, TotalAbsorption);

            var type = ResolveType(_absorptionSigned);
            if (force || type != _lastType)
            {
                _lastType = type;
                OnAbsorptionTypeChanged?.Invoke(type);
            }

            int stacks = 0;
            float maxHealth = CurrentMaxHealth;
            if (maxHealth > 0f)
            {
                float stepAbs = maxHealth * StackStepPercent01;
                stacks = stepAbs > 0f ? Mathf.FloorToInt(TotalAbsorption / stepAbs) : 0;
            }

            if (force || stacks != _lastStacks)
            {
                _lastStacks = stacks;
                OnAbsorptionStacksChanged?.Invoke(type, stacks);
            }
        }

        private void HandleOwnerStatsRecalculated()
        {
            float previous = _absorptionSigned;
            ClampToMaxHealth();
            bool changedByClamp = !Mathf.Approximately(previous, _absorptionSigned);
            NotifyIfStateChanged(force: changedByClamp);
        }

        private void HandleAbsorptionStacksChanged(MysticAbsorptionType type, int stacks)
        {
            int lightStacks = type == MysticAbsorptionType.Light ? stacks : 0;
            int darknessStacks = type == MysticAbsorptionType.Darkness ? stacks : 0;

            if (!(lightStacks <= 0 && _owner.effectController.GetAllEffectsOfType<LightAbsorptionDebuff>().Count == 0))
            {
                _owner.effectController.AddEffect(new LightAbsorptionDebuff(lightStacks));
            }
            if (!(darknessStacks <= 0 && _owner.effectController.GetAllEffectsOfType<DarknessAbsorptionDebuff>().Count == 0))
            {
                _owner.effectController.AddEffect(new DarknessAbsorptionDebuff(darknessStacks));
            }
        }

        private void ClampToMaxHealth()
        {
            _absorptionSigned = ClampByCurrentMaxHealth(_absorptionSigned);
        }

        private float ClampByCurrentMaxHealth(float value)
        {
            float maxHealth = CurrentMaxHealth;
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp(value, -maxHealth, maxHealth);
        }

        private float ToPercent01(float absoluteValue)
        {
            float maxHealth = CurrentMaxHealth;
            if (maxHealth <= 0f)
            {
                return 0f;
            }

            return absoluteValue / maxHealth;
        }

        private float CurrentMaxHealth => _owner != null && _owner.health != null ? _owner.health.MaxHealth : 0f;

        private static MysticAbsorptionType ResolveType(float signedAbsorption)
        {
            if (signedAbsorption > 0f)
            {
                return MysticAbsorptionType.Light;
            }

            if (signedAbsorption < 0f)
            {
                return MysticAbsorptionType.Darkness;
            }

            return MysticAbsorptionType.None;
        }
    }

}
