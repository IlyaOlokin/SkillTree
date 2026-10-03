using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Modules/Core Profile")]
    public class EnemyCoreProfile : ScriptableObject
    {
        [Header("Category Weights")]
        [Range(0f, 1f)] public float healthWeight = 1f;
        [Range(0f, 1f)] public float offenceWeight;
        [Range(0f, 1f)] public float defenceWeight;
        [Range(0f, 1f)] public float utilityWeight;

        [Header("Extra Modifiers")]
        [SerializeField] private List<ModifierContainer> extraModifiers = new();

        public IReadOnlyList<ModifierContainer> ExtraModifiers => extraModifiers;

        private void OnValidate()
        {
            healthWeight = Mathf.Clamp01(healthWeight);
            offenceWeight = Mathf.Clamp01(offenceWeight);
            defenceWeight = Mathf.Clamp01(defenceWeight);
            utilityWeight = Mathf.Clamp01(utilityWeight);
            EnemyWeightMath.NormalizeToOne(ref healthWeight, ref offenceWeight, ref defenceWeight, ref utilityWeight);
        }

        public float GetTotalCategoryWeight()
        {
            return Mathf.Max(0f, healthWeight) +
                   Mathf.Max(0f, offenceWeight) +
                   Mathf.Max(0f, defenceWeight) +
                   Mathf.Max(0f, utilityWeight);
        }
    }

}
