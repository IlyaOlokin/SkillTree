using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyPool pool;
        [SerializeField] private EnemyConfigDatabase database;

        private int _currentClearedWaves;
        
        private WaveFactory _waveFactory;
        private readonly List<EnemyUnit> _activeEnemies = new();
        private Coroutine _respawnCoroutine;

        private int _selectedLevel;
        private int _maxUnlockedLevel;
        private bool _autoProgressionEnabled = true;

        private int StartingLevel => database != null ? database.StartingLevel : 1;
        private int WavesToUnlockNextLevelInternal => database != null ? database.WavesToUnlockNextLevel : 10;
        private int MaxWaveLevel => database != null ? database.MaxWaveLevel : 100;
        private float RespawnDelay => database != null ? database.RespawnDelay : 2f;

        public int SelectedLevel => _selectedLevel;
        public int MaxUnlockedLevel => _maxUnlockedLevel;
        public int WavesToUnlockNextLevel => WavesToUnlockNextLevelInternal;
        public int CurrentClearedWaves => _currentClearedWaves;
        
        public event Action OnLevelChanged;
        public event Action OnWaveCleared;

        private void Awake()
        {
            if (database == null)
            {
                Debug.LogError($"{nameof(EnemySpawner)} requires {nameof(EnemyConfigDatabase)} reference.", this);
                enabled = false;
                return;
            }

            var enemyFactory = new EnemyFactory(database);
            _waveFactory = new WaveFactory(enemyFactory, database);

            _maxUnlockedLevel = Mathf.Clamp(StartingLevel, 1, MaxWaveLevel);
            _selectedLevel = _maxUnlockedLevel;
        }

        private void Start()
        {
            SpawnCurrentLevel();
        }

        public void Spawn(int level)
        {
            SetSelectedLevel(level);
            SpawnCurrentLevel();
        }

        public void SelectPreviousLevel()
        {
            SetSelectedLevel(_selectedLevel - 1);
            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        public void SelectNextLevel()
        {
            SetSelectedLevel(_selectedLevel + 1);
            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        private void SetSelectedLevel(int level)
        {
            _selectedLevel = Mathf.Clamp(level, 1, _maxUnlockedLevel);
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
            _currentClearedWaves = 0;
            OnLevelChanged?.Invoke();
        }
        
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

                if (packages[i] == null)
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

            RegisterWaveClear();
            ScheduleRespawn(RespawnDelay);
        }

        private void RegisterWaveClear()
        {
            _currentClearedWaves++;

            if (_currentClearedWaves >= WavesToUnlockNextLevelInternal && _autoProgressionEnabled)
            {
                _maxUnlockedLevel = Mathf.Min(_maxUnlockedLevel + 1, MaxWaveLevel);
                SelectNextLevel();
            }
            OnWaveCleared?.Invoke();
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
