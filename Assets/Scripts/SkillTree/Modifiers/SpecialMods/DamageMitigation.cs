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
    }
}

