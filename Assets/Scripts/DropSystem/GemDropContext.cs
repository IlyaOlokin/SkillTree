using System;
using Battle;
using UnityEngine;

namespace DropSystem
{
    [Serializable]
    public class GemDropContext
    {
        [SerializeField] private string sourceId;
        [SerializeField] private int sourceLevel;
        [SerializeField] private bool isBoss;
        [SerializeField] private EnemyRarity rarity;
        [SerializeField] private float power;

        public string SourceId => sourceId;
        public int SourceLevel => sourceLevel;
        public bool IsBoss => isBoss;
        public EnemyRarity Rarity => rarity;
        public float Power => power;

        public GemDropContext(string sourceId, int sourceLevel, bool isBoss, EnemyRarity rarity, float power)
        {
            this.sourceId = sourceId;
            this.sourceLevel = sourceLevel;
            this.isBoss = isBoss;
            this.rarity = rarity;
            this.power = power;
        }

        public static GemDropContext FromSpawnData(EnemySpawnData spawnData)
        {
            if (spawnData == null)
                return null;

            string sourceId = spawnData.Archetype != null ? spawnData.Archetype.name : string.Empty;
            int sourceLevel = spawnData.Archetype != null ? spawnData.Archetype.minLevel : 0;
            bool sourceIsBoss = spawnData.Rarity == EnemyRarity.Boss || (spawnData.Archetype != null && spawnData.Archetype.bossOnly);

            return new GemDropContext(
                sourceId,
                sourceLevel,
                sourceIsBoss,
                spawnData.Rarity,
                spawnData.Power);
        }
    }
}
