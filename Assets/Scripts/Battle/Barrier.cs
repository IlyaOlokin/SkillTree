using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class Barrier : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;

        private int _barrierCount;
        private int _maxBarrierCount;

        public static readonly float BarrierCooldown = 5f;

        private float _cooldownProgress;
        private float _regenSpeedMult;
        private float _barrierPower;

        private DamageType _blockedTypes;

        public int BarrierCount => _barrierCount;
        public int MaxBarrierCount => _maxBarrierCount;
        public float CooldownProgress => _cooldownProgress;

        public bool HasBarrier => _maxBarrierCount > 0;
        public bool IsFull => _barrierCount >= _maxBarrierCount;

        public event Action OnBarrierCountChanged;
        public event Action OnMaxBarrierChanged;
        public event Action OnBarrierRestored;

        public void Init(Unit unit)
        {
            _owner = unit;
            _owner.OnStatsRecalculated += UpdateBarrierValues;
        }

        private void OnDestroy()
        {
            if (_owner != null)
                _owner.OnStatsRecalculated -= UpdateBarrierValues;
        }

        public void CombatTick(float deltaTime)
        {
            Regenerate(deltaTime);
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

            var damageTypes = new List<DamageType>(damage.Damage.Keys);
            foreach (var damageType in damageTypes)
            {
                if (_blockedTypes.HasFlag(damageType))
                {
                    damage.Damage[damageType] *= multiplier;
                }
            }
        }
        
        private void UpdateBarrierValues()
        {
            _maxBarrierCount = (int)_owner.BaseUnitModifiers.GetStatValue(StatType.BarrierCount);
            _barrierPower = Mathf.Max(1f, _owner.BaseUnitModifiers.GetStatValue(StatType.BarrierCapacity));
            _regenSpeedMult = _owner.BaseUnitModifiers.GetStatValue(StatType.BarrierRegenerationSpeed);
            _blockedTypes = (DamageType) _owner.BaseUnitModifiers.GetStatValue(StatType.BarrierDamageTypeMask);

            OnMaxBarrierChanged?.Invoke();
            OnBarrierCountChanged?.Invoke();
        }

        private void Regenerate(float deltaTime)
        {
            if (IsFull)
                return;

            _cooldownProgress += (deltaTime / BarrierCooldown) * _regenSpeedMult;

            if (_cooldownProgress >= 1f)
            {
                _cooldownProgress -= 1f;
                int previousBarrierCount = _barrierCount;
                _barrierCount = Mathf.Min(_barrierCount + 1, _maxBarrierCount);

                OnBarrierCountChanged?.Invoke();

                if (_barrierCount > previousBarrierCount)
                {
                    OnBarrierRestored?.Invoke();
                }
            }
        }

        public void RestoreFull()
        {
            _barrierCount = _maxBarrierCount;
            _cooldownProgress = 0f;
            OnBarrierCountChanged?.Invoke();
        }

        public int Restore(int amount)
        {
            if (amount <= 0 || IsFull)
            {
                return 0;
            }

            int previousBarrierCount = _barrierCount;
            _barrierCount = Mathf.Min(_barrierCount + amount, _maxBarrierCount);
            int restoredAmount = _barrierCount - previousBarrierCount;

            if (restoredAmount <= 0)
            {
                return 0;
            }

            OnBarrierCountChanged?.Invoke();

            for (int i = 0; i < restoredAmount; i++)
            {
                OnBarrierRestored?.Invoke();
            }

            return restoredAmount;
        }
    }
}
