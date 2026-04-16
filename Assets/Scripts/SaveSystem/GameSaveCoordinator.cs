using System;
using Battle;
using Gems;
using InventorySystem;
using LocalizationSupport;
using SkillTree;
using UnityEngine;
using Zenject;

namespace SaveSystem
{
    public sealed class GameSaveCoordinator : IInitializable, ITickable, IDisposable
    {
        private const int PlayerDocumentVersion = 1;
        private const int ProgressDocumentVersion = 1;
        private const int SkillTreeDocumentVersion = 1;
        private const int InventoryDocumentVersion = 1;
        private const float DeferredSaveDelaySeconds = 0.75f;
        private readonly UnitLevel _unitLevel;
        private readonly EnemySpawner _enemySpawner;
        private readonly MainSkillTree _skillTree;
        private readonly PlayerInventory _playerInventory;
        private readonly SaveFileStorage _storage;
        private readonly SaveProfileManager _profileManager;
        private readonly GemDefinitionCatalog _gemDefinitionCatalog;
        private readonly CloudSettingsService _cloudSettingsService;
        private readonly LocalSettingsService _localSettingsService;

        private readonly SaveMigrationPipeline<PlayerSaveData> _playerMigrations = new(Array.Empty<ISaveDataMigration<PlayerSaveData>>());
        private readonly SaveMigrationPipeline<ProgressSaveData> _progressMigrations = new(Array.Empty<ISaveDataMigration<ProgressSaveData>>());
        private readonly SaveMigrationPipeline<SkillTreeSaveData> _skillTreeMigrations = new(Array.Empty<ISaveDataMigration<SkillTreeSaveData>>());
        private readonly SaveMigrationPipeline<InventorySaveData> _inventoryMigrations = new(Array.Empty<ISaveDataMigration<InventorySaveData>>());

        private SaveProfileDescriptor _activeProfile;
        private bool _isApplyingSaveState;
        private bool _playerDirty;
        private bool _progressDirty;
        private bool _skillTreeDirty;
        private bool _inventoryDirty;
        private float _lastDirtyTime;

        public GameSaveCoordinator(
            UnitLevel unitLevel,
            EnemySpawner enemySpawner,
            MainSkillTree skillTree,
            PlayerInventory playerInventory,
            SaveFileStorage storage,
            SaveProfileManager profileManager,
            GemDefinitionCatalog gemDefinitionCatalog,
            CloudSettingsService cloudSettingsService,
            LocalSettingsService localSettingsService)
        {
            _unitLevel = unitLevel;
            _enemySpawner = enemySpawner;
            _skillTree = skillTree;
            _playerInventory = playerInventory;
            _storage = storage;
            _profileManager = profileManager;
            _gemDefinitionCatalog = gemDefinitionCatalog;
            _cloudSettingsService = cloudSettingsService;
            _localSettingsService = localSettingsService;
        }

        public SaveProfileDescriptor ActiveProfile => _activeProfile;

        public void Initialize()
        {
            _cloudSettingsService.Load();
            _localSettingsService.Load();
            _activeProfile = _profileManager.GetOrCreateActiveProfile(GetDefaultProfileDisplayName());
            LoadActiveProfile();
            Subscribe();
            Application.quitting += HandleApplicationQuitting;
        }

        public void Tick()
        {
            if (_isApplyingSaveState)
                return;

            if (!HasDirtyDocuments())
                return;

            if (Time.unscaledTime - _lastDirtyTime < DeferredSaveDelaySeconds)
                return;

            SaveDirtyDocuments();
        }

        public void Dispose()
        {
            Application.quitting -= HandleApplicationQuitting;
            Unsubscribe();
        }

        public SaveProfileDescriptor CreateProfile(string displayName, bool makeActive = true)
        {
            SaveDirtyDocuments();
            SaveProfileDescriptor profile = _profileManager.CreateProfile(displayName, makeActive);
            if (makeActive)
            {
                _activeProfile = profile;
                ApplyDefaultsForFreshProfile();
                SaveAllDocuments();
            }

            return profile;
        }

        public bool TrySwitchProfile(string profileId)
        {
            SaveDirtyDocuments();
            if (!_profileManager.TrySetActiveProfile(profileId))
                return false;

            _activeProfile = _profileManager.GetOrCreateActiveProfile(GetDefaultProfileDisplayName());
            LoadActiveProfile();
            return true;
        }

        public void SaveNow()
        {
            SaveAllDocuments();
        }

        private void Subscribe()
        {
            _unitLevel.OnExpChanged += HandlePlayerChanged;
            _unitLevel.OnLevelUp += HandlePlayerLevelUp;
            _unitLevel.OnSkillPointsChanged += HandleSkillPointsChanged;
            _enemySpawner.OnLevelChanged += HandleProgressChanged;
            _enemySpawner.OnWaveCleared += HandleWaveCleared;
            _skillTree.OnSkillTreeChanged += HandleSkillTreeChanged;
            _playerInventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void Unsubscribe()
        {
            _unitLevel.OnExpChanged -= HandlePlayerChanged;
            _unitLevel.OnLevelUp -= HandlePlayerLevelUp;
            _unitLevel.OnSkillPointsChanged -= HandleSkillPointsChanged;
            _enemySpawner.OnLevelChanged -= HandleProgressChanged;
            _enemySpawner.OnWaveCleared -= HandleWaveCleared;
            _skillTree.OnSkillTreeChanged -= HandleSkillTreeChanged;
            _playerInventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void LoadActiveProfile()
        {
            _isApplyingSaveState = true;
            try
            {
                LoadPlayerState();
                LoadProgressState();
                LoadSkillTreeState();
                LoadInventoryState();
                ClearDirtyFlags();
            }
            finally
            {
                _isApplyingSaveState = false;
            }
        }

        private void LoadPlayerState()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetPlayerFile(_activeProfile.ProfileId),
                    SaveDocumentType.Player,
                    PlayerDocumentVersion,
                    _playerMigrations,
                    out PlayerSaveData saveData))
            {
                _unitLevel.ApplySaveData(saveData);
                return;
            }

            _unitLevel.ResetToDefaults();
        }

        private void LoadProgressState()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetProgressFile(_activeProfile.ProfileId),
                    SaveDocumentType.Progress,
                    ProgressDocumentVersion,
                    _progressMigrations,
                    out ProgressSaveData saveData))
            {
                _enemySpawner.ApplySaveData(saveData);
                return;
            }

            _enemySpawner.ResetProgressToDefaults();
        }

        private void LoadSkillTreeState()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetSkillTreeFile(_activeProfile.ProfileId),
                    SaveDocumentType.SkillTree,
                    SkillTreeDocumentVersion,
                    _skillTreeMigrations,
                    out SkillTreeSaveData saveData))
            {
                _skillTree.ApplySaveData(saveData, RestoreGemInstance);
                return;
            }

            _skillTree.ResetToDefaults(RestoreGemInstance);
        }

        private void LoadInventoryState()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetInventoryFile(_activeProfile.ProfileId),
                    SaveDocumentType.Inventory,
                    InventoryDocumentVersion,
                    _inventoryMigrations,
                    out InventorySaveData saveData))
            {
                _playerInventory.ApplySaveData(saveData, RestoreGemInstance);
                return;
            }

            _playerInventory.ResetToDefaults(RestoreGemInstance);
        }

        private void ApplyDefaultsForFreshProfile()
        {
            _isApplyingSaveState = true;
            try
            {
                _unitLevel.ResetToDefaults();
                _enemySpawner.ResetProgressToDefaults();
                _skillTree.ResetToDefaults(RestoreGemInstance);
                _playerInventory.ResetToDefaults(RestoreGemInstance);
                ClearDirtyFlags();
            }
            finally
            {
                _isApplyingSaveState = false;
            }
        }

        private GemInstance RestoreGemInstance(GemInstanceSaveData saveData)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(saveData.definitionId))
                return null;

            if (!_gemDefinitionCatalog.TryResolve(saveData.definitionId, out GemDefinition definition))
            {
                Debug.LogWarning($"Unable to resolve gem definition '{saveData.definitionId}' during save load.");
                return null;
            }

            return GemInstance.Restore(definition, saveData.instanceId, saveData.rolledValues);
        }

        private void HandleApplicationQuitting()
        {
            SaveDirtyDocuments();
            _cloudSettingsService.Save();
            _localSettingsService.Save();
        }

        private void HandlePlayerChanged()
        {
            MarkPlayerDirty();
        }

        private void HandlePlayerLevelUp(int _)
        {
            MarkPlayerDirty();
        }

        private void HandleSkillPointsChanged(int _)
        {
            MarkPlayerDirty();
        }

        private void HandleProgressChanged()
        {
            MarkProgressDirty();
        }

        private void HandleWaveCleared()
        {
            MarkPlayerDirty();
        }

        private void HandleSkillTreeChanged()
        {
            //_skillTree.RebuildAllocatedNodes();
            MarkPlayerDirty();
            MarkSkillTreeDirty();
        }

        private void HandleInventoryChanged()
        {
            MarkInventoryDirty();
        }

        private void MarkPlayerDirty()
        {
            if (_isApplyingSaveState)
                return;

            _playerDirty = true;
            _lastDirtyTime = Time.unscaledTime;
        }

        private void MarkProgressDirty()
        {
            if (_isApplyingSaveState)
                return;

            _progressDirty = true;
            _lastDirtyTime = Time.unscaledTime;
        }

        private void MarkSkillTreeDirty()
        {
            if (_isApplyingSaveState)
                return;

            _skillTreeDirty = true;
            _lastDirtyTime = Time.unscaledTime;
        }

        private void MarkInventoryDirty()
        {
            if (_isApplyingSaveState)
                return;

            _inventoryDirty = true;
            _lastDirtyTime = Time.unscaledTime;
        }

        private bool HasDirtyDocuments()
        {
            return _playerDirty || _progressDirty || _skillTreeDirty || _inventoryDirty;
        }

        private void SaveAllDocuments()
        {
            _playerDirty = true;
            _progressDirty = true;
            _skillTreeDirty = true;
            _inventoryDirty = true;
            SaveDirtyDocuments();
        }

        private void SaveDirtyDocuments()
        {
            if (_activeProfile == null)
                return;

            bool savedAnyDocument = false;

            if (_playerDirty)
            {
                _storage.SaveDocument(SavePaths.GetPlayerFile(_activeProfile.ProfileId), SaveDocumentType.Player, PlayerDocumentVersion, _unitLevel.CaptureSaveData());
                _playerDirty = false;
                savedAnyDocument = true;
            }

            if (_progressDirty)
            {
                _storage.SaveDocument(SavePaths.GetProgressFile(_activeProfile.ProfileId), SaveDocumentType.Progress, ProgressDocumentVersion, _enemySpawner.CaptureSaveData());
                _progressDirty = false;
                savedAnyDocument = true;
            }

            if (_skillTreeDirty)
            {
                _storage.SaveDocument(SavePaths.GetSkillTreeFile(_activeProfile.ProfileId), SaveDocumentType.SkillTree, SkillTreeDocumentVersion, _skillTree.CaptureSaveData());
                _skillTreeDirty = false;
                savedAnyDocument = true;
            }

            if (_inventoryDirty)
            {
                _storage.SaveDocument(SavePaths.GetInventoryFile(_activeProfile.ProfileId), SaveDocumentType.Inventory, InventoryDocumentVersion, _playerInventory.CaptureSaveData());
                _inventoryDirty = false;
                savedAnyDocument = true;
            }

            if (savedAnyDocument)
                _profileManager.TouchProfile(_activeProfile.ProfileId);
        }

        private void ClearDirtyFlags()
        {
            _playerDirty = false;
            _progressDirty = false;
            _skillTreeDirty = false;
            _inventoryDirty = false;
        }

        private static string GetDefaultProfileDisplayName()
        {
            return GameLocalization.Get("save.profile.defaultFirst", "Profile 1");
        }
    }
}
