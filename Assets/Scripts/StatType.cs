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
    AilmentPower  = 11,
    
    IgnitePower = 12,
    ChillPower = 13,
    OverchargePower = 14,
    BleedPower = 15,
    
    AilmentChance = 68,
    
    IgniteChance = 38,
    ChillChance = 39,
    OverchargeChance = 40,
    BleedChance = 41,
    
    AilmentGuard = 51,
    IgniteMitigation = 47,
    ChillDurationReduction = 48,
    OverchargeAvoidanceChance = 49,
    BleedMitigation = 50,

    // Fixed-effect debuffs (separate from ailments, not affected by AilmentPower/AilmentGuard/AilmentChance)
    SunderChance = 53,
    SunderPower = 54,
    SunderMitigation = 55,
    DistractChance = 56,
    DistractPower = 57,
    DistractMitigation = 58,
    ExposeChance = 61,
    ExposePower = 62,
    ExposeMitigation = 63,
    
    // Crit
    CritChance = 16,
    CritDamageBonus = 17,
    
    // AttackSpeed
    AttackSpeed = 18,
    
    // Defence
    Armor = 19,
    Evasion = 20,
    BlockChance = 42,
    Defence = 26,
    Accuracy = 21,
    MaximumHealth = 22,
    ProfanedHealthPercent = 59,
    HealingReceived = 60,
    HealthRegenerationPerSecond = 31,
    BarrierCount = 32,
    BarrierCapacity = 33,
    BarrierRegenerationSpeed = 34,
    BarrierDamageTypeMask = 35,
    LifeSteal = 36,
    LifeStealTypeMask = 37,
    
    ElementalResistance = 27,
    FireResistance = 28,
    ColdResistance = 29,
    LightningResistance = 30,
    
    MaxElementalResistance = 43,
    MaxFireResistance = 44,
    MaxColdResistance = 45,
    MaxLightningResistance = 46,

    ElementalResistancePenetration = 64,
    FireResistancePenetration = 65,
    ColdResistancePenetration = 66,
    LightningResistancePenetration = 67,
    
    MysticCleansePerSecond = 52,
    
    // Attributes
    Strength = 23,
    Dexterity = 24,
    Intelligence = 25,
    AllAttributes = 69,
}
