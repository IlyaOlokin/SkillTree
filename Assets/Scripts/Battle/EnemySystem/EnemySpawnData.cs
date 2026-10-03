using System.Collections.Generic;
using SkillTree;

namespace Battle
{
    public class EnemySpawnData : System.IDisposable
    {
        private readonly bool _ownsModifiers;
        private bool _disposed;
        public GeneratedEnemyDefinition Definition { get; }
        public EnemyRarity Rarity { get; }
        public float Power { get; }
        public float ExperienceReward { get; }
        public BaseInnateModifiers Modifiers { get; }
        public IReadOnlyList<EnemyAffix> Affixes { get; }
        public float BaseEvasion { get; }

        public EnemySpawnData(
            GeneratedEnemyDefinition definition,
            EnemyRarity rarity,
            float power,
            float experienceReward,
            BaseInnateModifiers modifiers,
            IReadOnlyList<EnemyAffix> affixes = null,
            float baseEvasion = 0f,
            bool ownsModifiers = false)
        {
            Definition = definition;
            Rarity = rarity;
            Power = power;
            ExperienceReward = experienceReward;
            Modifiers = modifiers;
            Affixes = affixes ?? System.Array.Empty<EnemyAffix>();
            BaseEvasion = baseEvasion;
            _ownsModifiers = ownsModifiers;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            if (_ownsModifiers && Modifiers != null)
                UnityEngine.Object.Destroy(Modifiers);
        }
    }
}
