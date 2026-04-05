namespace Battle
{
    public readonly struct WaveContext
    {
        public WaveContext(
            int level,
            int waveIndex,
            int wavesInLevel,
            bool isBossWave = false,
            int bossEnemyCount = 0,
            int forcedEnemyCount = 0,
            int bossAffixLimit = 0)
        {
            Level = level;
            WaveIndex = waveIndex;
            WavesInLevel = wavesInLevel;
            IsBossWave = isBossWave;
            BossEnemyCount = bossEnemyCount;
            ForcedEnemyCount = forcedEnemyCount;
            BossAffixLimit = bossAffixLimit;
        }

        public int Level { get; }
        public int WaveIndex { get; }
        public int WavesInLevel { get; }
        public bool IsLastWave => WaveIndex >= WavesInLevel;
        public bool IsBossWave { get; }
        public int BossEnemyCount { get; }
        public int ForcedEnemyCount { get; }
        public int BossAffixLimit { get; }
    }
}
