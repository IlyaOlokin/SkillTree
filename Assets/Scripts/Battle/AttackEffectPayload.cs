using System;
using System.Collections.Generic;

namespace Battle
{
    public sealed class AttackEffectPayload
    {
        private readonly HashSet<Type> _guaranteedEffects = new HashSet<Type>();
        private readonly HashSet<Type> _effectsRedirectedToOwner = new HashSet<Type>();

        public void Reset()
        {
            _guaranteedEffects.Clear();
            _effectsRedirectedToOwner.Clear();
        }

        public void Guarantee<T>() where T : BaseEffect
        {
            _guaranteedEffects.Add(typeof(T));
        }

        public void RedirectToOwner<T>() where T : BaseEffect
        {
            _effectsRedirectedToOwner.Add(typeof(T));
        }

        public bool IsGuaranteed<T>() where T : BaseEffect
        {
            return IsGuaranteed(typeof(T));
        }

        public bool IsRedirectedToOwner<T>() where T : BaseEffect
        {
            return IsRedirectedToOwner(typeof(T));
        }

        public bool IsGuaranteed(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _guaranteedEffects.Contains(effectType);
        }

        public bool IsRedirectedToOwner(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _effectsRedirectedToOwner.Contains(effectType);
        }
    }
}
