using System;
using System.Collections.Generic;
using SaveSystem;
using UnityEngine;

namespace Battle
{
    internal sealed class EnemyLocationProgressService
    {
        private const string FallbackLocationId = "default";

        private readonly EnemyConfigDatabase _fallbackDatabase;
        private readonly LocationCatalog _locationCatalog;
        private readonly Dictionary<string, LocationProgressState> _locationProgressById = new(StringComparer.Ordinal);

        private int _selectedLevel;
        private int _maxUnlockedLevel;
        private bool _autoProgressionEnabled = true;
        private string _selectedLocationId;
        private LocationDefinition _selectedLocation;
        private LocationProgressState _selectedLocationProgress;

        public EnemyLocationProgressService(EnemyConfigDatabase fallbackDatabase, LocationCatalog locationCatalog)
        {
            _fallbackDatabase = fallbackDatabase;
            _locationCatalog = locationCatalog;
        }

        public EnemyConfigDatabase ActiveDatabase => _selectedLocation != null ? _selectedLocation.EnemyDatabase : _fallbackDatabase;
        public int StartingLevel => ActiveDatabase != null ? ActiveDatabase.StartingLevel : 1;
        public int WavesToUnlockNextLevel => GetWavesToUnlockNextLevel(_selectedLevel);
        public int MaxWaveLevel => ActiveDatabase != null ? ActiveDatabase.MaxWaveLevel : 100;
        public float RespawnDelay => ActiveDatabase != null ? ActiveDatabase.RespawnDelay : 2f;
        public int SelectedLevel => _selectedLevel;
        public int MaxUnlockedLevel => _maxUnlockedLevel;
        public bool AutoProgressionEnabled => _autoProgressionEnabled;
        public string SelectedLocationId => _selectedLocationId;
        public LocationDefinition SelectedLocation => _selectedLocation;
        public IReadOnlyList<LocationDefinition> Locations => _locationCatalog != null
            ? _locationCatalog.Locations
            : Array.Empty<LocationDefinition>();

        public void Initialize()
        {
            string defaultLocationId = GetDefaultLocationId();
            if (TryResolveLocation(defaultLocationId, out var location))
            {
                ApplySelectedLocation(location, defaultLocationId);
                return;
            }

            _selectedLocation = null;
            _selectedLocationId = FallbackLocationId;
            _selectedLocationProgress = GetOrCreateProgressState(_selectedLocationId, ActiveDatabase);
            LoadSelectedLocationProgress();
        }

        public bool TrySelectLocation(string locationId)
        {
            if (TryResolveLocation(locationId, out var location) == false)
                return false;

            if (location != null && IsLocationUnlocked(location) == false)
                return false;

            ApplySelectedLocation(location, locationId);
            return true;
        }

        public void SetSelectedLevel(int level)
        {
            int minLevel = StartingLevel;
            _selectedLevel = Mathf.Clamp(level, minLevel, _maxUnlockedLevel);
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
            PersistSelectedLocationProgress();
        }

        public int GetWavesToUnlockNextLevel(int level)
        {
            return ActiveDatabase != null ? ActiveDatabase.GetWavesToUnlockNextLevel(level) : 10;
        }

        public bool IsLocationUnlocked(LocationDefinition location)
        {
            if (location == null)
                return false;

            IReadOnlyList<LocationDefinition> prerequisites = location.UnlockPrerequisites;
            if (prerequisites == null || prerequisites.Count == 0)
                return true;

            for (int i = 0; i < prerequisites.Count; i++)
            {
                LocationDefinition prerequisite = prerequisites[i];
                if (prerequisite != null && IsLocationCompleted(prerequisite))
                    return true;
            }

            return false;
        }

        public bool IsLocationUnlocked(string locationId)
        {
            if (TryResolveLocation(locationId, out var location) == false || location == null)
                return false;

            return IsLocationUnlocked(location);
        }

        public bool IsLocationCompleted(LocationDefinition location)
        {
            if (location == null)
                return false;

            EnemyConfigDatabase locationDatabase = location.EnemyDatabase;
            if (locationDatabase == null)
                return false;

            if (TryGetLocationProgress(location.LocationId, out _, out _, out int completedLevelCount) == false)
                return false;

            return completedLevelCount >= locationDatabase.MaxWaveLevel;
        }

        public bool IsLocationCompleted(string locationId)
        {
            if (TryResolveLocation(locationId, out var location) == false || location == null)
                return false;

            return IsLocationCompleted(location);
        }

        public bool HasClaimedReward(string rewardId)
        {
            if (_selectedLocationProgress == null || string.IsNullOrWhiteSpace(rewardId))
                return false;

            return _selectedLocationProgress.ClaimedRewardIds.Contains(rewardId);
        }

        public bool HasClaimedReward(string locationId, string rewardId)
        {
            if (string.IsNullOrWhiteSpace(locationId) || string.IsNullOrWhiteSpace(rewardId))
                return false;

            if (!_locationProgressById.TryGetValue(locationId, out var progressState) || progressState == null)
                return false;

            return progressState.ClaimedRewardIds.Contains(rewardId);
        }

        public bool TryClaimReward(string rewardId)
        {
            if (_selectedLocationProgress == null || string.IsNullOrWhiteSpace(rewardId))
                return false;

            return _selectedLocationProgress.ClaimedRewardIds.Add(rewardId);
        }

        public bool TryClaimReward(PendingLocationReward pendingReward)
        {
            if (pendingReward == null || !pendingReward.IsValid)
                return false;

            if (_selectedLocation == null || pendingReward.Location != _selectedLocation)
                return false;

            return TryClaimReward(pendingReward.RewardId);
        }

        public List<PendingLocationReward> GetPendingLocationRewards(int completedLevel)
        {
            List<PendingLocationReward> pendingRewards = new();
            if (_selectedLocation == null || _selectedLocation.LevelRewards == null || _selectedLocation.LevelRewards.Count == 0)
                return pendingRewards;

            IReadOnlyList<LocationLevelRewardEntry> rewards = _selectedLocation.LevelRewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                LocationLevelRewardEntry reward = rewards[i];
                if (reward == null || reward.LevelNumber != completedLevel || !reward.IsValid)
                    continue;

                string rewardId = reward.GetRewardId(_selectedLocation);
                if (HasClaimedReward(rewardId))
                    continue;

                PendingLocationReward pendingReward = new(_selectedLocation, reward, rewardId);
                if (pendingReward.IsValid)
                    pendingRewards.Add(pendingReward);
            }

            return pendingRewards;
        }

        public bool TryGetLocationProgress(
            string locationId,
            out int selectedLevel,
            out int maxUnlockedLevel,
            out int completedLevelCount)
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

        public bool RegisterCompletedLevel(out int completedLevel)
        {
            completedLevel = Mathf.Clamp(_selectedLevel, 0, MaxWaveLevel);
            if (_selectedLocationProgress == null)
                return false;

            int previousCompletedLevelCount = _selectedLocationProgress.CompletedLevelCount;
            _selectedLocationProgress.CompletedLevelCount = Mathf.Max(
                previousCompletedLevelCount,
                completedLevel);

            return completedLevel > previousCompletedLevelCount;
        }

        public void UnlockNextLevel()
        {
            _maxUnlockedLevel = Mathf.Min(_maxUnlockedLevel + 1, MaxWaveLevel);
            PersistSelectedLocationProgress();
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

            if (TryResolveLocation(locationIdToApply, out var location) &&
                (location == null || IsLocationUnlocked(location)))
            {
                ApplySelectedLocation(location, locationIdToApply);
            }
            else
            {
                Initialize();
            }
        }

        public void ResetProgressToDefaults()
        {
            _locationProgressById.Clear();
            Initialize();
        }

        private void ApplySelectedLocation(LocationDefinition location, string locationId)
        {
            _selectedLocation = location;
            _selectedLocationId = string.IsNullOrWhiteSpace(locationId)
                ? GetDefaultLocationId()
                : locationId;
            _selectedLocationProgress = GetOrCreateProgressState(_selectedLocationId, ActiveDatabase);
            LoadSelectedLocationProgress();
        }

        private string GetDefaultLocationId()
        {
            LocationDefinition defaultLocation = _locationCatalog != null ? _locationCatalog.GetDefaultLocation() : null;
            if (defaultLocation != null)
                return defaultLocation.LocationId;

            return FallbackLocationId;
        }

        private bool TryResolveLocation(string locationId, out LocationDefinition location)
        {
            if (_locationCatalog != null && _locationCatalog.TryGetLocation(locationId, out location))
                return true;

            location = null;
            return string.Equals(locationId, FallbackLocationId, StringComparison.Ordinal);
        }

        private EnemyConfigDatabase ResolveDatabaseForLocation(string locationId)
        {
            if (_locationCatalog != null &&
                _locationCatalog.TryGetLocation(locationId, out var location) &&
                location != null)
            {
                return location.EnemyDatabase;
            }

            return string.Equals(locationId, FallbackLocationId, StringComparison.Ordinal)
                ? _fallbackDatabase
                : null;
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
            _autoProgressionEnabled = _selectedLevel >= _maxUnlockedLevel;
        }

        private void PersistSelectedLocationProgress()
        {
            if (_selectedLocationProgress == null)
                return;

            _selectedLocationProgress.SelectedLevel = _selectedLevel;
            _selectedLocationProgress.MaxUnlockedLevel = _maxUnlockedLevel;
        }

        private sealed class LocationProgressState
        {
            public int SelectedLevel;
            public int MaxUnlockedLevel;
            public int CompletedLevelCount;
            public HashSet<string> ClaimedRewardIds = new(StringComparer.Ordinal);
        }
    }
}
