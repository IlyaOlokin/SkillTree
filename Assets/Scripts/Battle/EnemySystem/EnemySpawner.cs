using System;
using System.Collections;
using System.Collections.Generic;
using DropSystem;
using SaveSystem;
using UnityEngine;
using Zenject;

namespace Battle
{
    public class EnemySpawner : MonoBehaviour
    {
        private static readonly List<EnemySpawner> ActiveSpawners = new();

        private const float NormalEnemyWaveWeight = 0.33f;

        [SerializeField] private EnemyPool pool;
        [SerializeField] private EnemyConfigDatabase database;
        [SerializeField] private LocationCatalog locationCatalog;

        [Inject] private PlayerUnit _player;

        private int _currentClearedWaves;
        
        private EnemyLocationProgressService _locationProgress;
        private WaveFactory _waveFactory;
        private GoldDropResolver _goldDropResolver;
        private readonly List<EnemyUnit> _activeEnemies = new();
        private Coroutine _respawnCoroutine;
        private bool _hasStarted;
        private bool _battleActive = true;

        private EnemyConfigDatabase ActiveDatabase => _locationProgress?.ActiveDatabase;
        private int StartingLevel => _locationProgress != null ? _locationProgress.StartingLevel : 1;
        private int WavesToUnlockNextLevelInternal => _locationProgress != null ? _locationProgress.WavesToUnlockNextLevel : 10;
        private float RespawnDelay => _locationProgress != null ? _locationProgress.RespawnDelay : 2f;

        public int SelectedLevel => _locationProgress != null ? _locationProgress.SelectedLevel : 1;
        public int MaxUnlockedLevel => _locationProgress != null ? _locationProgress.MaxUnlockedLevel : 1;
        public int WavesToUnlockNextLevel => WavesToUnlockNextLevelInternal;
        public int CurrentClearedWaves => _currentClearedWaves;
        public int CurrentLocationStartingLevel => StartingLevel;
        public string SelectedLocationId => _locationProgress?.SelectedLocationId;
        public LocationDefinition SelectedLocation => _locationProgress?.SelectedLocation;
        public IReadOnlyList<LocationDefinition> Locations => _locationProgress != null ? _locationProgress.Locations : Array.Empty<LocationDefinition>();
        public bool IsBattleActive => _battleActive;
        public PlayerUnit Player => _player;
        public EnemyAffixGenerationRules AffixRules { get; } = new();
        
        public event Action OnLocationChanged;
        public event Action OnLevelChanged;
        public event Action OnWaveCleared;
        public event Action<int> OnWaveClearedNumber;
        public event Action<LocationDefinition, int> OnLocationCompletedFirstTime;
        public event Action<EnemyUnit, GoldDropResult> OnEnemyGoldResolved;
        public event Action<bool> OnBattleActivityChanged;

        public static EnemySpawner For(Unit unit)
        {
            if (unit == null)
                return null;

            for (int i = 0; i < ActiveSpawners.Count; i++)
            {
                EnemySpawner spawner = ActiveSpawners[i];
                if (spawner != null && spawner.Player == unit)
                    return spawner;
            }

            return null;
        }

        private void OnEnable()
        {
            if (!ActiveSpawners.Contains(this))
                ActiveSpawners.Add(this);
        }

        private void OnDisable()
        {
            ActiveSpawners.Remove(this);
        }

        private void Awake()
        {
            if (locationCatalog == null && database == null)
            {
                Debug.LogError($"{nameof(EnemySpawner)} requires either {nameof(LocationCatalog)} or {nameof(EnemyConfigDatabase)} reference.", this);
                enabled = false;
                return;
            }

            _locationProgress = new EnemyLocationProgressService(database, locationCatalog);
            _locationProgress.Initialize();
            ApplyLocationProgressChanges(false);
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
            SetSelectedLevel(SelectedLevel - 1);

            if (!_battleActive)
                return;

            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        public void SelectNextLevel()
        {
            SetSelectedLevel(SelectedLevel + 1);

            if (!_battleActive)
                return;

            DeactivatePool();
            ScheduleRespawn(RespawnDelay);
        }

        public void RestartCurrentLevel()
        {
            SetSelectedLevel(SelectedLevel);

            if (!_battleActive)
                return;

            SpawnCurrentLevel();
        }

        public bool SelectLocation(string locationId, bool restartBattle = true)
        {
            if (_locationProgress == null || _locationProgress.TrySelectLocation(locationId) == false)
                return false;

            ApplyLocationProgressChanges(restartBattle && _battleActive);
            return true;
        }

        public bool IsLocationUnlocked(LocationDefinition location)
        {
            return _locationProgress != null && _locationProgress.IsLocationUnlocked(location);
        }

        public bool IsLocationUnlocked(string locationId)
        {
            return _locationProgress != null && _locationProgress.IsLocationUnlocked(locationId);
        }

        public bool IsLocationCompleted(LocationDefinition location)
        {
            return _locationProgress != null && _locationProgress.IsLocationCompleted(location);
        }

        public bool IsLocationCompleted(string locationId)
        {
            return _locationProgress != null && _locationProgress.IsLocationCompleted(locationId);
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
            return _locationProgress != null && _locationProgress.HasClaimedReward(rewardId);
        }

        public bool HasClaimedReward(string locationId, string rewardId)
        {
            return _locationProgress != null && _locationProgress.HasClaimedReward(locationId, rewardId);
        }

        public bool TryClaimReward(string rewardId)
        {
            return _locationProgress != null && _locationProgress.TryClaimReward(rewardId);
        }

        public bool TryClaimReward(PendingLocationReward pendingReward)
        {
            return _locationProgress != null && _locationProgress.TryClaimReward(pendingReward);
        }

        public List<PendingLocationReward> GetPendingLocationRewards(int completedLevel)
        {
            return _locationProgress != null
                ? _locationProgress.GetPendingLocationRewards(completedLevel)
                : new List<PendingLocationReward>();
        }

        public bool TryGetLocationProgress(string locationId, out int selectedLevel, out int maxUnlockedLevel, out int completedLevelCount)
        {
            if (_locationProgress != null)
                return _locationProgress.TryGetLocationProgress(locationId, out selectedLevel, out maxUnlockedLevel, out completedLevelCount);

            selectedLevel = 1;
            maxUnlockedLevel = 1;
            completedLevelCount = 0;
            return false;
        }

        public bool TryGetActiveWavePower(out float totalPower)
        {
            totalPower = 0f;

            if (ActiveDatabase == null)
                return false;

            WaveContext context = BuildWaveContext();
            totalPower = ActiveDatabase.GetPowerForLevel(context.Level);

            if (ActiveDatabase.WavePowerBalance != null)
                totalPower *= ActiveDatabase.WavePowerBalance.GetMultiplier(context);

            return totalPower > 0f;
        }

        public bool TryGetActiveWaveNormalEnemyEstimates(out float accuracy, out float physicalDamage)
        {
            const float normalEnemyOffenceRatio = 0.33f;

            accuracy = 0f;
            physicalDamage = 0f;

            if (TryGetActiveWavePower(out float totalPower) == false)
                return false;

            accuracy = totalPower / 7f;

            float physicalBudget = totalPower * NormalEnemyWaveWeight * normalEnemyOffenceRatio;
            EnemyStatBudgetRule rule = ActiveDatabase.StatBudgetConfig != null
                ? ActiveDatabase.StatBudgetConfig.GetRule(StatType.PhysicalDamage)
                : EnemyStatBudgetRuleDefaults.Get(StatType.PhysicalDamage);
            physicalDamage = rule.Evaluate(physicalBudget, normalEnemyOffenceRatio);
            return true;
        }

        public bool TryGetActiveWaveNormalEnemyBaseEvasionEstimate(out float evasion)
        {
            evasion = 0f;

            if (TryGetActiveWavePower(out float totalPower) == false)
                return false;

            evasion = totalPower / 30f;
            return true;
        }

        private void SetSelectedLevel(int level)
        {
            _locationProgress?.SetSelectedLevel(level);
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

            if (!_battleActive || ActiveDatabase == null)
                return;

            var context = BuildWaveContext();
            var packages = _waveFactory.CreateWave(context, AffixRules);
            int spawnCount = Mathf.Min(packages.Count, pool.Units.Count);
            pool.PositionEnemies(spawnCount);
            var enemiesForResolver = new List<Unit>(spawnCount);

            for (int i = 0; i < spawnCount; i++)
            {
                if (pool.Units[i] is not EnemyUnit enemy)
                {
                    packages[i]?.Dispose();
                    continue;
                }

                if (packages[i] == null)
                    continue;

                enemy.Initialize(packages[i]);
                enemy.OnDeath += HandleEnemyDeath;
                enemy.gameObject.SetActive(true);
                _activeEnemies.Add(enemy);
                enemiesForResolver.Add(enemy);
            }

            for (int i = spawnCount; i < packages.Count; i++)
                packages[i]?.Dispose();

            pool.attackResolver?.SetNewEnemies(enemiesForResolver);
        }

        private WaveContext BuildWaveContext()
        {
            int wavesInLevel = Mathf.Max(1, WavesToUnlockNextLevelInternal);
            int waveIndex = Mathf.Clamp(_currentClearedWaves + 1, 1, wavesInLevel);
            var draftContext = new WaveContext(SelectedLevel, waveIndex, wavesInLevel);

            if (ActiveDatabase != null &&
                ActiveDatabase.BossBalance != null &&
                ActiveDatabase.BossBalance.TryGetRule(draftContext, out var bossRule))
            {
                return new WaveContext(
                    SelectedLevel,
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

            GoldDropResult resolvedGold = ResolveGold(enemy);
            if (resolvedGold.HasGold)
                OnEnemyGoldResolved?.Invoke(enemy, resolvedGold);

            if (_activeEnemies.Count > 0)
                return;

            WaveContext clearedContext = BuildWaveContext();
            bool shouldContinueBattle = RegisterWaveClear(clearedContext);

            if (_battleActive && shouldContinueBattle)
                ScheduleRespawn(RespawnDelay);
        }

        public GoldDropResult ResolveGold(EnemyUnit enemy)
        {
            if (enemy == null)
                return default;

            _goldDropResolver ??= new GoldDropResolver(ActiveDatabase != null ? ActiveDatabase.GoldDropConfig : null);
            return _goldDropResolver.Resolve(enemy, SelectedLevel);
        }

        private bool RegisterWaveClear(WaveContext clearedContext)
        {
            _currentClearedWaves++;
            int clearedWaveNumber = _currentClearedWaves;

            bool completedLevelFirstTime = false;
            int completedLevel = SelectedLevel;
            if (_currentClearedWaves >= WavesToUnlockNextLevelInternal)
                completedLevelFirstTime = _locationProgress != null && _locationProgress.RegisterCompletedLevel(out completedLevel);

            bool shouldShowLocationComplete =
                clearedContext.IsBossWave &&
                completedLevelFirstTime;

            if (_currentClearedWaves >= WavesToUnlockNextLevelInternal &&
                _locationProgress != null &&
                _locationProgress.AutoProgressionEnabled)
            {
                _player.ResetCombatState();
                _locationProgress.UnlockNextLevel();

                if (!shouldShowLocationComplete)
                    SelectNextLevel();
            }

            OnWaveCleared?.Invoke();
            OnWaveClearedNumber?.Invoke(clearedWaveNumber);

            if (shouldShowLocationComplete)
                OnLocationCompletedFirstTime?.Invoke(SelectedLocation, completedLevel);

            return !shouldShowLocationComplete;
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
            ActiveSpawners.Remove(this);
            UnsubscribeFromActiveEnemies();
        }

        public ProgressSaveData CaptureSaveData()
        {
            return _locationProgress != null
                ? _locationProgress.CaptureSaveData()
                : new ProgressSaveData();
        }

        public void ApplySaveData(ProgressSaveData saveData)
        {
            _locationProgress ??= new EnemyLocationProgressService(database, locationCatalog);
            _locationProgress.ApplySaveData(saveData);
            ApplyLocationProgressChanges(false);

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
            _locationProgress ??= new EnemyLocationProgressService(database, locationCatalog);
            _locationProgress.ResetProgressToDefaults();
            ApplyLocationProgressChanges(false);

            if (_hasStarted && isActiveAndEnabled)
            {
                if (_battleActive)
                    SpawnCurrentLevel();
                else
                    DeactivatePool();
            }
        }

        private void ApplyLocationProgressChanges(bool respawn)
        {
            CreateWaveFactoryIfPossible();
            _currentClearedWaves = 0;

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

        private void CreateWaveFactoryIfPossible()
        {
            if (ActiveDatabase == null)
            {
                _waveFactory = null;
                _goldDropResolver = null;
                return;
            }

            var enemyFactory = new EnemyFactory(ActiveDatabase);
            _waveFactory = new WaveFactory(enemyFactory, ActiveDatabase);
            _goldDropResolver = new GoldDropResolver(ActiveDatabase.GoldDropConfig);
        }
    }
}
