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

                if (e.TimeLeft < 0)
                {
                    if (Effects[i].Effect.IsReadyToBeRemoved(_owner))
                    {
                        e.Effect.OnRemove(_owner);
                        Effects.RemoveAt(i);
                    }
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

