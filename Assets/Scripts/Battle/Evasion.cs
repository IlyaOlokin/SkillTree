using UnityEngine;

namespace Battle
{
    public static class Evasion
    {
        public const float EqualRatingHitChance = 0.8f;
        public const float RatingStability = 5f;
        public const float MinimumHitChance = 0.01f;
        public const float MaximumHitChance = 1f;

        public static bool ApplyEvasion(Unit defender, Unit attacker)
        {
            float evasion = defender.BaseUnitModifiers.GetStatValue(StatType.Evasion);
            
            float accuracy = attacker.BaseUnitModifiers.GetStatValue(StatType.Accuracy);
            float dodgeChance = CalculateDodgeChance(evasion, accuracy);
            return Random.Range(0f,1f) < dodgeChance;
        }

        public static float CalculateDodgeChance(float evasion, float accuracy)
        {
            return Mathf.Clamp(
                CalculateRawDodgeChance(evasion, accuracy),
                0f,
                1f - MinimumHitChance);
        }

        public static float CalculateHitChance(float accuracy, float evasion)
        {
            return Mathf.Clamp(
                CalculateRawHitChance(accuracy, evasion),
                MinimumHitChance,
                MaximumHitChance);
        }

        public static float CalculateRawHitChance(float accuracy, float evasion)
        {
            accuracy = Mathf.Max(0f, accuracy);
            evasion = Mathf.Max(0f, evasion);

            if (evasion <= 0f)
                return MaximumHitChance;

            return EqualRatingHitChance * (accuracy + RatingStability) / evasion;
        }

        public static float CalculateRawDodgeChance(float evasion, float accuracy)
        {
            return 1f - CalculateRawHitChance(accuracy, evasion);
        }
    }
}
