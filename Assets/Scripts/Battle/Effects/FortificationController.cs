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
        private BaseModifier _mitigationModifier;
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
                UpdateMitigationBonus();
            }
            finally { _refreshing = false; }
        }

        private void UpdateMitigationBonus()
        {
            float value = Count * FortificationEffect.MitigationPerStack;
            if (_mitigationModifier != null && _mitigationModifier.modifierContainer.value == value) return;

            if (_mitigationModifier != null) _owner.RemoveOuterModifier(_mitigationModifier);
            if (value <= 0f)
            {
                if (_mitigationModifier != null) UnityEngine.Object.Destroy(_mitigationModifier);
                _mitigationModifier = null;
                return;
            }

            if (_mitigationModifier == null)
                _mitigationModifier = ScriptableObject.CreateInstance<BaseModifier>();
            // Stacks add percentage points through the ordinary mitigation stat.
            _mitigationModifier.modifierContainer = new ModifierContainer(ModifierType.Added, StatType.DamageMitigation, value);
            _owner.AddOuterModifier(_mitigationModifier);
        }

        public void Dispose()
        {
            _owner.OnStatsRecalculated -= UpdateLimits;
            _owner.OnCombatStateReset -= RefreshStacks;
            if (_effects != null) _effects.OnEffectsChanged -= RefreshStacks;
            if (_mitigationModifier != null)
            {
                _owner.RemoveOuterModifier(_mitigationModifier);
                UnityEngine.Object.Destroy(_mitigationModifier);
                _mitigationModifier = null;
            }
        }
    }
}
