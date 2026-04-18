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
        [SerializeField, Min(0f)] private float powerFlatIncrease = 10f;
        [SerializeField, Min(1f)] private float powerExponent = 1f;

        [Header("Wave progression")]
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int wavesToUnlockNextLevel = 10;
        [SerializeField, Min(1)] private int maxWaveLevel = 100;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        [Header("Wave composition")]
        [SerializeField, Range(1, 3)] private int maxEnemiesPerWave = 3;

        [Header("Rarity balance")]
        [SerializeField] private EnemyRarityBalanceConfig rarityBalance;

        [Header("Boss balance")]
        [SerializeField] private EnemyBossBalanceConfig bossBalance;

        [Header("Wave power balance")]
        [SerializeField] private EnemyWavePowerBalanceConfig wavePowerBalance;

        [Header("Stat budget rules")]
        [SerializeField] private EnemyStatBudgetConfig statBudgetConfig;
        
        [Header("Global enemy modifiers")]
        [SerializeField] private List<ModifierContainer> globalModifiers = new();

        public float BasePower => basePower;
        public float PowerFlatIncrease => powerFlatIncrease;
        public float PowerExponent => powerExponent;
        public int StartingLevel => startingLevel;
        public int WavesToUnlockNextLevel => wavesToUnlockNextLevel;
        public int MaxWaveLevel => maxWaveLevel;
        public float RespawnDelay => respawnDelay;
        public int MaxEnemiesPerWave => maxEnemiesPerWave;
        public EnemyRarityBalanceConfig RarityBalance => rarityBalance;
        public EnemyBossBalanceConfig BossBalance => bossBalance;
        public EnemyWavePowerBalanceConfig WavePowerBalance => wavePowerBalance;
        public EnemyStatBudgetConfig StatBudgetConfig => statBudgetConfig;
        public IReadOnlyList<ModifierContainer> GlobalModifiers => globalModifiers;

        public EnemyArchetype GetRandomArchetype(WaveContext context, EnemyRarity rarity)
        {
            if (archetypes == null || archetypes.Count == 0)
            {
                Debug.LogError($"{nameof(EnemyConfigDatabase)} has no enemy archetypes assigned.", this);
                return null;
            }

            var matchingArchetypes = new List<EnemyArchetype>();

            for (int i = 0; i < archetypes.Count; i++)
            {
                var archetype = archetypes[i];
                if (archetype != null && archetype.Matches(context, rarity))
                    matchingArchetypes.Add(archetype);
            }

            if (matchingArchetypes.Count > 0)
                return matchingArchetypes[Random.Range(0, matchingArchetypes.Count)];

            Debug.LogWarning(
                $"{nameof(EnemyConfigDatabase)} found no matching archetypes for level {context.Level}, wave {context.WaveIndex}, rarity {rarity}. Falling back to any archetype.",
                this);

            return archetypes[Random.Range(0, archetypes.Count)];
        }
    }
}
