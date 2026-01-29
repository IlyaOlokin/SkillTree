using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "SkillTree/Tool Config")]
public class SkillTreeToolConfig : ScriptableObject
{
    public List<GameObject> nodePrefabs = new();
    public int currentPrefabIndex;
}
