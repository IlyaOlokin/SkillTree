using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    public class EnergyBarrier : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;
        
        private int _barrierCount = 0;
        private int _maxBarrierCount = 0;
        
        private const float BARRIER_COOLDOWN = 5f;
        private float _cooldownProgress;
        private float _cachedBarrierRegenerationSpeed;
        
        private List<DamageType> _damageTypesToBlock = new List<DamageType>() { DamageType.Physical };

        public bool HasActiveBarrier => _barrierCount > 0;
        
        public void Init(Unit unit)
        {
            _owner = unit;
            _barrierCount = _maxBarrierCount;
            _owner.OnStatsRecalculated += UpdateBarrierValues;
        }

        private void Update()
        {
            HandleRegeneration(Time.deltaTime);
        }
        
        public void TakeDamage(DamageInstance damageInstance)
        {
            if (!damageInstance.Damage.Keys.Any(type => _damageTypesToBlock.Contains(type)))
                return;

            float flatDamage = 0;
            foreach (var damageType in _damageTypesToBlock)
            {
                if (damageInstance.Damage.Keys.Contains(damageType))
                    flatDamage += damageInstance.Damage[damageType];
            }
            float cachedDamage = flatDamage;

            for (int i = 0; i < _barrierCount; i++)
            {
                if (flatDamage > GetBarrierPower())
                {
                    _barrierCount -= 1;
                    flatDamage -= GetBarrierPower();
                }
                else
                {
                    _barrierCount -= 1;
                    foreach (var damageType in _damageTypesToBlock)
                    {
                        damageInstance.Damage[damageType] = 0;
                    }
                    return;
                }
            }
            
            float damagePassed = flatDamage / cachedDamage;
            foreach (var damageType in _damageTypesToBlock)
            {
                damageInstance.Damage[damageType] *= damagePassed;
            }
        }

        private float GetBarrierPower()
        {
            return _owner.baseUnitModifiers.StatValues[StatType.BarrierPower];
        }
        
        private void UpdateBarrierValues()
        {
            _maxBarrierCount = (int) _owner.baseUnitModifiers.StatValues[StatType.BarrierCount];
            _barrierCount = Math.Min(_barrierCount, _maxBarrierCount);

            _cachedBarrierRegenerationSpeed = _owner.baseUnitModifiers.StatValues[StatType.BarrierRegenerationSpeed];
        }

        private void HandleRegeneration(float deltaTime)
        {
            if (_barrierCount == _maxBarrierCount) return;
            
            _cooldownProgress += (deltaTime / BARRIER_COOLDOWN) * (1 + _cachedBarrierRegenerationSpeed);
            
            if (_cooldownProgress >= 1)
            {
                _barrierCount += 1;
                _cooldownProgress = 0;
                _barrierCount = Math.Min(_barrierCount, _maxBarrierCount);
            }
        }
    }
}



