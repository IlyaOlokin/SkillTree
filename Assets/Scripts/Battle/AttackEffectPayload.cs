using System;
using System.Collections.Generic;

namespace Battle
{
    public sealed class AttackEffectPayload
    {
        private readonly HashSet<Type> _guaranteedEffects = new HashSet<Type>();

        public void Reset()
        {
            _guaranteedEffects.Clear();
        }

        public void Guarantee<T>() where T : BaseEffect
        {
            _guaranteedEffects.Add(typeof(T));
        }

        public void Guarantee(Type effectType)
        {
            if (effectType == null)
            {
                throw new ArgumentNullException(nameof(effectType));
            }

            if (!typeof(BaseEffect).IsAssignableFrom(effectType))
            {
                throw new ArgumentException("Effect type must inherit from BaseEffect.", nameof(effectType));
            }

            _guaranteedEffects.Add(effectType);
        }

        public bool IsGuaranteed<T>() where T : BaseEffect
        {
            return IsGuaranteed(typeof(T));
        }

        public bool IsGuaranteed(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _guaranteedEffects.Contains(effectType);
        }
    }
}
