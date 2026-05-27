using System;
using Battle;
using LocalizationSupport;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;


namespace SkillTree
{
    [CreateAssetMenu(menuName = "Modifiers/FlatDamageMitigation", fileName = "New Flat Damage Mitigation")]
    public class DamageMitigation : Modifier
    {
        [SerializeField] private DamageType damageType;
        [SerializeField] private float mitigationValue;
        
        public override void ApplyEffect(DamageInfo damageInfo)
        {
            var damageKeys = damageInfo.DamageInstance.Damage.Keys.ToArray();
            foreach (var damageType in damageKeys)
            {
                if (this.damageType.HasFlag(damageType))
                {
                    damageInfo.DamageInstance.Damage[damageType] *= 1f - mitigationValue;
                }
            }
        }

        public override string GetDescription()
        {
            return GameLocalization.FormatModifier(
                "modifier.damageMitigation.description",
                "Take [[0]]% less [[1]] Damage",
                mitigationValue * 100f,
                FormatDamageTypeMask(damageType));
        }

        private static string FormatDamageTypeMask(DamageType damageTypeMask)
        {
            List<string> damageTypeNames = new List<string>();
            foreach (DamageType damageType in Enum.GetValues(typeof(DamageType)))
            {
                if (damageTypeMask.HasFlag(damageType))
                {
                    damageTypeNames.Add(GameLocalization.LocalizeEnum(damageType));
                }
            }

            if (damageTypeNames.Count == 0)
            {
                return GameLocalization.GetModifier("modifier.damageTypeMask.none", "no");
            }

            if (damageTypeNames.Count == 1)
            {
                return damageTypeNames[0];
            }

            return string.Join(", ", damageTypeNames);
        }
    }
}
