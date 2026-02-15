using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Battle
{
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private Unit enemyPrefab;
        [SerializeField] private int poolSize = 3;
        [SerializeField] private AttackResolver attackResolver;
        [SerializeField] private List<Transform> spawnPositions;

        private List<Unit> _units = new();
        public List<Unit> Units => _units;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var unit = Instantiate(enemyPrefab, transform);
                unit.gameObject.SetActive(false);
                unit.gameObject.transform.position = spawnPositions[i].position;
                _units.Add(unit);
                
            }
            attackResolver.SetNewEnemies(_units);
        }
    }
}