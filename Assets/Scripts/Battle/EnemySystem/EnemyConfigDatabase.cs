using System.Collections.Generic;
using UnityEngine;

namespace Battle
{
    [CreateAssetMenu(menuName = "Enemies/Config Database")]
    public class EnemyConfigDatabase : ScriptableObject
    {
        public List<EnemyArchetype> archetypes = new();

        public EnemyArchetype GetRandomArchetype()
        {
            return archetypes[Random.Range(0, archetypes.Count)];
        }
    }
}
