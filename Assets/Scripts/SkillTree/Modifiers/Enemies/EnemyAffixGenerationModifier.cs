using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Enemies/Affix Generation", fileName = "New EnemyAffixGenerationModifier")]
    public sealed class EnemyAffixGenerationModifier : Modifier
    {
        [SerializeField] private EnemyRarity rarity = EnemyRarity.Normal;
        [SerializeField] private int addedMinAffixes;
        [SerializeField] private int addedMaxAffixes;
        [SerializeField] private float addedExtraAffixChance;
        [SerializeField] private float increasedExtraAffixChance;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EnemySpawner spawner = EnemySpawner.For(unit);
            if (spawner == null)
                return null;

            int minAffixes = Mathf.RoundToInt(powerContext.Scale(addedMinAffixes));
            int maxAffixes = Mathf.RoundToInt(powerContext.Scale(addedMaxAffixes));
            float extraChance = powerContext.Scale(addedExtraAffixChance);
            float increasedChance = powerContext.Scale(increasedExtraAffixChance);

            return new DelegateModifierRuntimeBinding(
                () => Add(spawner, minAffixes, maxAffixes, extraChance, increasedChance),
                () => Add(spawner, -minAffixes, -maxAffixes, -extraChance, -increasedChance));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            string rarityName = GameLocalization.LocalizeEnum(rarity);
            int maxAffixes = Mathf.RoundToInt(powerContext.Scale(addedMaxAffixes));
            if (maxAffixes != 0 && addedMinAffixes == 0 && Mathf.Approximately(addedExtraAffixChance, 0f) && Mathf.Approximately(increasedExtraAffixChance, 0f))
            {
                return GameLocalization.FormatModifier(
                    "modifier.enemyAffixGeneration.maxAffixes.description",
                    "[[0]] maximum affixes on [[1]] enemies",
                    powerContext.HighlightValue(maxAffixes.ToString("+0;-0;0")),
                    rarityName);
            }

            return GameLocalization.FormatModifier(
                "modifier.enemyAffixGeneration.description",
                "Modifies affix generation for [[0]] enemies",
                rarityName);
        }

        private void Add(
            EnemySpawner spawner,
            int minAffixes,
            int maxAffixes,
            float extraChance,
            float increasedChance)
        {
            spawner.AffixRules.AddMinAffixBonus(rarity, minAffixes);
            spawner.AffixRules.AddMaxAffixBonus(rarity, maxAffixes);
            spawner.AffixRules.AddExtraAffixChanceBonus(rarity, extraChance);
            spawner.AffixRules.AddExtraAffixChanceMultiplier(rarity, increasedChance);
        }
    }
}
