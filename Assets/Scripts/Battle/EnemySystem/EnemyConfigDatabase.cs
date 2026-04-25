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

        [Header("Level power")]
        [SerializeField] private List<float> levelPowers = new() { 10f };

        [Header("Wave progression")]
        [SerializeField, Min(1)] private int startingLevel = 1;
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

        public IReadOnlyList<float> LevelPowers => levelPowers;
        public int StartingLevel => startingLevel;
        public int WavesToUnlockNextLevel => wavesToUnlockNextLevel;
        public int MaxWaveLevel => Mathf.Max(startingLevel, startingLevel + GetLevelPowerCount() - 1);
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
            wavesToUnlockNextLevel = Mathf.Max(1, wavesToUnlockNextLevel);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            maxEnemiesPerWave = Mathf.Clamp(maxEnemiesPerWave, 1, 3);

            levelPowers ??= new List<float>();
            if (levelPowers.Count == 0)
                levelPowers.Add(10f);

            for (int i = 0; i < levelPowers.Count; i++)
                levelPowers[i] = Mathf.Max(0.01f, levelPowers[i]);
        }

        public float GetPowerForLevel(int level)
        {
            int powerCount = GetLevelPowerCount();
            if (powerCount <= 0)
            {
                Debug.LogWarning($"{nameof(EnemyConfigDatabase)} has no level powers configured. Using fallback power 10.", this);
                return 10f;
            }

            int clampedLevel = Mathf.Clamp(level, StartingLevel, MaxWaveLevel);
            int index = clampedLevel - StartingLevel;
            return levelPowers[index];
        }

        public int GetLevelPowerCount()
        {
            return levelPowers?.Count ?? 0;
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
