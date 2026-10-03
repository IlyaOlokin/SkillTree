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
        [SerializeField] private List<Transform> spawnPositionsFor1Enemy = new();
        [SerializeField] private List<Transform> spawnPositionsFor2Enemies = new();
        [FormerlySerializedAs("spawnPositions")]
        [SerializeField] private List<Transform> spawnPositionsFor3Enemies;
        [Inject] private DiContainer _container;

        private List<Unit> _units = new();
        public List<Unit> Units => _units;

        private void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                var unit = _container.InstantiatePrefabForComponent<Unit>(enemyPrefab, transform);
                unit.gameObject.SetActive(false);
                _units.Add(unit);
            }
            attackResolver.SetNewEnemies(_units);
        }

        public void PositionEnemies(int enemyCount)
        {
            List<Transform> positions = enemyCount switch
            {
                1 => spawnPositionsFor1Enemy,
                2 => spawnPositionsFor2Enemies,
                _ => spawnPositionsFor3Enemies
            };

            for (int i = 0; i < enemyCount && i < _units.Count; i++)
            {
                Transform position = positions != null && i < positions.Count ? positions[i] : null;
                // Preserve existing scene layouts until the new lists are assigned.
                if (position == null && spawnPositionsFor3Enemies != null && i < spawnPositionsFor3Enemies.Count)
                    position = spawnPositionsFor3Enemies[i];

                if (position != null)
                    _units[i].transform.position = position.position;
            }
        }
    }
}
