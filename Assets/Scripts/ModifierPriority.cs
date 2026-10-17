using UnityEngine;

public enum ModifierPriority
{
    PreAttribute = 0,
    Secondary = 1,
    Special = 2,
    OnAttack = 3,
    OnGettingHit = 4,
    PreAttribute2 = 5,
    IncomingPreMitigation = 6,
    AfterCriticalHit = 7,
    // After AllAttributes expansion, before attribute-derived modifiers.
    AttributeFinalization = 8,
}
