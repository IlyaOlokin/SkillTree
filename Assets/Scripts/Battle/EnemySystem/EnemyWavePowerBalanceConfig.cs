using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Wave Power Balance Config")]
    public class EnemyWavePowerBalanceConfig : ScriptableObject
    {
        [SerializeField, Min(0.01f)] private float firstWaveMultiplier = 0.85f;
        [SerializeField, Min(0.01f)] private float lastWaveMultiplier = 1.15f;
        [SerializeField, Min(0.01f)] private float bossWaveMultiplier = 1.5f;

        public float GetMultiplier(WaveContext context)
        {
            if (context.IsBossWave)
                return bossWaveMultiplier;

            if (context.WavesInLevel <= 1)
                return context.IsLastWave ? lastWaveMultiplier : firstWaveMultiplier;

            float t = Mathf.InverseLerp(1f, context.WavesInLevel, context.WaveIndex);
            return Mathf.Lerp(firstWaveMultiplier, lastWaveMultiplier, t);
        }
    }
}
