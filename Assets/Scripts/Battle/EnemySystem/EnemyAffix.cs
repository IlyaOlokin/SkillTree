using System.Collections.Generic;
using UnityEngine;
using SkillTree;

[CreateAssetMenu(menuName = "Enemies/Affix")]
public class EnemyAffix : ScriptableObject
{
    public string affixName;
    public List<ModifierContainer> modifiers = new();
}