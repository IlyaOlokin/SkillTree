using System.Collections.Generic;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Config Database")]
    public class EnemyConfigDatabase : ScriptableObject
    {
        [Header("Enemy selection")]
        public List<EnemyArchetype> archetypes = new();

        [Header("Power scaling")]
        [SerializeField, Min(0.01f)] private float basePower = 10f;
        [SerializeField, Min(1f)] private float powerGrowthRate = 1.12f;

        [Header("Wave progression")]
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int wavesToUnlockNextLevel = 10;
        [SerializeField, Min(1)] private int maxWaveLevel = 100;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        [Header("Wave composition")]
        [SerializeField, Range(1, 3)] private int maxEnemiesPerWave = 3;
        
        [Header("Global enemy modifiers")]
        [SerializeField] private List<ModifierContainer> globalModifiers = new();

        public float BasePower => basePower;
        public float PowerGrowthRate => powerGrowthRate;
        public int StartingLevel => startingLevel;
        public int WavesToUnlockNextLevel => wavesToUnlockNextLevel;
        public int MaxWaveLevel => maxWaveLevel;
        public float RespawnDelay => respawnDelay;
        public int MaxEnemiesPerWave => maxEnemiesPerWave;
        public IReadOnlyList<ModifierContainer> GlobalModifiers => globalModifiers;

        public EnemyArchetype GetRandomArchetype()
        {
            if (archetypes == null || archetypes.Count == 0)
            {
                Debug.LogError($"{nameof(EnemyConfigDatabase)} has no enemy archetypes assigned.", this);
                return null;
            }

            return archetypes[Random.Range(0, archetypes.Count)];
        }
    }
}
