using UnityEngine;

/// <summary>Wisp counts are passive counters consumed by other modifiers.</summary>
public static class WispStats
{
    public static bool IsWisp(StatType statType)
    {
        switch (statType)
        {
            case StatType.SteelWisp:
            case StatType.AshWisp:
            case StatType.FrostWisp:
            case StatType.StormWisp:
                return true;
            default:
                return false;
        }
    }

    public static float Normalize(StatType statType, float value)
    {
        // Round only the final total, so fractional contributions can accumulate.
        return IsWisp(statType) ? Mathf.Floor(Mathf.Max(0f, value)) : value;
    }
}
