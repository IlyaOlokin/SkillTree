using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class EffectController : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;
        private bool _isClearing;

        public readonly List<ActiveEffect> Effects = new List<ActiveEffect>();
        public event Action<Func<BaseEffect>> OnEffectReceived;
        public event Action OnEffectsChanged;
        public event Action<BaseEffect> OnEffectAdded;

        public void Init(Unit owner)
        {
            _owner = owner;
        }

        public void AddEffect(BaseEffect newEffect)
        {
            AddEffect(newEffect, null, true);
        }

        public void AddEffect(Func<BaseEffect> effectFactory)
        {
            AddEffect(effectFactory, null);
        }

        // Pass the source for effects that should notify its application listeners.
        // The factory must create a fresh effect and is intended for synchronous repeats.
        public void AddEffect(Func<BaseEffect> effectFactory, Unit source)
        {
            if (effectFactory == null)
            {
                return;
            }

            BaseEffect effect = effectFactory();
            if (AddEffect(effect, effectFactory, true) && source != null && _owner != null)
            {
                source.EffectApplied(_owner, effect.GetType(), effectFactory);
            }
        }

        public bool AddRepeatedEffect(Func<BaseEffect> effectFactory)
        {
            if (effectFactory == null)
            {
                return false;
            }

            // Repeats notify neither received-effect nor source-side application listeners.
            return AddEffect(effectFactory(), null, false);
        }

        private bool AddEffect(BaseEffect newEffect, Func<BaseEffect> repeatFactory, bool notifyReceived)
        {
            if (newEffect == null)
            {
                return false;
            }

            if (_isClearing)
            {
                newEffect.ReleaseRuntimeModifiers();
                return false;
            }

            var existing = Effects
                .Find(e => e.Effect.GetType() == newEffect.GetType());

            if (existing != null)
            {
                existing.Effect.OnStack(_owner, newEffect, existing);
                OnEffectsChanged?.Invoke();
                if (existing.Effect.IsStackable)
                {
                    if (!ReferenceEquals(existing.Effect, newEffect))
                        newEffect.ReleaseRuntimeModifiers();
                    NotifyEffectReceived(repeatFactory, notifyReceived);
                    OnEffectAdded?.Invoke(existing.Effect);
                    return true;
                }
            }

            var active = new ActiveEffect(newEffect);

            Effects.Add(active);
            newEffect.OnApply(_owner);
            OnEffectsChanged?.Invoke();

            NotifyEffectReceived(repeatFactory, notifyReceived);
            OnEffectAdded?.Invoke(newEffect);
            return true;
        }

        private void NotifyEffectReceived(Func<BaseEffect> repeatFactory, bool notifyReceived)
        {
            if (notifyReceived && repeatFactory != null)
            {
                OnEffectReceived?.Invoke(repeatFactory);
            }
        }


        public void CombatTick(float deltaTime)
        {
            // Tick callbacks can clear effects, kill the owner, or add replacements.
            var snapshot = Effects.ToArray();
            for (int i = snapshot.Length - 1; i >= 0; i--)
            {
                var e = snapshot[i];
                if (!Effects.Contains(e)) continue;

                float tickDuration = e.TimeLeft < 0f
                    ? Mathf.Max(0f, deltaTime)
                    : Mathf.Min(Mathf.Max(0f, deltaTime), e.TimeLeft);
                e.Effect.OnTick(_owner, tickDuration);
                if (!Effects.Contains(e)) continue;

                if (e.Effect.IsReadyToBeRemoved(_owner))
                {
                    RemoveEffect(e);
                    continue;
                }

                if (e.TimeLeft < 0)
                {
                    continue;
                }

                e.TimeLeft -= tickDuration;
                if (e.TimeLeft <= 0)
                {
                    RemoveEffect(e);
                }
            }
        }

        public List<ActiveEffect> GetAllEffectsOfType<T>()
        {
            var result = new List<ActiveEffect>();
            foreach (var effect in Effects)
            {
                if (effect.Effect.GetType() == typeof(T))
                {
                    result.Add(effect);
                }
            }
            return result;
        }

        public bool HasEffect<T>() where T : BaseEffect
        {
            for (int i = 0; i < Effects.Count; i++)
            {
                if (Effects[i].Effect.GetType() == typeof(T)) return true;
            }

            return false;
        }

        public bool HasEffectOfVisualType(EffectVisualType effectType)
        {
            if (effectType == EffectVisualType.None)
            {
                return false;
            }

            foreach (ActiveEffect activeEffect in Effects)
            {
                if (activeEffect?.Effect != null && activeEffect.Effect.VisualType == effectType)
                {
                    return true;
                }
            }

            return false;
        }

        public void RemoveEffectsOfType<T>()
        {
            var snapshot = GetAllEffectsOfType<T>();
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                RemoveEffect(snapshot[i]);
            }
        }

        public void RemoveEffect(ActiveEffect activeEffect)
        {
            if (activeEffect == null)
            {
                return;
            }

            int index = Effects.IndexOf(activeEffect);
            if (index < 0)
            {
                return;
            }

            Effects.RemoveAt(index);
            try
            {
                activeEffect.Effect.OnRemove(_owner);
            }
            finally
            {
                activeEffect.Effect.ReleaseRuntimeModifiers();
            }
            if (!_isClearing) OnEffectsChanged?.Invoke();
        }

        public void ClearAllEffects()
        {
            if (_isClearing) return;
            bool hadEffects = Effects.Count > 0;
            _isClearing = true;
            try
            {
                while (Effects.Count > 0)
                    RemoveEffect(Effects[Effects.Count - 1]);
            }
            finally
            {
                _isClearing = false;
            }
            if (hadEffects) OnEffectsChanged?.Invoke();
        }

        private void OnDestroy()
        {
            ClearAllEffects();
        }
    }
}

