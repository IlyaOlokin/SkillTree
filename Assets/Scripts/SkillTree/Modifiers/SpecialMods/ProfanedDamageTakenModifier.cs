using System.Collections.Generic;
using Battle;
using LocalizationSupport;
using UnityEngine;

namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/Special/Profaned Damage Taken", fileName = "New ProfanedDamageTakenModifier")]
    public class ProfanedDamageTakenModifier : Modifier
    {
        private const float DamageTakenMultiplier = 1.5f;

        public override bool IsInPriority(ModifierPriority priority)
        {
            return priority == ModifierPriority.IncomingPreMitigation;
        }

        public override bool IsApplicable(Unit unit)
        {
            return unit != null
                   && unit.health != null
                   && unit.BaseUnitModifiers.GetStatValue(StatType.ProfanedHealthPercent) > 0f
                   && unit.health.HasProfanedHealth;
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
                "modifier.profanedDamageTaken.description",
                "While you have {profanedHealthPercent|Profaned Health}, take 50% more Damage");
        }
    }
}
