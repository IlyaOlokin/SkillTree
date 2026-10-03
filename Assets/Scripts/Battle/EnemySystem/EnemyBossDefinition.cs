using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Boss Definition")]
    public class EnemyBossDefinition : ScriptableObject
    {
        [SerializeField, Range(0.2f, 1f)] private float waveWeight = 1f;
        [SerializeField] private EnemyCoreProfile coreProfile;
        [SerializeField] private EnemyAttackSpeedModule attackSpeedModule;
        [SerializeField] private EnemyAttackModule attackModule;
        [SerializeField] private EnemyDefenceModule defenceModule;
        [SerializeField] private EnemyUtilityModule utilityModule;
        [SerializeField] private List<EnemyAffix> affixPool = new();

        public float WaveWeight => Mathf.Clamp(waveWeight, 0.2f, 1f);

        public bool TryCreateDefinition(out GeneratedEnemyDefinition definition)
        {
            return TryCreateDefinition(1f, out definition);
        }

        public bool TryCreateDefinition(float powerMultiplier, out GeneratedEnemyDefinition definition)
        {
            definition = null;

            if (coreProfile == null || attackSpeedModule == null || attackModule == null || defenceModule == null || utilityModule == null)
                return false;

            definition = new GeneratedEnemyDefinition(
                WaveWeight,
                coreProfile,
                attackSpeedModule,
                attackModule,
                defenceModule,
                utilityModule,
                affixPool,
                WeaponType.Sword,
                powerMultiplier);
            return true;
        }
    }
}
