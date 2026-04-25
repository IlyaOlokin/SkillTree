using System;
using System.Collections;
using System.Collections.Generic;
using DropSystem;
using InventorySystem;
using SaveSystem;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class EnemySpawner : MonoBehaviour
    {
        private const string FallbackLocationId = "default";

        [SerializeField] private EnemyPool pool;
        [SerializeField] private EnemyConfigDatabase database;
        [SerializeField] private LocationCatalog locationCatalog;

        [Inject] private PlayerUnit _player;

        private int _currentClearedWaves;
        
        private WaveFactory _waveFactory;
        private readonly ItemDropResolver _itemDropResolver = new();
        private readonly List<EnemyUnit> _activeEnemies = new();
        private readonly Dictionary<string, LocationProgressState> _locationProgressById = new(StringComparer.Ordinal);
        private Coroutine _respawnCoroutine;
        private bool _hasStarted;
        private bool _battleActive = true;

        private int _selectedLevel;
        private int _maxUnlockedLevel;
        private bool _autoProgressionEnabled = true;
        private string _selectedLocationId;
        private LocationDefinition _selectedLocation;
        private LocationProgressState _selectedLocationProgress;

        private EnemyConfigDatabase ActiveDatabase => _selectedLocation != null ? _selectedLocation.EnemyDatabase : database;
        private int StartingLevel => ActiveDatabase != null ? ActiveDatabase.StartingLevel : 1;
        private int WavesToUnlockNextLevelInternal => ActiveDatabase != null ? ActiveDatabase.WavesToUnlockNextLevel : 10;
        private int MaxWaveLevel => ActiveDatabase != null ? ActiveDatabase.MaxWaveLevel : 100;
        private float RespawnDelay => ActiveDatabase != null ? ActiveDatabase.RespawnDelay : 2f;

        public int SelectedLevel => _selectedLevel;
        public int MaxUnlockedLevel => _maxUnlockedLevel;
        public int WavesToUnlockNextLevel => WavesToUnlockNextLevelInternal;
        public int CurrentClearedWaves => _currentClearedWaves;
        public int CurrentLocationStartingLevel => StartingLevel;
        public string SelectedLocationId => _selectedLocationId;
        public LocationDefinition SelectedLocation => _selectedLocation;
        public IReadOnlyList<LocationDefinition> Locations => locationCatalog != null ? locationCatalog.Locations : Array.Empty<LocationDefinition>();
        public bool IsBattleActive => _battleActive;
        
        public event Action OnLocationChanged;
        public event Action OnLevelChanged;
        public event Action OnWaveCleared;
        public event Action<EnemyUnit, IReadOnlyList<InventoryItem>> OnEnemyDropsResolved;
        public event Action<IReadOnlyList<InventoryItem>, Vector3> OnLocationRewardsResolved;
        public event Action<bool> OnBattleActivityChanged;

        private void Awake()
        {
            if (locationCatalog == null && database == null)
            {
                Debug.LogError($"{nameof(EnemySpawner)} requires either {nameof(LocationCatalog)} or {nameof(EnemyConfigDatabase)} reference.", this);
                enabled = false;
                return;
            }

            InitializeSelectedLocation();
        }

        private void Start()
        {
            _hasStarted = true;

            if (_battleActive && ActiveDatabase != null)
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

            if (!_battleActive)
                return;

            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        public void SelectNextLevel()
        {
            SetSelectedLevel(_selectedLevel + 1);

            if (!_battleActive)
                return;

            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        public void RestartCurrentLevel()
        {
            SetSelectedLevel(_selectedLevel);

            if (!_battleActive)
                return;

            SpawnCurrentLevel();
        }

        public bool SelectLocation(string locationId, bool restartBattle = true)
        {
            if (TryResolveLocation(locationId, out var location) == false)
                return false;

            ApplySelectedLocation(location, locationId, restartBattle && _battleActive);
            return true;
        }

        public void EnterBattle()
        {
            SetBattleActive(true, true);
        }

        public void ExitBattle()
        {
            SetBattleActive(false);
        }

        public bool HasClaimedReward(string rewardId)
        {
            if (_selectedLocationProgress == null || string.IsNullOrWhiteSpace(rewardId))
                return false;

            return _selectedLocationProgress.ClaimedRewardIds.Contains(rewardId);
        }

        public bool TryClaimReward(string rewardId)
        {
            if (_selectedLocationProgress == null || string.IsNullOrWhiteSpace(rewardId))
                return false;

            return _selectedLocationProgress.ClaimedRewardIds.Add(rewardId);
        }

        public bool TryGetLocationProgress(string locationId, out int selectedLevel, out int maxUnlockedLevel, out int completedLevelCount)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                selectedLevel = 1;
                maxUnlockedLevel = 1;
                completedLevelCount = 0;
                return false;
            }

            EnemyConfigDatabase targetDatabase = ResolveDatabaseForLocation(locationId);
            if (targetDatabase == null)
            {
                selectedLevel = 1;
                maxUnlockedLevel = 1;
                completedLevelCount = 0;
                return false;
            }

            int startingLevel = Mathf.Clamp(targetDatabase.StartingLevel, 1, targetDatabase.MaxWaveLevel);
            if (_locationProgressById.TryGetValue(locationId, out var progressState))
            {
                maxUnlockedLevel = Mathf.Clamp(progressState.MaxUnlockedLevel, startingLevel, targetDatabase.MaxWaveLevel);
                selectedLevel = Mathf.Clamp(progressState.SelectedLevel, startingLevel, maxUnlockedLevel);
                completedLevelCount = Mathf.Clamp(progressState.CompletedLevelCount, 0, targetDatabase.MaxWaveLevel);
                return true;
            }

            selectedLevel = startingLevel;
            maxUnlockedLevel = startingLevel;
            completedLevelCount = 0;
            return true;
        }

        private void SetSelectedLevel(int level)
        {
            int minLevel = StartingLevel;
            _selectedLevel = Mathf.Clamp(level, minLevel, _maxUnlockedLevel);
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
            _currentClearedWaves = 0;
            PersistSelectedLocationProgress();
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

            if (!_battleActive || ActiveDatabase == null)
                return;

            var context = BuildWaveContext();
            var packages = _waveFactory.CreateWave(context);
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

        private WaveContext BuildWaveContext()
        {
            int wavesInLevel = Mathf.Max(1, WavesToUnlockNextLevelInternal);
            int waveIndex = Mathf.Clamp(_currentClearedWaves + 1, 1, wavesInLevel);
            var draftContext = new WaveContext(_selectedLevel, waveIndex, wavesInLevel);

            if (ActiveDatabase != null &&
                ActiveDatabase.BossBalance != null &&
                ActiveDatabase.BossBalance.TryGetRule(draftContext, out var bossRule))
            {
                return new WaveContext(
                    _selectedLevel,
                    waveIndex,
                    wavesInLevel,
                    true,
                    bossRule.BossCount,
                    bossRule.TotalEnemiesInWave,
                    bossRule.MaxBossAffixes);
            }

            return draftContext;
        }

        private void HandleEnemyDeath(Unit unit)
        {
            if (unit is not EnemyUnit enemy)
                return;

            if (_activeEnemies.Remove(enemy) == false)
                return;

            enemy.OnDeath -= HandleEnemyDeath;

            List<InventoryItem> resolvedDrops = ResolveDrops(enemy);
            OnEnemyDropsResolved?.Invoke(enemy, resolvedDrops);

            if (_activeEnemies.Count > 0)
                return;

            RegisterWaveClear();

            if (_battleActive)
                ScheduleRespawn(RespawnDelay);
        }

        public List<InventoryItem> ResolveDrops(EnemyUnit enemy)
        {
            if (enemy == null)
                return new List<InventoryItem>();

            return _itemDropResolver.Resolve(enemy);
        }

        private void RegisterWaveClear()
        {
            _currentClearedWaves++;

            if (_currentClearedWaves >= WavesToUnlockNextLevelInternal)
                RegisterCompletedLevel();

            if (_currentClearedWaves >= WavesToUnlockNextLevelInternal && _autoProgressionEnabled)
            {
                _player.ResetCombatState();
                _maxUnlockedLevel = Mathf.Min(_maxUnlockedLevel + 1, MaxWaveLevel);
                PersistSelectedLocationProgress();
                SelectNextLevel();
            }
            OnWaveCleared?.Invoke();
        }

        private void ScheduleRespawn(float delay)
        {
            if (!_battleActive)
                return;

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

            if (!_battleActive)
                yield break;

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

        public ProgressSaveData CaptureSaveData()
        {
            PersistSelectedLocationProgress();

            var locations = new List<LocationProgressSaveData>(_locationProgressById.Count);
            foreach (var pair in _locationProgressById)
            {
                if (pair.Value == null)
                    continue;

                locations.Add(new LocationProgressSaveData
                {
                    locationId = pair.Key,
                    selectedLevel = pair.Value.SelectedLevel,
                    maxUnlockedLevel = pair.Value.MaxUnlockedLevel,
                    completedLevelCount = pair.Value.CompletedLevelCount,
                    claimedRewardIds = new List<string>(pair.Value.ClaimedRewardIds)
                });
            }

            return new ProgressSaveData
            {
                selectedLocationId = _selectedLocationId,
                locations = locations
            };
        }

        public void ApplySaveData(ProgressSaveData saveData)
        {
            if (saveData == null)
            {
                ResetProgressToDefaults();
                return;
            }

            _locationProgressById.Clear();

            if (saveData.locations != null)
            {
                for (int i = 0; i < saveData.locations.Count; i++)
                {
                    var locationSave = saveData.locations[i];
                    if (locationSave == null || string.IsNullOrWhiteSpace(locationSave.locationId))
                        continue;

                    var saveDatabase = ResolveDatabaseForLocation(locationSave.locationId);
                    int minLevel = saveDatabase != null ? saveDatabase.StartingLevel : 1;
                    int maxLevel = saveDatabase != null ? saveDatabase.MaxWaveLevel : 100;

                    _locationProgressById[locationSave.locationId] = new LocationProgressState
                    {
                        SelectedLevel = Mathf.Clamp(locationSave.selectedLevel, minLevel, Mathf.Clamp(locationSave.maxUnlockedLevel, minLevel, maxLevel)),
                        MaxUnlockedLevel = Mathf.Clamp(locationSave.maxUnlockedLevel, minLevel, maxLevel),
                        CompletedLevelCount = Mathf.Clamp(locationSave.completedLevelCount, 0, maxLevel),
                        ClaimedRewardIds = locationSave.claimedRewardIds != null
                            ? new HashSet<string>(locationSave.claimedRewardIds, StringComparer.Ordinal)
                            : new HashSet<string>(StringComparer.Ordinal)
                    };
                }
            }

            string locationIdToApply = string.IsNullOrWhiteSpace(saveData.selectedLocationId)
                ? GetDefaultLocationId()
                : saveData.selectedLocationId;

            if (TryResolveLocation(locationIdToApply, out var location))
            {
                ApplySelectedLocation(location, locationIdToApply, false);
            }
            else
            {
                InitializeSelectedLocation();
            }

            if (_hasStarted && isActiveAndEnabled)
            {
                if (_battleActive)
                    SpawnCurrentLevel();
                else
                    DeactivatePool();
            }
        }

        public void ResetProgressToDefaults()
        {
            _locationProgressById.Clear();
            InitializeSelectedLocation();

            if (_hasStarted && isActiveAndEnabled)
            {
                if (_battleActive)
                    SpawnCurrentLevel();
                else
                    DeactivatePool();
            }
        }

        private void InitializeSelectedLocation()
        {
            string defaultLocationId = GetDefaultLocationId();
            if (TryResolveLocation(defaultLocationId, out var location))
            {
                ApplySelectedLocation(location, defaultLocationId, false);
                return;
            }

            _selectedLocation = null;
            _selectedLocationId = FallbackLocationId;
            _selectedLocationProgress = GetOrCreateProgressState(_selectedLocationId, ActiveDatabase);
            CreateWaveFactoryIfPossible();
            LoadSelectedLocationProgress();
            OnLocationChanged?.Invoke();
            OnLevelChanged?.Invoke();
        }

        private void ApplySelectedLocation(LocationDefinition location, string locationId, bool respawn)
        {
            _selectedLocation = location;
            _selectedLocationId = string.IsNullOrWhiteSpace(locationId)
                ? GetDefaultLocationId()
                : locationId;
            _selectedLocationProgress = GetOrCreateProgressState(_selectedLocationId, ActiveDatabase);

            CreateWaveFactoryIfPossible();
            LoadSelectedLocationProgress();

            OnLocationChanged?.Invoke();
            OnLevelChanged?.Invoke();

            if (!respawn || !_hasStarted || !isActiveAndEnabled)
                return;

            SpawnCurrentLevel();
        }

        private void SetBattleActive(bool battleActive, bool restartCurrentLevel = false)
        {
            if (_battleActive == battleActive)
            {
                if (_battleActive && restartCurrentLevel && _hasStarted && isActiveAndEnabled)
                    SpawnCurrentLevel();

                return;
            }

            _battleActive = battleActive;
            _currentClearedWaves = 0;

            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }

            UnsubscribeFromActiveEnemies();
            DeactivatePool();
            OnBattleActivityChanged?.Invoke(_battleActive);
            OnLevelChanged?.Invoke();

            if (_battleActive && restartCurrentLevel && _hasStarted && isActiveAndEnabled)
                SpawnCurrentLevel();
        }

        private string GetDefaultLocationId()
        {
            LocationDefinition defaultLocation = locationCatalog != null ? locationCatalog.GetDefaultLocation() : null;
            if (defaultLocation != null)
                return defaultLocation.LocationId;

            return FallbackLocationId;
        }

        private bool TryResolveLocation(string locationId, out LocationDefinition location)
        {
            if (locationCatalog != null && locationCatalog.TryGetLocation(locationId, out location))
                return true;

            location = null;
            return string.Equals(locationId, FallbackLocationId, StringComparison.Ordinal);
        }

        private EnemyConfigDatabase ResolveDatabaseForLocation(string locationId)
        {
            if (locationCatalog != null &&
                locationCatalog.TryGetLocation(locationId, out var location) &&
                location != null)
            {
                return location.EnemyDatabase;
            }

            return string.Equals(locationId, FallbackLocationId, StringComparison.Ordinal) ? database : null;
        }

        private LocationProgressState GetOrCreateProgressState(string locationId, EnemyConfigDatabase targetDatabase)
        {
            if (_locationProgressById.TryGetValue(locationId, out var state))
                return state;

            int minLevel = targetDatabase != null ? targetDatabase.StartingLevel : 1;
            state = new LocationProgressState
            {
                SelectedLevel = minLevel,
                MaxUnlockedLevel = minLevel,
                CompletedLevelCount = 0,
                ClaimedRewardIds = new HashSet<string>(StringComparer.Ordinal)
            };

            _locationProgressById[locationId] = state;
            return state;
        }

        private void LoadSelectedLocationProgress()
        {
            int minLevel = StartingLevel;
            _maxUnlockedLevel = Mathf.Clamp(_selectedLocationProgress.MaxUnlockedLevel, minLevel, MaxWaveLevel);
            _selectedLevel = Mathf.Clamp(_selectedLocationProgress.SelectedLevel, minLevel, _maxUnlockedLevel);
            _currentClearedWaves = 0;
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
        }

        private void PersistSelectedLocationProgress()
        {
            if (_selectedLocationProgress == null)
                return;

            _selectedLocationProgress.SelectedLevel = _selectedLevel;
            _selectedLocationProgress.MaxUnlockedLevel = _maxUnlockedLevel;
        }

        private void RegisterCompletedLevel()
        {
            if (_selectedLocationProgress == null)
                return;

            int completedLevel = Mathf.Clamp(_selectedLevel, 0, MaxWaveLevel);
            int previousCompletedLevelCount = _selectedLocationProgress.CompletedLevelCount;
            _selectedLocationProgress.CompletedLevelCount = Mathf.Max(
                previousCompletedLevelCount,
                completedLevel);

            if (completedLevel <= previousCompletedLevelCount)
                return;

            ResolveLocationRewardsForCompletedLevel(completedLevel);
        }

        private void ResolveLocationRewardsForCompletedLevel(int completedLevel)
        {
            if (_selectedLocation == null || _selectedLocation.LevelRewards == null || _selectedLocation.LevelRewards.Count == 0)
                return;

            List<InventoryItem> rewardItems = new();
            IReadOnlyList<LocationLevelRewardEntry> rewards = _selectedLocation.LevelRewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                LocationLevelRewardEntry reward = rewards[i];
                if (reward == null || reward.LevelNumber != completedLevel || reward.ItemDefinition == null)
                    continue;

                string rewardId = reward.GetRewardId(_selectedLocation);
                if (!TryClaimReward(rewardId))
                    continue;

                InventoryItem rewardItem = reward.CreateRewardItem();
                if (rewardItem == null || rewardItem.IsEmpty)
                    continue;

                rewardItems.Add(rewardItem);
            }

            if (rewardItems.Count <= 0)
                return;

            Vector3 spawnPosition = GetLocationRewardSpawnPosition();
            OnLocationRewardsResolved?.Invoke(rewardItems, spawnPosition);
        }

        private Vector3 GetLocationRewardSpawnPosition()
        {
            if (pool != null && pool.Units != null && pool.Units.Count > 0 && pool.Units[0] != null)
                return pool.Units[0].transform.position;

            return _player != null ? _player.transform.position : Vector3.zero;
        }

        private void CreateWaveFactoryIfPossible()
        {
            if (ActiveDatabase == null)
            {
                _waveFactory = null;
                return;
            }

            var enemyFactory = new EnemyFactory(ActiveDatabase);
            _waveFactory = new WaveFactory(enemyFactory, ActiveDatabase);
        }

        [Serializable]
        private sealed class LocationProgressState
        {
            public int SelectedLevel;
            public int MaxUnlockedLevel;
            public int CompletedLevelCount;
            public HashSet<string> ClaimedRewardIds = new(StringComparer.Ordinal);
        }
    }
}
