using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Modules/Attack Speed")]
    public class EnemyAttackSpeedModule : ScriptableObject
    {
        [Min(0f)] [SerializeField] private float baseAttackSpeed = 1f;
        [Min(0f)] [SerializeField] private float powerMultiplier = 1f;

        public float BaseAttackSpeed => Mathf.Max(0f, baseAttackSpeed);
        public float PowerMultiplier => Mathf.Max(0f, powerMultiplier);

        private void OnValidate()
        {
            baseAttackSpeed = Mathf.Max(0f, baseAttackSpeed);
            powerMultiplier = Mathf.Max(0f, powerMultiplier);
        }

        public float ApplyPowerMultiplier(float power)
        {
            return Mathf.Max(0f, power) * PowerMultiplier;
        }
    }
}
