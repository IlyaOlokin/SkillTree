using System;
using Battle;
using UnityEngine;

namespace DropSystem
{
    [Serializable]
    public sealed class GoldDropContext
    {
        [SerializeField] private string sourceId;
        [SerializeField] private int sourceLevel;
        [SerializeField] private bool isBoss;
        [SerializeField] private EnemyRarity rarity;
        [SerializeField] private float power;
        [SerializeField] private float chanceMultiplier = 1f;
        [SerializeField] private float amountMultiplier = 1f;

        public string SourceId => sourceId;
        public int SourceLevel => sourceLevel;
        public bool IsBoss => isBoss;
        public EnemyRarity Rarity => rarity;
        public float Power => power;
        public float ChanceMultiplier => Mathf.Max(0f, chanceMultiplier);
        public float AmountMultiplier => Mathf.Max(0f, amountMultiplier);

        public GoldDropContext(
            string sourceId,
            int sourceLevel,
            bool isBoss,
            EnemyRarity rarity,
            float power,
            float chanceMultiplier = 1f,
            float amountMultiplier = 1f)
        {
            this.sourceId = sourceId ?? string.Empty;
            this.sourceLevel = Mathf.Max(1, sourceLevel);
            this.isBoss = isBoss;
            this.rarity = rarity;
            this.power = Mathf.Max(0f, power);
            this.chanceMultiplier = Mathf.Max(0f, chanceMultiplier);
            this.amountMultiplier = Mathf.Max(0f, amountMultiplier);
        }

        public static GoldDropContext FromSpawnData(EnemySpawnData spawnData, int sourceLevel)
        {
            if (spawnData == null)
                return null;

            return new GoldDropContext(
                spawnData.Definition?.CoreProfile != null ? spawnData.Definition.CoreProfile.name : string.Empty,
                sourceLevel,
                spawnData.Rarity == EnemyRarity.Boss,
                spawnData.Rarity,
                spawnData.Power);
        }
    }
}
