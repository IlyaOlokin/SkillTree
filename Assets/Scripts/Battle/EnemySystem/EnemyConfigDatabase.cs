using System.Collections.Generic;
using DropSystem;
using SkillTree;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Config Database")]
    public class EnemyConfigDatabase : ScriptableObject
    {
        [Header("Enemy generation")]
        [SerializeField] private List<EnemyGenerationPool> modulePools = new();

        [Header("Gold drops")]
        [SerializeField] private GoldDropConfig goldDropConfig;

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
        [SerializeField] private EnemyAffixRollSettings affixRollSettings = new();

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
        public EnemyAffixRollSettings AffixRollSettings => affixRollSettings;
        public EnemyBossBalanceConfig BossBalance => bossBalance;
        public EnemyWavePowerBalanceConfig WavePowerBalance => wavePowerBalance;
        public EnemyStatBudgetConfig StatBudgetConfig => statBudgetConfig;
        public IReadOnlyList<ModifierContainer> GlobalModifiers => globalModifiers;
        public GoldDropConfig GoldDropConfig => goldDropConfig;
        public IReadOnlyList<EnemyGenerationPool> ModulePools => modulePools;

        public IReadOnlyList<EnemyAffix> GetLocationAffixPool()
        {
            var locationAffixes = new List<EnemyAffix>();
            if (modulePools == null)
                return locationAffixes;

            for (int i = 0; i < modulePools.Count; i++)
            {
                var pool = modulePools[i];
                if (pool?.AffixPool == null)
                    continue;

                for (int j = 0; j < pool.AffixPool.Count; j++)
                {
                    var affix = pool.AffixPool[j];
                    if (affix != null && !ContainsAffix(locationAffixes, affix))
                        locationAffixes.Add(affix);
                }
            }

            return locationAffixes;
        }

        private void OnValidate()
        {
            startingLevel = Mathf.Max(1, startingLevel);
            maxLevel = Mathf.Max(startingLevel, maxLevel);
            wavesToUnlockNextLevel = Mathf.Max(1, wavesToUnlockNextLevel);
            respawnDelay = Mathf.Max(0f, respawnDelay);
            maxEnemiesPerWave = Mathf.Clamp(maxEnemiesPerWave, 1, 3);
            affixRollSettings?.Validate();
        }

        public float GetPowerForLevel(int level)
        {
            int clampedLevel = Mathf.Clamp(level, StartingLevel, MaxWaveLevel);
            return levelPowerConfig.GetPowerForLevel(clampedLevel, this);

        }

        public int GetWavesToUnlockNextLevel(int level)
        {
            int clampedLevel = Mathf.Clamp(level, StartingLevel, MaxWaveLevel);
            if (bossBalance != null &&
                bossBalance.TryGetWavesInLevelOverride(clampedLevel, out int wavesInLevel))
            {
                return wavesInLevel;
            }

            return Mathf.Max(1, wavesToUnlockNextLevel);
        }

        public int GetLevelPowerCount()
        {
            return MaxWaveLevel - StartingLevel + 1;
        }

        public bool TryGenerateEnemyDefinition(
            WaveContext context,
            EnemyRarity rarity,
            int enemyIndex,
            float enemyWeight,
            out GeneratedEnemyDefinition definition)
        {
            definition = null;

            if (rarity == EnemyRarity.Boss &&
                bossBalance != null &&
                bossBalance.TryGetRule(context, out var bossRule) &&
                bossRule.TryGenerateBossDefinition(enemyIndex, enemyWeight, out definition))
            {
                return true;
            }

            return TryGenerateFromModulePools(context, rarity, enemyWeight, out definition);
        }

        private bool TryGenerateFromModulePools(
            WaveContext context,
            EnemyRarity rarity,
            float enemyWeight,
            out GeneratedEnemyDefinition definition)
        {
            definition = null;

            if (modulePools == null || modulePools.Count == 0)
            {
                Debug.LogError($"{nameof(EnemyConfigDatabase)} has no enemy generation pools assigned.", this);
                return false;
            }

            var candidates = new List<EnemyGenerationPool>();
            float totalWeight = 0f;
            for (int i = 0; i < modulePools.Count; i++)
            {
                var pool = modulePools[i];
                if (pool == null || !pool.CanGenerate(context, rarity, enemyWeight))
                    continue;

                candidates.Add(pool);
                totalWeight += pool.SelectionWeight;
            }

            while (candidates.Count > 0)
            {
                int index = PickWeightedPoolIndex(candidates, totalWeight);
                var candidate = candidates[index];
                if (candidate.TryGenerate(context, rarity, enemyWeight, out definition))
                    return true;

                totalWeight -= candidate.SelectionWeight;
                candidates.RemoveAt(index);
            }

            Debug.LogWarning(
                $"{nameof(EnemyConfigDatabase)} could not generate enemy for level {context.Level}, wave {context.WaveIndex}, rarity {rarity}, weight {enemyWeight:0.##}.",
                this);
            return false;
        }

        private static int PickWeightedPoolIndex(IReadOnlyList<EnemyGenerationPool> pools, float totalWeight)
        {
            if (pools == null || pools.Count == 0)
                return -1;

            if (totalWeight <= 0f)
                return Random.Range(0, pools.Count);

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;
            for (int i = 0; i < pools.Count; i++)
            {
                cumulative += pools[i].SelectionWeight;
                if (roll <= cumulative)
                    return i;
            }

            return pools.Count - 1;
        }

        private static bool ContainsAffix(IReadOnlyList<EnemyAffix> affixes, EnemyAffix affix)
        {
            if (affixes == null || affix == null)
                return false;

            for (int i = 0; i < affixes.Count; i++)
            {
                if (affixes[i] == affix)
                    return true;
            }

            return false;
        }
    }
}
