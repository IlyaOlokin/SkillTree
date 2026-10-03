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

        public static readonly float BarrierCooldown = 4f;

        private float _cooldownProgress;
        private float _regenSpeedMult;
        private float _barrierPower;

        private DamageType _blockedTypes;

        public int BarrierCount => _barrierCount;
        public int MaxBarrierCount => _maxBarrierCount;
        public float CooldownProgress => _cooldownProgress;

        public bool HasBarrier => _maxBarrierCount > 0;
        public bool IsFull => _barrierCount >= _maxBarrierCount;
        public bool IsRestoringAdditionalBarrier { get; private set; }

        public event Action OnBarrierCountChanged;
        public event Action OnMaxBarrierChanged;
        public event Action OnBarrierRestored;
        public event Action<int> OnBarriersLost;

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

        public void TakeDamage(DamageInstance damage, float barrierDamageMultiplier = 1f,
            int maxBarriersLostPerAttack = int.MaxValue)
        {
            float blockedDamage = 0f;

            foreach (var pair in damage.Damage)
            {
                if (_blockedTypes.HasFlag(pair.Key))
                    blockedDamage += pair.Value;
            }

            if (blockedDamage <= 0f || _barrierCount <= 0)
                return;

            float remainingBarrierDamage = blockedDamage * Mathf.Max(0f, barrierDamageMultiplier);
            int consumedBarrierCount = 0;
            int barrierLossLimit = Mathf.Max(1, maxBarriersLostPerAttack);

            while (_barrierCount > 0 && remainingBarrierDamage > 0f && consumedBarrierCount < barrierLossLimit)
            {
                _barrierCount--;
                consumedBarrierCount++;
                remainingBarrierDamage -= _barrierPower;
            }

            OnBarrierCountChanged?.Invoke();

            float absorbedDamage = Mathf.Min(blockedDamage, consumedBarrierCount * _barrierPower);
            float remainingDamage = Mathf.Max(0f, blockedDamage - absorbedDamage);
            float multiplier = remainingDamage / blockedDamage;

            var damageTypes = new List<DamageType>(damage.Damage.Keys);
            foreach (var damageType in damageTypes)
            {
                if (_blockedTypes.HasFlag(damageType))
                {
                    damage.Damage[damageType] *= multiplier;
                }
            }

            if (consumedBarrierCount > 0)
                OnBarriersLost?.Invoke(consumedBarrierCount);
        }
        
        private void UpdateBarrierValues()
        {
            _maxBarrierCount = Mathf.Max(0, (int)_owner.BaseUnitModifiers.GetStatValue(StatType.BarrierCount));
            _barrierCount = Mathf.Clamp(_barrierCount, 0, _maxBarrierCount);
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

        // Spend an exact cost and publish the same count notification as damage loss.
        public bool TryConsume(int amount)
        {
            if (amount <= 0 || _barrierCount < amount)
                return false;

            _barrierCount -= amount;
            OnBarrierCountChanged?.Invoke();
            OnBarriersLost?.Invoke(amount);
            return true;
        }

        // Keep normal restoration notifications, but prevent bonus restoration chains
        // across all modifier bindings on this barrier.
        public int RestoreAdditional(int amount)
        {
            if (IsRestoringAdditionalBarrier)
            {
                return 0;
            }

            IsRestoringAdditionalBarrier = true;
            try
            {
                return Restore(amount);
            }
            finally
            {
                IsRestoringAdditionalBarrier = false;
            }
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
