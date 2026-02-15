using SkillTree;

namespace Battle
{
    public class EnemySpawnData
    {
        public EnemyArchetype Archetype { get; }
        public EnemyRarity Rarity { get; }
        public float Power { get; }
        public BaseInnateModifiers Modifiers { get; }

        public EnemySpawnData(
            EnemyArchetype archetype,
            EnemyRarity rarity,
            float power,
            BaseInnateModifiers modifiers)
        {
            Archetype = archetype;
            Rarity = rarity;
            Power = power;
            Modifiers = modifiers;
        }
    }
}