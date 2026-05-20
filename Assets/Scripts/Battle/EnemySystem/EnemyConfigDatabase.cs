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

        [Header("Level range")]
        [SerializeField] private EnemyLevelPowerConfig levelPowerConfig;
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int maxLevel = 1;

        [Header("Wave progression")]
        [SerializeField, Min(1)] private int wavesToUnlockNextLevel = 10;
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

        public EnemyLevelPowerConfig LevelPowerConfig => levelPowerConfig;
        public IReadOnlyList<float> LevelPowers => levelPowerConfig.LevelPowers;
        public int StartingLevel => startingLevel;
        public int WavesToUnlockNextLevel => wavesToUnlockNextLevel;
        public int MaxWaveLevel => Mathf.Max(startingLevel, maxLevel);
        public float RespawnDelay => respawnDelay;
        public int MaxEnemiesPerWave => maxEnemiesPerWave;
        public EnemyRarityBalanceConfig RarityBalance => rarityBalance;
        public EnemyBossBalanceConfig BossBalance => bossBalance;
        public EnemyWavePowerBalanceConfig WavePowerBalance => wavePowerBalance;
        public EnemyStatBudgetConfig StatBudgetConfig => statBudgetConfig;
        public IReadOnlyList<ModifierContainer> GlobalModifiers => globalModifiers;

        private void OnValidate()
        {
            startingLevel = Mathf.Max(1, startingLevel);
            maxLevel = Mathf.Max(startingLevel, maxLevel);
            wavesToUnlockNextLevel = Mathf.Max(1, wavesToUnlockNextLevel);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            maxEnemiesPerWave = Mathf.Clamp(maxEnemiesPerWave, 1, 3);
        }

        public float GetPowerForLevel(int level)
        {
            int clampedLevel = Mathf.Clamp(level, StartingLevel, MaxWaveLevel);
            return levelPowerConfig.GetPowerForLevel(clampedLevel, this);

        }

        public int GetLevelPowerCount()
        {
            return MaxWaveLevel - StartingLevel + 1;
        }

        public EnemyArchetype GetRandomArchetype(WaveContext context, EnemyRarity rarity, int enemyIndex = 0)
        {
            if (archetypes == null || archetypes.Count == 0)
            {
                Debug.LogError($"{nameof(EnemyConfigDatabase)} has no enemy archetypes assigned.", this);
                return null;
            }

            IReadOnlyList<EnemyArchetype> archetypePool = GetArchetypePool(context, rarity, enemyIndex);
            var matchingArchetypes = new List<EnemyArchetype>();

            for (int i = 0; i < archetypePool.Count; i++)
            {
                var archetype = archetypePool[i];
                if (archetype != null && archetype.Matches(context, rarity))
                    matchingArchetypes.Add(archetype);
            }

            if (matchingArchetypes.Count > 0)
                return matchingArchetypes[Random.Range(0, matchingArchetypes.Count)];

            Debug.LogWarning(
                $"{nameof(EnemyConfigDatabase)} found no matching archetypes for level {context.Level}, wave {context.WaveIndex}, rarity {rarity}. Falling back to any archetype from the selected pool.",
                this);

            return GetRandomAnyArchetype(archetypePool);
        }

        private IReadOnlyList<EnemyArchetype> GetArchetypePool(WaveContext context, EnemyRarity rarity, int enemyIndex)
        {
            if (context.IsBossWave &&
                bossBalance != null &&
                bossBalance.TryGetRule(context, out var bossRule))
            {
                IReadOnlyList<EnemyArchetype> specificPool = bossRule.GetArchetypePool(enemyIndex, rarity);
                if (specificPool is { Count: > 0 })
                    return specificPool;
            }

            return archetypes;
        }

        private EnemyArchetype GetRandomAnyArchetype(IReadOnlyList<EnemyArchetype> archetypePool)
        {
            var availableArchetypes = new List<EnemyArchetype>();

            for (int i = 0; i < archetypePool.Count; i++)
            {
                if (archetypePool[i] != null)
                    availableArchetypes.Add(archetypePool[i]);
            }

            if (availableArchetypes.Count > 0)
                return availableArchetypes[Random.Range(0, availableArchetypes.Count)];

            Debug.LogError($"{nameof(EnemyConfigDatabase)} selected archetype pool contains no valid entries.", this);
            return null;
        }
    }
}
