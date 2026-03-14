using UnityEngine;

namespace Battle
{
    public class LevelPowerCalculator
    {
        private readonly float _basePower;
        private readonly float _growthRate;

        public LevelPowerCalculator(float basePower, float growthRate)
        {
            _basePower = Mathf.Max(0.01f, basePower);
            _growthRate = Mathf.Max(1f, growthRate);
        }

        public float Calculate(int level)
        {
            return Mathf.Pow(_growthRate, level) * _basePower;
        }
    }
}
