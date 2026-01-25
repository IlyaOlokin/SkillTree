using UnityEngine;

public enum StatModifierMoreType 
{
    Empty = 9999,
    // Damage
    MoreDamage = 0,
    MoreElementalDamage = 1,
    MoreMysticDamage = 2,
    
    MorePhysicalDamage = 3,
    MoreFireDamage = 4,
    MoreColdDamage = 5,
    MoreLightningDamage = 6,
    MoreLightDamage = 7,
    MoreDarknessDamage  = 8,
    MorePoisonDamage = 9,
    
    // Negative Effects
    MoreNegativeEffectMagnitude  = 10,
    
    MoreIgniteMagnitude = 11,
    MoreChillMagnitude = 12,
    MoreOverchargeMagnitude = 13,
    MoreBleedMagnitude = 14,
    
    // Crit
    MoreCritChance = 15,
    MoreCritDamageBonus = 16,
    
    // AttackSpeed
    MoreAttackSpeed = 17,
    
    // Defence
    MoreArmor = 18,
    MoreEvasion = 19,
}
