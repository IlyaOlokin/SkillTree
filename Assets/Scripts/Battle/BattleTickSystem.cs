using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public enum CombatTickPhase
    {
        Mods = 0,
        Resources = 1,
        Effects = 2,
        Actions = 3
    }

    public interface ICombatTickable
    {
        void CombatTick(float deltaTime, CombatTickPhase phase);
    }

    [DefaultExecutionOrder(-1000)]
    public class BattleTickSystem : MonoBehaviour
    {
        private const float MinimumTickDuration = 0.001f;

        [SerializeField, Min(MinimumTickDuration)] private float tickDuration = 1f / 60f;
        [SerializeField, Min(0f)] private float speedMultiplier = 1f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField, Min(1)] private int maxTicksPerFrame = 8;

        private readonly List<ICombatTickable> _tickables = new();
        private readonly List<ICombatTickable> _pendingRegistrations = new();
        private readonly List<ICombatTickable> _pendingUnregistrations = new();

        private float _accumulator;
        private bool _isTicking;
        private bool _isPaused;

        public float TickDuration
        {
            get => Mathf.Max(MinimumTickDuration, tickDuration);
            set => tickDuration = Mathf.Max(MinimumTickDuration, value);
        }
        public float TickRate => 1f / TickDuration;
        public float SpeedMultiplier
        {
            get => speedMultiplier;
            set => speedMultiplier = Mathf.Max(0f, value);
        }
        public bool IsPaused => _isPaused;

        public void SetTickRate(float tickRate)
        {
            if (tickRate <= 0f)
            {
                return;
            }

            TickDuration = 1f / tickRate;
        }

        public void Pause()
        {
            _isPaused = true;
            _accumulator = 0f;
        }

        public void Resume()
        {
            _isPaused = false;
            _accumulator = 0f;
        }

        public void Register(ICombatTickable tickable)
        {
            if (tickable == null)
            {
                return;
            }

            if (_isTicking)
            {
                _pendingUnregistrations.Remove(tickable);
                if (!_pendingRegistrations.Contains(tickable) && !_tickables.Contains(tickable))
                {
                    _pendingRegistrations.Add(tickable);
                }
                return;
            }

            if (_tickables.Contains(tickable))
            {
                return;
            }

            _tickables.Add(tickable);
        }

        public void Unregister(ICombatTickable tickable)
        {
            if (tickable == null)
            {
                return;
            }

            if (_isTicking)
            {
                _pendingRegistrations.Remove(tickable);
                if (!_pendingUnregistrations.Contains(tickable))
                {
                    _pendingUnregistrations.Add(tickable);
                }
                return;
            }

            _tickables.Remove(tickable);
        }

        private void Update()
        {
            if (_isPaused)
            {
                return;
            }

            float frameDeltaTime = (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * speedMultiplier;
            if (frameDeltaTime <= 0f)
            {
                return;
            }

            _accumulator += frameDeltaTime;

            float step = TickDuration;
            int processedTicks = 0;

            while (_accumulator >= step && processedTicks < maxTicksPerFrame)
            {
                _accumulator -= step;
                RunTick(step);
                processedTicks++;
            }
        }

        private void RunTick(float deltaTime)
        {
            _isTicking = true;

            ExecutePhase(deltaTime, CombatTickPhase.Mods);
            ExecutePhase(deltaTime, CombatTickPhase.Resources);
            ExecutePhase(deltaTime, CombatTickPhase.Effects);
            ExecutePhase(deltaTime, CombatTickPhase.Actions);

            _isTicking = false;
            FlushPendingUnregistrations();
            FlushPendingRegistrations();
        }

        private void ExecutePhase(float deltaTime, CombatTickPhase phase)
        {
            for (int i = 0; i < _tickables.Count; i++)
            {
                ICombatTickable tickable = _tickables[i];
                if (tickable is Behaviour behaviour)
                {
                    if (!behaviour.isActiveAndEnabled)
                    {
                        continue;
                    }
                }
                else if (tickable == null)
                {
                    continue;
                }

                tickable.CombatTick(deltaTime, phase);
            }
        }

        private void FlushPendingRegistrations()
        {
            for (int i = 0; i < _pendingRegistrations.Count; i++)
            {
                Register(_pendingRegistrations[i]);
            }

            _pendingRegistrations.Clear();
        }

        private void FlushPendingUnregistrations()
        {
            for (int i = 0; i < _pendingUnregistrations.Count; i++)
            {
                Unregister(_pendingUnregistrations[i]);
            }

            _pendingUnregistrations.Clear();
        }
    }
}
