using System;
using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [Serializable]
    public class EnemyGenerationPool
    {
        [SerializeField] private string poolId;
        [SerializeField, Min(0f)] private float selectionWeight = 1f;
        [SerializeField, Range(0.2f, 1f)] private float minEnemyWeight = 0.2f;
        [SerializeField, Range(0.2f, 1f)] private float maxEnemyWeight = 1f;
        [SerializeField] private List<EnemyCoreProfile> coreProfiles = new();
        [SerializeField] private List<EnemyAttackSpeedModule> attackSpeedModules = new();
        [SerializeField] private List<EnemyAttackModule> attackModules = new();
        [SerializeField] private List<EnemyDefenceModule> defenceModules = new();
        [SerializeField] private List<EnemyUtilityModule> utilityModules = new();
        [SerializeField] private List<EnemyAffix> affixPool = new();

        public string PoolId => poolId;
        public float SelectionWeight => Mathf.Max(0f, selectionWeight);
        public IReadOnlyList<EnemyAffix> AffixPool => affixPool;

        public bool CanGenerate(WaveContext context, EnemyRarity rarity, float enemyWeight)
        {
            if (coreProfiles == null || coreProfiles.Count == 0)
                return false;

            if (attackSpeedModules == null || attackSpeedModules.Count == 0)
                return false;

            if (attackModules == null || attackModules.Count == 0)
                return false;

            if (defenceModules == null || defenceModules.Count == 0)
                return false;

            if (utilityModules == null || utilityModules.Count == 0)
                return false;

            float minWeight = Mathf.Min(minEnemyWeight, maxEnemyWeight);
            float maxWeight = Mathf.Max(minEnemyWeight, maxEnemyWeight);
            return enemyWeight >= minWeight - 0.0001f && enemyWeight <= maxWeight + 0.0001f;
        }

        public bool TryGenerate(WaveContext context, EnemyRarity rarity, float enemyWeight, out GeneratedEnemyDefinition definition)
        {
            definition = null;

            if (!CanGenerate(context, rarity, enemyWeight))
                return false;

            var coreProfile = Pick(coreProfiles);
            var attackSpeedModule = Pick(attackSpeedModules);
            var attackModule = Pick(attackModules);
            var defenceModule = Pick(defenceModules);
            var utilityModule = Pick(utilityModules);

            if (coreProfile == null || attackSpeedModule == null || attackModule == null || defenceModule == null || utilityModule == null)
                return false;

            definition = new GeneratedEnemyDefinition(
                enemyWeight,
                coreProfile,
                attackSpeedModule,
                attackModule,
                defenceModule,
                utilityModule,
                affixPool,
                WeaponType.Sword);
            return true;
        }

        private static T Pick<T>(IReadOnlyList<T> values) where T : UnityEngine.Object
        {
            if (values == null || values.Count == 0)
                return null;

            for (int attempt = 0; attempt < values.Count; attempt++)
            {
                T value = values[UnityEngine.Random.Range(0, values.Count)];
                if (value != null)
                    return value;
            }

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] != null)
                    return values[i];
            }

            return null;
        }
    }
}
