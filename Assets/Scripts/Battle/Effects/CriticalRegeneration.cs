using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    public class CriticalRegeneration : BaseEffect
    {
        private readonly float _maxHealthRegenerationPerSecond;
        private PercentHealthRegeneration _regenerationModifier;

        public override bool IsStackable { get; set; } = false;
        public override EffectVisualType VisualType => EffectVisualType.CriticalRegeneration;

        public CriticalRegeneration(float duration, float maxHealthRegenerationPerSecond)
        {
            Duration = Mathf.Max(0f, duration);
            _maxHealthRegenerationPerSecond = Mathf.Max(0f, maxHealthRegenerationPerSecond);
        }

        public override void OnApply(Unit unit)
        {
            if (unit == null || _maxHealthRegenerationPerSecond <= 0f)
            {
                return;
            }

            _regenerationModifier = ScriptableObject.CreateInstance<PercentHealthRegeneration>();
            _regenerationModifier.Initialize(_maxHealthRegenerationPerSecond);
            unit.AddOuterModifier(_regenerationModifier);
        }

        public override void OnRemove(Unit unit)
        {
            if (unit == null || _regenerationModifier == null)
            {
                return;
            }

            unit.RemoveOuterModifier(_regenerationModifier);
            _regenerationModifier = null;
        }

        public override string GetIconText(IReadOnlyList<ActiveEffect> activeEffects)
        {
            return activeEffects != null && activeEffects.Count > 1 ? activeEffects.Count.ToString() : string.Empty;
        }

        protected override string GetDescriptionId()
        {
            return "criticalRegeneration";
        }
    }
}
