using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Enemies/Experience Reward", fileName = "New EnemyExperienceRewardModifier")]
    public sealed class EnemyExperienceRewardModifier : Modifier
    {
        [SerializeField] private float increasedExperienceReward = 0.1f;

        public override IModifierRuntimeBinding CreateRuntimeBinding(Unit unit, ModifierPowerContext powerContext)
        {
            EnemySpawner spawner = EnemySpawner.For(unit);
            if (spawner == null)
                return null;

            float scaledReward = powerContext.Scale(increasedExperienceReward);
            return new DelegateModifierRuntimeBinding(
                () => spawner.AffixRules.AddExperienceRewardBonus(scaledReward),
                () => spawner.AffixRules.AddExperienceRewardBonus(-scaledReward));
        }

        public override string GetDescription(ModifierPowerContext powerContext)
        {
            return GameLocalization.FormatModifier(
                "modifier.enemyExperienceReward.description",
                "[[0]]% increased experience from enemies",
                powerContext.HighlightValue(powerContext.Scale(increasedExperienceReward) * 100f));
        }
    }
}
