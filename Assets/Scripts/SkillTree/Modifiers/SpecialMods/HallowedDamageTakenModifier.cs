using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Hallowed Damage Taken", fileName = "New HallowedDamageTakenModifier")]
    public class HallowedDamageTakenModifier : Modifier
    {
        private const float DamageTakenMultiplier = 0.5f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.IncomingPreMitigation;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit != null
                   && unit.health != null
                   && unit.BaseUnitModifiers.GetStatValue(StatType.HallowedHealthPercent) > 0f
                   && unit.health.IsHealthInsideHallowedThreshold;
        }

        public override void ApplyEffect(DamageInfo damageInfo)
        {
            if (damageInfo?.DamageInstance == null)
            {
                return;
            }

            var damageTypes = new List<DamageType>(damageInfo.DamageInstance.Damage.Keys);
            foreach (var damageType in damageTypes)
            {
                damageInfo.DamageInstance.Damage[damageType] *= DamageTakenMultiplier;
            }
        }

        public override string GetDescription()
        {
            return GameLocalization.GetModifier(
                "modifier.hallowedDamageTaken.description",
                "While you have {hallowedHealth|Hallowed Health}, take 50% less Damage");
        }
    }
}
