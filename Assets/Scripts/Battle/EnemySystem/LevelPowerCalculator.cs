using UnityEngine;

namespace Battle
{
    public class LevelPowerCalculator
    {
        private readonly float _basePower;
        private readonly float _flatIncrease;
        private readonly float _exponent;

        public LevelPowerCalculator(float basePower, float flatIncrease, float exponent)
        {
            _basePower = Mathf.Max(0.01f, basePower);
            _flatIncrease = Mathf.Max(0f, flatIncrease);
            _exponent = Mathf.Max(1f, exponent);
        }

        public float Calculate(int level)
        {
            int normalizedLevel = Mathf.Max(1, level);
            float power = _basePower;

            for (int currentLevel = 2; currentLevel <= normalizedLevel; currentLevel++)
                power = Mathf.Pow(power + _flatIncrease, _exponent);

            return power;
        }
    }
}
