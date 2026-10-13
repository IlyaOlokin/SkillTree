using System;
using UnityEngine;

namespace Battle
{
    // Owns stack rules; numeric contributions belong to the ordinary stat system.
    public sealed class FortificationController : IDisposable
    {
        private readonly Unit _owner;
        private readonly EffectController _effects;
        private bool _refreshing;
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
            }
            finally { _refreshing = false; }
        }

        public void Dispose()
        {
            _owner.OnStatsRecalculated -= UpdateLimits;
            _owner.OnCombatStateReset -= RefreshStacks;
            if (_effects != null) _effects.OnEffectsChanged -= RefreshStacks;
        }
    }
}
