using System;
using System.Collections.Generic;

namespace Battle
{
    public enum EnemyIntelFeatureType
    {
        Damage = 0,
        ElementalDamage = 1,
        MysticDamage = 2,
        PhysicalDamage = 3,
        FireDamage = 4,
        ColdDamage = 5,
        LightningDamage = 6,
        LightDamage = 7,
        DarknessDamage = 8,
        PoisonDamage = 9,
        Armor = 10,
        Evasion = 11,
        Block = 12,
        Barrier = 13,
        HealthRegeneration = 14,
        ElementalResistance = 15,
        FireResistance = 16,
        ColdResistance = 17,
        LightningResistance = 18,
        MysticNegation = 19,
        MysticCleanse = 20,
        AttackSpeed = 21,
        CriticalChance = 22,
        CriticalDamage = 23,
        Ailment = 24,
        Ignite = 25,
        Chill = 26,
        Overcharge = 27,
        Bleed = 28,
        Sunder = 29,
        Distract = 30,
        Expose = 31
    }

    public enum EnemyIntelFeatureDisposition
    {
        Strength = 0,
        Weakness = 1
    }

    public readonly struct EnemyIntelFeature : IEquatable<EnemyIntelFeature>
    {
        public EnemyIntelFeature(EnemyIntelFeatureType type, EnemyIntelFeatureDisposition disposition)
        {
            Type = type;
            Disposition = disposition;
        }

        public EnemyIntelFeatureType Type { get; }
        public EnemyIntelFeatureDisposition Disposition { get; }

        public bool Equals(EnemyIntelFeature other)
        {
            return Type == other.Type && Disposition == other.Disposition;
        }

        public override bool Equals(object obj)
        {
            return obj is EnemyIntelFeature other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Type * 397) ^ (int)Disposition;
        }
    }

    public sealed class EnemyIntelData
    {
        public static readonly EnemyIntelData Empty = new EnemyIntelData(
            Array.Empty<EnemyIntelFeature>(),
            Array.Empty<string>());

        public EnemyIntelData(
            IReadOnlyList<EnemyIntelFeature> features,
            IReadOnlyList<string> affixLocalizationKeys)
        {
            Features = features ?? Array.Empty<EnemyIntelFeature>();
            AffixLocalizationKeys = affixLocalizationKeys ?? Array.Empty<string>();
        }

        public IReadOnlyList<EnemyIntelFeature> Features { get; }
        public IReadOnlyList<string> AffixLocalizationKeys { get; }
    }
}
