using UnityEngine;
using System.Collections.Generic;

namespace Battle
{
    public class Attacker : MonoBehaviour, IUnitComponent
    {
        private const float AttackCycleProgress = 1f;
        // Bound self-feeding progress callbacks without discarding accumulated progress.
        private const int MaxAttackCyclesPerTick = 128;

        private Unit _owner;
        private BaseUnitModifiers _attackSnapshot;
        private DamageInfo _attackDamageInfo;
        public ITarget Target { get; private set; }

        public float AttackProgress => (float)_attackTimer;
        public bool ExternalAttackProgressLocked => _externalAttackProgressLockCount > 0;

        private double _attackTimer;
        private bool _isAttacking;
        private int _externalAttackProgressLockCount;
        private readonly List<float> _extraAttackMoments = new List<float>();
        private readonly List<float> _triggeredExtraAttackMomentsThisCycle = new List<float>();

        private void Start()
        {
            ResetAttackCooldownHard();
        }
        
        public void Init(Unit owner)
        {
            _owner = owner;
            _attackSnapshot = new BaseUnitModifiers();
            _attackDamageInfo = new DamageInfo(_owner, _attackSnapshot);
        }

        public void SetTarget(ITarget target)
        {
            Target = target;
        }

        public void CombatTick(float deltaTime)
        {
            double remainingTime = Mathf.Max(0f, deltaTime);
            for (int cycle = 0; cycle < MaxAttackCyclesPerTick; cycle++)
            {
                if (!_owner.isActiveAndEnabled || IsAttackSuppressed()) return;

                if (_attackTimer < AttackCycleProgress)
                {
                    float speed = GetCalculatedAttackSpeed();
                    if (speed <= 0f || remainingTime <= 0f) return;

                    double step = System.Math.Min(remainingTime, (AttackCycleProgress - _attackTimer) / speed);
                    remainingTime = System.Math.Max(0d, remainingTime - step);
                    AddAttackProgress(speed * step);
                    if (System.Math.Abs(_attackTimer - AttackCycleProgress) <= 1e-7)
                        _attackTimer = AttackCycleProgress;
                }

                if (_attackTimer < AttackCycleProgress || Target?.UnitObject == null ||
                    !_owner.isActiveAndEnabled || IsAttackSuppressed()) return;

                AttackTarget();
                ConsumeAttackCycle();
                TryTriggerExtraAttacks(0f, _attackTimer);
            }

            _attackTimer += Mathf.Max(0f, GetCalculatedAttackSpeed()) * remainingTime;
        }

        private float GetCalculatedAttackSpeed()
        {
            return _owner.BaseUnitModifiers.GetStatValue(StatType.AttackSpeed);
        }

        public void ResetAttackCooldownHard()
        {
            _attackTimer = 0;
            _triggeredExtraAttackMomentsThisCycle.Clear();
        }
        
        public void ConsumeAttackCycle()
        {
            _attackTimer = System.Math.Max(0d, _attackTimer - AttackCycleProgress);
            _triggeredExtraAttackMomentsThisCycle.Clear();
        }

        public void AddExtraAttackMoment(float progressMoment)
        {
            float clampedMoment = Mathf.Clamp(progressMoment, 0f, 0.99f);
            if (ContainsMoment(_extraAttackMoments, clampedMoment))
            {
                return;
            }

            _extraAttackMoments.Add(clampedMoment);
            _extraAttackMoments.Sort();
        }

        public void RemoveExtraAttackMoment(float progressMoment)
        {
            float clampedMoment = Mathf.Clamp(progressMoment, 0f, 0.99f);
            RemoveMoment(_extraAttackMoments, clampedMoment);
            RemoveMoment(_triggeredExtraAttackMomentsThisCycle, clampedMoment);
        }

        public void ModifyAttackProgress(float deltaProgress)
        {
            if (ExternalAttackProgressLocked)
            {
                return;
            }

            AddAttackProgress(deltaProgress);
        }

        public void AddExternalAttackProgressLock()
        {
            _externalAttackProgressLockCount++;
        }

        public void RemoveExternalAttackProgressLock()
        {
            _externalAttackProgressLockCount = Mathf.Max(0, _externalAttackProgressLockCount - 1);
        }

        private void AddAttackProgress(double deltaProgress)
        {
            if (deltaProgress == 0d)
            {
                return;
            }

            double previousProgress = _attackTimer;
            _attackTimer = System.Math.Max(0d, _attackTimer + deltaProgress);

            TryTriggerExtraAttacks(previousProgress, _attackTimer);
        }

        private void TryTriggerExtraAttacks(double previousProgress, double currentProgress)
        {
            if (_isAttacking || !_owner.isActiveAndEnabled) return;
            if (_extraAttackMoments.Count == 0)
            {
                return;
            }

            if (Target?.UnitObject == null)
            {
                return;
            }

            if (IsAttackSuppressed())
            {
                return;
            }

            for (int i = 0; i < _extraAttackMoments.Count; i++)
            {
                float extraAttackMoment = _extraAttackMoments[i];
                bool crossedMoment = previousProgress < extraAttackMoment && currentProgress >= extraAttackMoment;
                if (!crossedMoment || ContainsMoment(_triggeredExtraAttackMomentsThisCycle, extraAttackMoment))
                {
                    continue;
                }

                _triggeredExtraAttackMomentsThisCycle.Add(extraAttackMoment);
                AttackTarget();
                if (!_owner.isActiveAndEnabled || Target?.UnitObject == null || IsAttackSuppressed()) return;
            }
        }

        private bool IsAttackSuppressed()
        {
            return _owner?.effectController != null &&
                   _owner.effectController.HasEffect<Freeze>();
        }

        private static bool ContainsMoment(List<float> moments, float value)
        {
            for (int i = 0; i < moments.Count; i++)
            {
                if (Mathf.Approximately(moments[i], value))
                {
                    return true;
                }
            }

            return false;
        }

        private static void RemoveMoment(List<float> moments, float value)
        {
            for (int i = 0; i < moments.Count; i++)
            {
                if (Mathf.Approximately(moments[i], value))
                {
                    moments.RemoveAt(i);
                    return;
                }
            }
        }

        private void AttackTarget()
        {
            if (_isAttacking) return;
            // Resolve before callbacks: selection changes only affect subsequent attacks.
            Unit attackTarget = Target?.UnitObject;
            if (attackTarget == null)
            {
                return;
            }

            _isAttacking = true;
            try
            {
                _owner.OnAttackStarted(attackTarget);
                _attackSnapshot.CopyFrom(_owner.BaseUnitModifiers);
                _attackDamageInfo.Reset(_owner, _attackSnapshot);
                _owner.PrepareAttack(_attackDamageInfo);

                AttackProcessor.HandleAttack(_owner, _attackDamageInfo, attackTarget);
                _owner.OnAttackFinished(attackTarget);
            }
            finally
            {
                _isAttacking = false;
            }
        }
    }
}
