using System;
using System.Collections.Generic;

namespace Battle
{
    public sealed class AttackContext
    {
        private readonly List<QueuedEffectConsumption> _effectsToConsume = new List<QueuedEffectConsumption>();
        private readonly List<Action> _postSuccessfulHitActions = new List<Action>();

        public Unit Attacker { get; }
        public ITarget Defender { get; }
        public DamageInfo DamageInfo { get; }
        public bool IsEvaded { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsHitConfirmed => !IsEvaded;

        public IReadOnlyList<QueuedEffectConsumption> EffectsToConsume => _effectsToConsume;

        public AttackContext(Unit attacker, ITarget defender, DamageInfo damageInfo)
        {
            Attacker = attacker;
            Defender = defender;
            DamageInfo = damageInfo;
        }

        public void QueueEffectConsumption(Unit owner, ActiveEffect activeEffect)
        {
            if (owner == null || activeEffect?.Effect == null)
            {
                return;
            }

            for (int i = 0; i < _effectsToConsume.Count; i++)
            {
                if (_effectsToConsume[i].ActiveEffect == activeEffect)
                {
                    return;
                }
            }

            _effectsToConsume.Add(new QueuedEffectConsumption(owner, activeEffect));
        }

        public void ConsumeQueuedEffects()
        {
            for (int i = 0; i < _effectsToConsume.Count; i++)
            {
                QueuedEffectConsumption queued = _effectsToConsume[i];
                queued.ActiveEffect.Effect.Consume(queued.Owner, queued.ActiveEffect);
                if (queued.ActiveEffect.Effect.IsReadyToBeRemoved(queued.Owner))
                {
                    queued.Owner?.effectController?.RemoveEffect(queued.ActiveEffect);
                }
            }

            _effectsToConsume.Clear();
        }

        public void QueuePostSuccessfulHitAction(Action action)
        {
            if (action == null)
            {
                return;
            }

            _postSuccessfulHitActions.Add(action);
        }

        public void ResolveSuccessfulHitSideEffects()
        {
            for (int i = 0; i < _postSuccessfulHitActions.Count; i++)
            {
                _postSuccessfulHitActions[i]?.Invoke();
            }

            _postSuccessfulHitActions.Clear();
        }
    }

    public readonly struct QueuedEffectConsumption
    {
        public Unit Owner { get; }
        public ActiveEffect ActiveEffect { get; }

        public QueuedEffectConsumption(Unit owner, ActiveEffect activeEffect)
        {
            Owner = owner;
            ActiveEffect = activeEffect;
        }
    }
}
