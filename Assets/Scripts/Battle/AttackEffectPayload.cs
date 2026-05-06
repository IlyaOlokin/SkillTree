using System;
using System.Collections.Generic;
using SkillTree;

namespace Battle
{
    public sealed class AttackEffectPayload
    {
        private readonly HashSet<Type> _guaranteedEffects = new HashSet<Type>();
        private readonly HashSet<Type> _suppressedEffects = new HashSet<Type>();
        private readonly HashSet<Type> _effectsRedirectedToOwner = new HashSet<Type>();
        private readonly Dictionary<Type, List<ModifierContainer>> _effectModifiers =
            new Dictionary<Type, List<ModifierContainer>>();

        public void Reset()
        {
            _guaranteedEffects.Clear();
            _suppressedEffects.Clear();
            _effectsRedirectedToOwner.Clear();
            _effectModifiers.Clear();
        }

        public void Guarantee<T>() where T : BaseEffect
        {
            _guaranteedEffects.Add(typeof(T));
        }

        public void Suppress<T>() where T : BaseEffect
        {
            _suppressedEffects.Add(typeof(T));
        }

        public void RedirectToOwner<T>() where T : BaseEffect
        {
            _effectsRedirectedToOwner.Add(typeof(T));
        }

        public bool IsGuaranteed<T>() where T : BaseEffect
        {
            return IsGuaranteed(typeof(T));
        }

        public bool IsSuppressed<T>() where T : BaseEffect
        {
            return IsSuppressed(typeof(T));
        }

        public bool IsRedirectedToOwner<T>() where T : BaseEffect
        {
            return IsRedirectedToOwner(typeof(T));
        }

        public void AddEffectModifier<T>(ModifierContainer modifier) where T : BaseEffect
        {
            AddEffectModifier(typeof(T), modifier);
        }

        public void AddEffectModifier(Type effectType, ModifierContainer modifier)
        {
            if (effectType == null || modifier == null)
            {
                return;
            }

            if (!_effectModifiers.TryGetValue(effectType, out List<ModifierContainer> modifiers))
            {
                modifiers = new List<ModifierContainer>();
                _effectModifiers.Add(effectType, modifiers);
            }

            modifiers.Add(CloneModifier(modifier));
        }

        public IReadOnlyList<ModifierContainer> GetEffectModifiers<T>() where T : BaseEffect
        {
            return GetEffectModifiers(typeof(T));
        }

        public IReadOnlyList<ModifierContainer> GetEffectModifiers(Type effectType)
        {
            if (effectType == null ||
                !_effectModifiers.TryGetValue(effectType, out List<ModifierContainer> modifiers))
            {
                return Array.Empty<ModifierContainer>();
            }

            return modifiers;
        }

        public bool IsGuaranteed(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _guaranteedEffects.Contains(effectType);
        }

        public bool IsSuppressed(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _suppressedEffects.Contains(effectType);
        }

        public bool IsRedirectedToOwner(Type effectType)
        {
            if (effectType == null)
            {
                return false;
            }

            return _effectsRedirectedToOwner.Contains(effectType);
        }

        private static ModifierContainer CloneModifier(ModifierContainer modifier)
        {
            return new ModifierContainer(modifier.modifierType, modifier.statType, modifier.value);
        }
    }
}
