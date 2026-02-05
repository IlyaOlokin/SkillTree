using UnityEngine;


public enum StatType
{
    Empty = 9999,
    // Damage
    Damage = 0,
    ElementalDamage = 1,
    MysticDamage = 2,
    
    PhysicalDamage = 3,
    FireDamage = 4,
    ColdDamage = 5,
    LightningDamage = 6,
    LightDamage = 7,
    DarknessDamage  = 8,
    PoisonDamage = 9,
    
    // Negative Effects
    NegativeEffectMagnitude  = 10,
    
    IgniteMagnitude = 11,
    ChillMagnitude = 12,
    OverchargeMagnitude = 13,
    BleedMagnitude = 14,
    
    // Crit
    CritChance = 15,
    CritDamageBonus = 16,
    
    // AttackSpeed
    AttackSpeed = 17,
    
    // Defence
    Armor = 18,
    Evasion = 19,
    Accuracy = 20,
    MaximumHealth = 21,
    
    // Attributes
    Strength = 22,
    Dexterity = 23,
    Intelligence = 24,
}