using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

namespace Battle
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyPool pool;
        [SerializeField] private EnemyConfigDatabase database;
        
        [Header("Wave progression")]
        [FormerlySerializedAs("level")]
        [SerializeField, Min(1)] private int startingLevel = 1;
        [SerializeField, Min(1)] private int wavesToUnlockNextLevel = 10;
        [SerializeField, Min(1)] private int maxWaveLevel = 100;
        [SerializeField, Min(0f)] private float respawnDelay = 2f;

        private WaveFactory _waveFactory;
        private readonly List<EnemyUnit> _activeEnemies = new();
        private readonly Dictionary<int, int> _clearedWavesPerLevel = new();
        private Coroutine _respawnCoroutine;

        private int _selectedLevel;
        private int _maxUnlockedLevel;

        public int SelectedLevel => _selectedLevel;
        public int MaxUnlockedLevel => _maxUnlockedLevel;

        private void Awake()
        {
            var enemyFactory = new EnemyFactory(database);
            _waveFactory = new WaveFactory(enemyFactory);

            _maxUnlockedLevel = Mathf.Clamp(startingLevel, 1, maxWaveLevel);
            _selectedLevel = _maxUnlockedLevel;
        }

        private void Start()
        {
            SpawnCurrentLevel();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                SelectPreviousLevel();
            }

            if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                SelectNextLevel();
            }
        }

        public void Spawn(int level)
        {
            SetSelectedLevel(level);
            SpawnCurrentLevel();
        }

        public void SelectPreviousLevel()
        {
            SetSelectedLevel(_selectedLevel - 1);
        }

        public void SelectNextLevel()
        {
            SetSelectedLevel(_selectedLevel + 1);
        }

        private void SetSelectedLevel(int level)
        {
            _selectedLevel = Mathf.Clamp(level, 1, _maxUnlockedLevel);
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
        }

        private bool _autoProgressionEnabled = true;

        private void SpawnCurrentLevel()
        {
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }

            UnsubscribeFromActiveEnemies();
            DeactivatePool();

            var packages = _waveFactory.CreateWave(_selectedLevel);
            int spawnCount = Mathf.Min(packages.Count, pool.Units.Count);
            var enemiesForResolver = new List<Unit>(spawnCount);

            for (int i = 0; i < spawnCount; i++)
            {
                if (pool.Units[i] is not EnemyUnit enemy)
                    continue;

                enemy.Initialize(packages[i]);
                enemy.OnDeath += HandleEnemyDeath;
                enemy.gameObject.SetActive(true);
                _activeEnemies.Add(enemy);
                enemiesForResolver.Add(enemy);
            }

            pool.attackResolver?.SetNewEnemies(enemiesForResolver);
        }

        private void HandleEnemyDeath(Unit unit)
        {
            if (unit is not EnemyUnit enemy)
                return;

            if (_activeEnemies.Remove(enemy) == false)
                return;

            enemy.OnDeath -= HandleEnemyDeath;

            if (_activeEnemies.Count > 0)
                return;

            RegisterWaveClear(_selectedLevel);
            ScheduleRespawn(respawnDelay);
        }

        private void RegisterWaveClear(int level)
        {
            if (_clearedWavesPerLevel.TryGetValue(level, out int clearedWaves) == false)
                clearedWaves = 0;

            clearedWaves += 1;

            while (clearedWaves >= wavesToUnlockNextLevel)
            {
                clearedWaves -= wavesToUnlockNextLevel;

                int nextLevel = Mathf.Min(level + 1, maxWaveLevel);
                if (nextLevel > _maxUnlockedLevel)
                {
                    bool shouldAutoAdvance = _autoProgressionEnabled && _selectedLevel >= _maxUnlockedLevel;
                    _maxUnlockedLevel = nextLevel;

                    if (shouldAutoAdvance)
                    {
                        _selectedLevel = _maxUnlockedLevel;
                    }
                }
            }

            _clearedWavesPerLevel[level] = clearedWaves;
        }

        private void ScheduleRespawn(float delay)
        {
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
            }

            _respawnCoroutine = StartCoroutine(RespawnRoutine(delay));
        }

        private IEnumerator RespawnRoutine(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            _respawnCoroutine = null;
            SpawnCurrentLevel();
        }

        private void DeactivatePool()
        {
            foreach (var unit in pool.Units)
            {
                unit.gameObject.SetActive(false);
            }
        }

        private void UnsubscribeFromActiveEnemies()
        {
            for (int i = 0; i < _activeEnemies.Count; i++)
            {
                if (_activeEnemies[i] != null)
                    _activeEnemies[i].OnDeath -= HandleEnemyDeath;
            }

            _activeEnemies.Clear();
        }

        private void OnDestroy()
        {
            UnsubscribeFromActiveEnemies();
        }
    }
}
