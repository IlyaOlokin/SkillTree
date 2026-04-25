using Battle;
using UnityEngine;

namespace Items
{
    [CreateAssetMenu(menuName = "Items/Consumables/Grant Skill Points", fileName = "GrantSkillPointsConsumable")]
    public sealed class GrantSkillPointsConsumableDefinition : ConsumableItemDefinition
    {
        [SerializeField] [Min(1)] private int skillPointsAmount = 1;

        protected override bool TryConsume(ItemUseContext context)
        {
            PlayerUnit player = context?.Player;
            player.UnitLevel.RefundSkillPoints(skillPointsAmount);
            return true;
        }
    }
}
