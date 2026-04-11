using System;
using SkillTree;
using UnityEngine;

namespace Gems
{
    [Serializable]
    public class GemModifierRollDefinition
    {
        [SerializeField] private Modifier modifierTemplate;
        [SerializeField] private float minRollValue;
        [SerializeField] private float maxRollValue;
        [SerializeField] [Min(0.0001f)] private float rollStep = 1f;

        public Modifier ModifierTemplate => modifierTemplate;
        public float MinRollValue => minRollValue;
        public float MaxRollValue => maxRollValue;
        public float RollStep => rollStep;

        public float RollValue()
        {
            float min = Mathf.Min(minRollValue, maxRollValue);
            float max = Mathf.Max(minRollValue, maxRollValue);
            float step = Mathf.Max(0.0001f, rollStep);

            if (Mathf.Approximately(min, max))
                return min;

            int stepCount = Mathf.Max(1, Mathf.RoundToInt((max - min) / step));
            int stepIndex = UnityEngine.Random.Range(0, stepCount + 1);
            float rolledValue = min + stepIndex * step;
            return Mathf.Clamp(rolledValue, min, max);
        }

        public Modifier CreateRolledModifier(float rolledValue)
        {
            return ModifierRollUtility.CreateRolledModifier(modifierTemplate, rolledValue);
        }

        public string CreateRolledDescription(float rolledValue)
        {
            Modifier rolledModifier = CreateRolledModifier(rolledValue);
            if (rolledModifier == null)
                return string.Empty;

            try
            {
                return rolledModifier.GetDescription();
            }
            finally
            {
                ModifierRollUtility.DestroyModifier(rolledModifier);
            }
        }
    }
}
