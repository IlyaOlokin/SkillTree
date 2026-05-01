using UnityEngine;
using System.Collections.Generic;

namespace Battle
{
    public class Attacker : MonoBehaviour, IUnitComponent
    {
        private const float AttackCycleProgress = 1f;

        private Unit _owner;
        private BaseUnitModifiers _attackSnapshot;
        private DamageInfo _attackDamageInfo;
        public ITarget Target { get; private set; }

        public float AttackProgress => _attackTimer;

        private float _attackTimer;
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
            if (_attackTimer < 1)
            {
                AddAttackProgress(GetCalculatedAttackSpeed() * deltaTime);
            }
            else if (Target?.UnitObject != null)
            {
                AttackTarget();
                ConsumeAttackCycle();
            }
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
            _attackTimer = Mathf.Max(0f, _attackTimer - AttackCycleProgress);
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
            AddAttackProgress(deltaProgress);
        }

        private void AddAttackProgress(float deltaProgress)
        {
            if (Mathf.Approximately(deltaProgress, 0f))
            {
                return;
            }

            float previousProgress = _attackTimer;
            _attackTimer = Mathf.Max(0f, _attackTimer + deltaProgress);

            TryTriggerExtraAttacks(previousProgress, _attackTimer);
        }

        private void TryTriggerExtraAttacks(float previousProgress, float currentProgress)
        {
            if (_extraAttackMoments.Count == 0)
            {
                return;
            }

            if (Target?.UnitObject == null)
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

                AttackTarget();
                _triggeredExtraAttackMomentsThisCycle.Add(extraAttackMoment);
            }
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
            _owner.OnAttackStarted(Target);
            _attackSnapshot.CopyFrom(_owner.BaseUnitModifiers);
            _attackDamageInfo.Reset(_owner, _attackSnapshot);
            
            AttackProcessor.HandleAttack(_owner, _attackDamageInfo, Target);
        }
    }
}
