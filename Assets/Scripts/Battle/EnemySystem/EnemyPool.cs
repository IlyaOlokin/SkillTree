using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Battle
{
    public class EnemyPool : MonoBehaviour
    {
        [SerializeField] private Unit enemyPrefab;
        [SerializeField] private int poolSize = 3;
        [SerializeField] public AttackResolver attackResolver;
        [SerializeField] private List<Transform> spawnPositions;
        [Inject] private DiContainer _container;

        private List<Unit> _units = new();
        public List<Unit> Units => _units;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var unit = _container.InstantiatePrefabForComponent<Unit>(enemyPrefab, transform);
                unit.gameObject.SetActive(false);
                unit.gameObject.transform.position = spawnPositions[i].position;
                _units.Add(unit);
                
            }
            attackResolver.SetNewEnemies(_units);
        }
    }
}
