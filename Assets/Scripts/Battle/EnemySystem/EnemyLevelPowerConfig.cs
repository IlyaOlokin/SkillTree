using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Level Power Config")]
    public class EnemyLevelPowerConfig : ScriptableObject
    {
        private const float FallbackPower = 10f;
        
        [SerializeField] private List<float> levelPowers = new();
        
        public IReadOnlyList<float> LevelPowers => levelPowers;
        public int LevelCount => levelPowers?.Count ?? 0;

        private void OnValidate()
        {
            levelPowers ??= new List<float>();
            if (levelPowers.Count == 0)
                levelPowers.Add(FallbackPower);

            for (int i = 0; i < levelPowers.Count; i++)
                levelPowers[i] = Mathf.Max(0.01f, levelPowers[i]);
        }

        public float GetPowerForLevel(int level, Object context = null)
        {
            return GetPowerForLevel(levelPowers, level, context != null ? context : this);
        }

        private static float GetPowerForLevel(IReadOnlyList<float> powers, int level, Object context)
        {
            if (powers == null || powers.Count == 0)
            {
                Debug.LogWarning($"{nameof(EnemyLevelPowerConfig)} has no powers configured. Using fallback power {FallbackPower}.", context);
                return FallbackPower;
            }

            int clampedLevel = Mathf.Clamp(level, 1, powers.Count);
            if (clampedLevel != level)
            {
                Debug.LogWarning(
                    $"{nameof(EnemyLevelPowerConfig)} has no power for level {level}. Clamping to level {clampedLevel}.",
                    context);
            }

            return powers[clampedLevel - 1];
        }
    }
}
