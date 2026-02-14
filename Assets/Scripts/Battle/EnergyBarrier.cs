using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class EnergyBarrier : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;

        private int _barrierCount;
        private int _maxBarrierCount;

        private const float BarrierCooldown = 10f;

        private float _cooldownProgress;
        private float _regenSpeed;
        private float _barrierPower;

        private DamageType _blockedTypes;

        public int BarrierCount => _barrierCount;
        public int MaxBarrierCount => _maxBarrierCount;
        public float CooldownProgress => _cooldownProgress;

        public bool HasBarrier => _maxBarrierCount > 0;
        public bool IsFull => _barrierCount >= _maxBarrierCount;

        public event Action OnBarrierCountChanged;
        public event Action OnMaxBarrierChanged;

        public void Init(Unit unit)
        {
            _owner = unit;
            _owner.OnStatsRecalculated += UpdateBarrierValues;

            UpdateBarrierValues();
            _barrierCount = _maxBarrierCount;
        }

        private void OnDestroy()
        {
            if (_owner != null)
                _owner.OnStatsRecalculated -= UpdateBarrierValues;
        }

        private void Update()
        {
            Regenerate(Time.deltaTime);
        }

        public void TakeDamage(DamageInstance damage)
        {
            float blockedDamage = 0f;

            foreach (var pair in damage.Damage)
            {
                if (_blockedTypes.HasFlag(pair.Key))
                    blockedDamage += pair.Value;
            }

            if (blockedDamage <= 0f || _barrierCount <= 0)
                return;
            
            float remainingDamage = blockedDamage;

            while (_barrierCount > 0 && remainingDamage > 0f)
            {
                _barrierCount--;
                remainingDamage -= _barrierPower;
            }

            OnBarrierCountChanged?.Invoke();
            
            float multiplier = remainingDamage > 0f
                ? remainingDamage / blockedDamage
                : 0f;

            foreach (var damageType in damage.Damage.Keys)
            {
                if (_blockedTypes.HasFlag(damageType))
                {
                    damage.Damage[damageType] *= multiplier;
                }
            }
        }
        
        private void UpdateBarrierValues()
        {
            _maxBarrierCount = (int)_owner.BaseUnitModifiers.StatValues[StatType.BarrierCount];
            _barrierCount = Mathf.Min(_barrierCount, _maxBarrierCount);
            _barrierPower = Mathf.Max(1f, _owner.BaseUnitModifiers.StatValues[StatType.BarrierPower]);
            _regenSpeed = _owner.BaseUnitModifiers.StatValues[StatType.BarrierRegenerationSpeed];
            _blockedTypes = (DamageType) _owner.BaseUnitModifiers.StatValues[StatType.BarrierDamageTypeMask];

            OnMaxBarrierChanged?.Invoke();
            OnBarrierCountChanged?.Invoke();
        }

        private void Regenerate(float deltaTime)
        {
            if (IsFull)
                return;

            _cooldownProgress += (deltaTime / BarrierCooldown) * (1f + _regenSpeed);

            if (_cooldownProgress >= 1f)
            {
                _cooldownProgress -= 1f;
                _barrierCount = Mathf.Min(_barrierCount + 1, _maxBarrierCount);

                OnBarrierCountChanged?.Invoke();
            }
        }
    }
}
