using UnityEngine;


public enum StatType
{
    Empty = 0,
    // Damage
    Damage = 1,
    ElementalDamage = 2,
    MysticDamage = 3,
    
    PhysicalDamage = 4,
    FireDamage = 5,
    ColdDamage = 6,
    LightningDamage = 7,
    LightDamage = 8,
    DarknessDamage  = 9,
    PoisonDamage = 10,
    
    // Negative Effects
    NegativeEffectMagnitude  = 11,
    
    IgniteMagnitude = 12,
    ChillMagnitude = 13,
    OverchargeMagnitude = 14,
    BleedMagnitude = 15,
    
    IgniteChance = 38,
    ChillChance = 39,
    OverchargeChance = 40,
    BleedChance = 41,
    
    // Crit
    CritChance = 16,
    CritDamageBonus = 17,
    
    // AttackSpeed
    AttackSpeed = 18,
    
    // Defence
    Armor = 19,
    Evasion = 20,
    Defence = 26,
    Accuracy = 21,
    MaximumHealth = 22,
    HealthRegenerationPerSecond = 31,
    BarrierCount = 32,
    BarrierPower = 33,
    BarrierRegenerationSpeed = 34,
    BarrierDamageTypeMask = 35,
    LifeSteel = 36,
    LifeSteelTypeMask = 37,
    
    ElementalResistance = 27,
    FireResistance = 28,
    ColdResistance = 29,
    LightningResistance = 30,
    
    // Attributes
    Strength = 23,
    Dexterity = 24,
    Intelligence = 25,
}