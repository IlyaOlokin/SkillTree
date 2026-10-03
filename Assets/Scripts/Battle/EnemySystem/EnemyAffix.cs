using System.Collections.Generic;
using UnityEngine;
using SkillTree;

[System.Serializable]
public class EnemyAffixStatWeight
{
    public StatType statType;
    [Min(0f)] public float weight;
}

[CreateAssetMenu(menuName = "Enemies/Affix")]
public class EnemyAffix : ScriptableObject
{
    public string affixName;
    [Min(0f)] public float moreExperience;
    public List<EnemyAffixStatWeight> addedStatWeights = new();
    public List<ModifierContainer> modifiers = new();
}
