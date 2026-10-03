using UnityEngine;

namespace Battle
{
    public sealed class EnemyAffixGenerationRules
    {
        private readonly EnemyRarityAffixGenerationBonus[] _bonuses =
        {
            new EnemyRarityAffixGenerationBonus(),
            new EnemyRarityAffixGenerationBonus(),
            new EnemyRarityAffixGenerationBonus(),
            new EnemyRarityAffixGenerationBonus(),
            new EnemyRarityAffixGenerationBonus()
        };

        private float _experienceRewardBonus;

        public float ExperienceRewardMultiplier => Mathf.Max(0f, 1f + _experienceRewardBonus);

        public void AddMinAffixBonus(EnemyRarity rarity, int value)
        {
            _bonuses[(int)rarity].MinAffixBonus += value;
        }

        public void AddMaxAffixBonus(EnemyRarity rarity, int value)
        {
            _bonuses[(int)rarity].MaxAffixBonus += value;
        }

        public void AddExtraAffixChanceBonus(EnemyRarity rarity, float value)
        {
            _bonuses[(int)rarity].ExtraAffixChanceBonus += value;
        }

        public void AddExtraAffixChanceMultiplier(EnemyRarity rarity, float value)
        {
            _bonuses[(int)rarity].ExtraAffixChanceMultiplierBonus += value;
        }

        public void AddExperienceRewardBonus(float value)
        {
            _experienceRewardBonus += value;
        }

        public EnemyAffixRollModifier GetModifier(EnemyRarity rarity, int? maxAffixCap = null)
        {
            EnemyRarityAffixGenerationBonus bonus = _bonuses[(int)rarity];
            return new EnemyAffixRollModifier(
                bonus.MinAffixBonus,
                bonus.MaxAffixBonus,
                bonus.ExtraAffixChanceBonus,
                Mathf.Max(0f, 1f + bonus.ExtraAffixChanceMultiplierBonus),
                maxAffixCap);
        }

        private sealed class EnemyRarityAffixGenerationBonus
        {
            public int MinAffixBonus;
            public int MaxAffixBonus;
            public float ExtraAffixChanceBonus;
            public float ExtraAffixChanceMultiplierBonus;
        }
    }
}
