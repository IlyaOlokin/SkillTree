using System;
using SkillTree;
using UnityEngine;

namespace Battle
{
    // Owns stack rules; numeric contributions belong to the ordinary stat system.
    public sealed class FortificationController : IDisposable
    {
        private readonly Unit _owner;
        private readonly EffectController _effects;
        private bool _refreshing;
        private BaseModifier _defenceModifier;
        public int Minimum { get; private set; }
        public int Maximum { get; private set; } = FortificationEffect.MaxStacks;

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var active in _effects.Effects)
                    if (active.Effect is FortificationEffect) count++;
                return count;
            }
        }

        public FortificationController(Unit owner)
        {
            _owner = owner;
            _effects = owner.effectController;
            _owner.OnStatsRecalculated += UpdateLimits;
            _effects.OnEffectsChanged += RefreshStacks;
            _owner.OnCombatStateReset += RefreshStacks;
        }

        public static bool IsStackStat(StatType type)
            => type == StatType.MinimumFortificationStacks || type == StatType.MaximumFortificationStacks;

        public static float NormalizeStat(StatType type, float value)
            => IsStackStat(type) ? Mathf.Floor(value) : value;

        private static int ToCount(float value)
        {
            if (float.IsNaN(value) || value <= 0f) return 0;
            if (value >= int.MaxValue) return int.MaxValue;
            return Mathf.FloorToInt(value);
        }

        private void UpdateLimits()
        {
            Maximum = ToCount(_owner.BaseUnitModifiers.GetStatValue(StatType.MaximumFortificationStacks));
            Minimum = Mathf.Min(Maximum,
                ToCount(_owner.BaseUnitModifiers.GetStatValue(StatType.MinimumFortificationStacks)));
            RefreshStacks();
        }

        private void RefreshStacks()
        {
            if (_refreshing || _effects == null) return;
            _refreshing = true;
            try
            {
                var stacks = _effects.GetAllEffectsOfType<FortificationEffect>();
                for (int i = stacks.Count - 1; i >= Maximum; i--)
                    _effects.RemoveEffect(stacks[i]);
                for (int i = Count; i < Minimum; i++)
                    _effects.AddEffect(new FortificationEffect());
                UpdateDefenceBonus();
            }
            finally { _refreshing = false; }
        }

        private void UpdateDefenceBonus()
        {
            float value = Count * FortificationEffect.DefencePerStack;
            if (_defenceModifier != null && _defenceModifier.modifierContainer.value == value) return;

            if (_defenceModifier != null) _owner.RemoveOuterModifier(_defenceModifier);
            if (value <= 0f)
            {
                if (_defenceModifier != null) UnityEngine.Object.Destroy(_defenceModifier);
                _defenceModifier = null;
                return;
            }

            if (_defenceModifier == null)
                _defenceModifier = ScriptableObject.CreateInstance<BaseModifier>();
            // One aggregate More contribution, never one multiplier per stack.
            _defenceModifier.modifierContainer = new ModifierContainer(ModifierType.More, StatType.Defence, value);
            _owner.AddOuterModifier(_defenceModifier);
        }

        public void Dispose()
        {
            _owner.OnStatsRecalculated -= UpdateLimits;
            _owner.OnCombatStateReset -= RefreshStacks;
            if (_effects != null) _effects.OnEffectsChanged -= RefreshStacks;
            if (_defenceModifier != null)
            {
                _owner.RemoveOuterModifier(_defenceModifier);
                UnityEngine.Object.Destroy(_defenceModifier);
                _defenceModifier = null;
            }
        }
    }
}
