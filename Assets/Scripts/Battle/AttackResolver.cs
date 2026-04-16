using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class AttackResolver : MonoBehaviour, ITarget
    {
        private readonly List<Unit> _enemies = new();
        private int _selectedEnemyIndex = -1;
        private Unit _lastNotifiedTarget;

        public event Action<Unit> OnCurrentTargetChanged;

        public Unit UnitObject
        {
            get => ResolveCurrentTarget();
            set => TrySelectTarget(value);
        }

        public DamageInstance ReceiveDamage(DamageInfo damageInfo)
        {
            Unit target = ResolveCurrentTarget();
            return target != null ? target.ReceiveDamage(damageInfo) : damageInfo?.DamageInstance;
        }

        public void ReceiveDoT(DamageInstance damageInstance)
        {
            Unit target = ResolveCurrentTarget();
            if (target == null)
            {
                return;
            }

            target.ReceiveDoT(damageInstance);
        }

        public void OnHitEvaded(DamageInstance damageInstance)
        {
            Unit target = ResolveCurrentTarget();
            if (target == null)
            {
                return;
            }

            target.OnHitEvaded(damageInstance);
        }

        public void OnHitBlock(DamageInstance damageInstance)
        {
            Unit target = ResolveCurrentTarget();
            if (target == null)
            {
                return;
            }

            target.OnHitBlock(damageInstance);
        }

        public void SetNewEnemies(List<Unit> enemyUnits)
        {
            UnsubscribeFromEnemyDeaths();
            _enemies.Clear();
            if (enemyUnits != null)
            {
                _enemies.AddRange(enemyUnits);
            }

            _selectedEnemyIndex = FindFirstAliveEnemyIndex();
            SubscribeToEnemyDeaths();
            NotifyCurrentTargetChanged();
        }

        public bool TrySelectTarget(Unit target)
        {
            int enemyIndex = IndexOfEnemy(target);
            if (!IsSelectableIndex(enemyIndex))
            {
                return false;
            }

            _selectedEnemyIndex = enemyIndex;
            NotifyCurrentTargetChanged();
            return true;
        }

        private Unit ResolveCurrentTarget()
        {
            if (_enemies.Count == 0)
            {
                _selectedEnemyIndex = -1;
                NotifyCurrentTargetChanged();
                return null;
            }

            if (IsSelectableIndex(_selectedEnemyIndex))
            {
                return _enemies[_selectedEnemyIndex];
            }

            int nextEnemyIndex = FindNextAliveEnemyIndex(_selectedEnemyIndex);
            _selectedEnemyIndex = nextEnemyIndex;
            Unit resolvedTarget = IsSelectableIndex(nextEnemyIndex) ? _enemies[nextEnemyIndex] : null;
            NotifyCurrentTargetChanged();
            return resolvedTarget;
        }

        private int FindFirstAliveEnemyIndex()
        {
            return FindNextAliveEnemyIndex(-1);
        }

        private int FindNextAliveEnemyIndex(int previousEnemyIndex)
        {
            if (_enemies.Count == 0)
            {
                return -1;
            }

            int startIndex = Mathf.Clamp(previousEnemyIndex + 1, 0, _enemies.Count);
            for (int i = startIndex; i < _enemies.Count; i++)
            {
                if (IsSelectableIndex(i))
                {
                    return i;
                }
            }

            for (int i = 0; i < startIndex; i++)
            {
                if (IsSelectableIndex(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private int IndexOfEnemy(Unit target)
        {
            if (target == null)
            {
                return -1;
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] == target)
                {
                    return i;
                }
            }

            return -1;
        }

        private bool IsSelectableIndex(int enemyIndex)
        {
            if (enemyIndex < 0 || enemyIndex >= _enemies.Count)
            {
                return false;
            }

            Unit enemy = _enemies[enemyIndex];
            return enemy != null && enemy.gameObject.activeSelf;
        }

        private void SubscribeToEnemyDeaths()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null)
                {
                    _enemies[i].OnDeath += HandleEnemyDeath;
                }
            }
        }

        private void UnsubscribeFromEnemyDeaths()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i] != null)
                {
                    _enemies[i].OnDeath -= HandleEnemyDeath;
                }
            }
        }

        private void HandleEnemyDeath(Unit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (IndexOfEnemy(unit) != _selectedEnemyIndex)
            {
                return;
            }

            _selectedEnemyIndex = FindNextAliveEnemyIndex(_selectedEnemyIndex);
            NotifyCurrentTargetChanged();
        }

        private void NotifyCurrentTargetChanged()
        {
            Unit currentTarget = IsSelectableIndex(_selectedEnemyIndex) ? _enemies[_selectedEnemyIndex] : null;
            if (_lastNotifiedTarget == currentTarget)
            {
                return;
            }

            _lastNotifiedTarget = currentTarget;
            OnCurrentTargetChanged?.Invoke(currentTarget);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEnemyDeaths();
        }
    }
}
