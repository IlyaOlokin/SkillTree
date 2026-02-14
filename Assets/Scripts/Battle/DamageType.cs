using UnityEngine;

namespace Battle
{
    [System.Flags]
    public enum DamageType
    {
        Physical  = 1 << 0, // 1
        Fire      = 1 << 1, // 2
        Cold      = 1 << 2, // 4
        Lightning = 1 << 3, // 8
        Light     = 1 << 4, // 16
        Darkness  = 1 << 5, // 32
        Poison    = 1 << 6, // 64
    }
}

