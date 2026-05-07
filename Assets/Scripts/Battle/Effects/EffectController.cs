using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    public class EffectController : MonoBehaviour, IUnitComponent
    {
        private Unit _owner;

        public readonly List<ActiveEffect> Effects = new List<ActiveEffect>();

        public void Init(Unit owner)
        {
            _owner = owner;
        }

        public void AddEffect(BaseEffect newEffect)
        {
            var existing = Effects
                .Find(e => e.Effect.GetType() == newEffect.GetType());

            if (existing != null)
            {
                existing.Effect.OnStack(_owner, newEffect, existing);
                if (existing.Effect.IsStackable) return;
            }

            var active = new ActiveEffect(newEffect);

            Effects.Add(active);
            newEffect.OnApply(_owner);
        }


        public void CombatTick(float deltaTime)
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                var e = Effects[i];

                e.Effect.OnTick(_owner, deltaTime);

                if (e.Effect.IsReadyToBeRemoved(_owner))
                {
                    e.Effect.OnRemove(_owner);
                    Effects.RemoveAt(i);
                    continue;
                }

                if (e.TimeLeft < 0)
                {
                    continue;
                }

                e.TimeLeft -= deltaTime;
                if (e.TimeLeft <= 0)
                {
                    e.Effect.OnRemove(_owner);
                    Effects.RemoveAt(i);
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
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                if (Effects[i].Effect.GetType() != typeof(T))
                {
                    continue;
                }

                Effects[i].Effect.OnRemove(_owner);
                Effects.RemoveAt(i);
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

            Effects[index].Effect.OnRemove(_owner);
            Effects.RemoveAt(index);
        }

        public void ClearAllEffects()
        {
            for (int i = Effects.Count - 1; i >= 0; i--)
            {
                Effects[i].Effect.OnRemove(_owner);
            }
            Effects.Clear();
        }
    }
}

