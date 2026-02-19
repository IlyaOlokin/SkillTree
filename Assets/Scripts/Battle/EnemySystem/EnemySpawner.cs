using System;
using UnityEngine;

namespace Battle
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyPool pool;
        [SerializeField] private EnemyConfigDatabase database;

        private WaveFactory _waveFactory;

        [SerializeField] private int level;

        private void Awake()
        {
            var enemyFactory = new EnemyFactory(database);
            _waveFactory = new WaveFactory(enemyFactory);
        }

        private void Start()
        {
            // attackResolver.SetNewEnemies ???
            Spawn(level);
        }

        public void Spawn(int level)
        {
            foreach (var unit in pool.Units)
                unit.gameObject.SetActive(false);

            var packages = _waveFactory.CreateWave(level);

            for (int i = 0; i < packages.Count; i++)
            {
                var unit = pool.Units[i];
                
                //packages[i].ApplyEffect(unit);
                ((EnemyUnit)unit).Initialize(packages[i]);
                // Recalculatye Unit stats

                unit.gameObject.SetActive(true);
            }
        }
    }
}