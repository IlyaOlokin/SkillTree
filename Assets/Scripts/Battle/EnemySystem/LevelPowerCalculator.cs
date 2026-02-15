using UnityEngine;

namespace Battle
{
    public class LevelPowerCalculator
    {
        private const float BasePower = 10f;
        private const float GrowthRate = 1.12f;

        public float Calculate(int level)
        {
            return Mathf.Pow(GrowthRate, level) * BasePower;
        }
    }
}
