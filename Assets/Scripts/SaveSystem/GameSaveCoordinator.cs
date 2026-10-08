using System;
using System.IO;
using System.Threading.Tasks;
using Battle;
using CurrencySystem;
using Gems;
using InventorySystem;
using Items;
using LocalizationSupport;
using ShopSystem;
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
        private const int SnapshotDocumentVersion = 1;
        private const float DeferredSaveDelaySeconds = 0.75f;
        private const double MaxDeferredSaveSeconds = 5d;
        private const double SaveRetryDelaySeconds = 5d;
        private readonly UnitLevel _unitLevel;
        private readonly PlayerWallet _playerWallet;
        private readonly EnemySpawner _enemySpawner;
        private readonly MainSkillTree _skillTree;
        private readonly PlayerInventory _playerInventory;
        private readonly ShopService _shopService;
        private readonly SaveFileStorage _storage;
        private readonly SaveProfileManager _profileManager;
        private readonly GemDefinitionCatalog _gemDefinitionCatalog;
        private readonly ItemDefinitionCatalog _itemDefinitionCatalog;
        private readonly CloudSettingsService _cloudSettingsService;
        private readonly LocalSettingsService _localSettingsService;

        private readonly SaveMigrationPipeline<PlayerSaveData> _playerMigrations = new(Array.Empty<ISaveDataMigration<PlayerSaveData>>());
        private readonly SaveMigrationPipeline<ProgressSaveData> _progressMigrations = new(Array.Empty<ISaveDataMigration<ProgressSaveData>>());
        private readonly SaveMigrationPipeline<SkillTreeSaveData> _skillTreeMigrations = new(Array.Empty<ISaveDataMigration<SkillTreeSaveData>>());
        private readonly SaveMigrationPipeline<InventorySaveData> _inventoryMigrations = new(Array.Empty<ISaveDataMigration<InventorySaveData>>());
        private readonly SaveMigrationPipeline<ProfileSnapshotSaveData> _snapshotMigrations = new(Array.Empty<ISaveDataMigration<ProfileSnapshotSaveData>>());

        private SaveProfileDescriptor _activeProfile;
        private bool _isApplyingSaveState;
        private bool _playerDirty;
        private bool _progressDirty;
        private bool _skillTreeDirty;
        private bool _inventoryDirty;
        private bool _tutorialsDirty;
        private readonly Tutorials.TutorialService _tutorials;
        private double _lastDirtyTime;
        private double _firstDirtyTime;
        private double _nextSaveAttemptTime;
        private bool _saveBlocked;
        private Task _pendingSave;
        private string _pendingSaveProfileId;

        public GameSaveCoordinator(
            UnitLevel unitLevel,
            PlayerWallet playerWallet,
            EnemySpawner enemySpawner,
            MainSkillTree skillTree,
            PlayerInventory playerInventory,
            ShopService shopService,
            SaveFileStorage storage,
            SaveProfileManager profileManager,
            GemDefinitionCatalog gemDefinitionCatalog,
            ItemDefinitionCatalog itemDefinitionCatalog,
            CloudSettingsService cloudSettingsService,
            LocalSettingsService localSettingsService,
            Tutorials.TutorialService tutorials)
        {
            _unitLevel = unitLevel;
            _playerWallet = playerWallet;
            _enemySpawner = enemySpawner;
            _skillTree = skillTree;
            _playerInventory = playerInventory;
            _shopService = shopService;
            _storage = storage;
            _profileManager = profileManager;
            _gemDefinitionCatalog = gemDefinitionCatalog;
            _itemDefinitionCatalog = itemDefinitionCatalog;
            _cloudSettingsService = cloudSettingsService;
            _localSettingsService = localSettingsService;
            _tutorials = tutorials;
        }

        public SaveProfileDescriptor ActiveProfile => _activeProfile;
        public bool IsFreshProfile { get; private set; }

        public void Initialize()
        {
            _cloudSettingsService.Load();
            _localSettingsService.Load();
            _activeProfile = _profileManager.GetOrCreateActiveProfile(GetDefaultProfileDisplayName());
            LoadActiveProfile();
            Subscribe();
            // Zenject owns this non-MonoBehaviour service; Dispose removes these subscriptions.
#pragma warning disable UDR0004
            Application.quitting += HandleApplicationQuitting;
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.focusChanged += HandleApplicationFocusChanged;
#endif
#pragma warning restore UDR0004
        }

        public void Tick()
        {
            if (_isApplyingSaveState || _saveBlocked)
                return;

            double now = Time.unscaledTimeAsDouble;
            try
            {
                if (!CompletePendingSave(wait: false) || !HasDirtyDocuments())
                    return;
                if (now < _nextSaveAttemptTime ||
                    (now - _lastDirtyTime < DeferredSaveDelaySeconds && now - _firstDirtyTime < MaxDeferredSaveSeconds))
                    return;

#if UNITY_WEBGL && !UNITY_EDITOR
                SaveDirtyDocuments();
#else
                StartBackgroundSave();
#endif
            }
            catch (Exception exception)
            {
                // Keep the dirty snapshot pending; a full disk must not trigger writes every frame.
                _nextSaveAttemptTime = now + SaveRetryDelaySeconds;
                Debug.LogError($"Unable to save profile. Will retry: {exception.Message}");
            }
        }

        public void Dispose()
        {
            if (!_saveBlocked && !_isApplyingSaveState)
            {
                try { SaveDirtyDocuments(); }
                catch (Exception exception) { Debug.LogError($"Unable to save profile before leaving the scene: {exception.Message}"); }
            }
            Application.quitting -= HandleApplicationQuitting;
#if UNITY_WEBGL && !UNITY_EDITOR
            Application.focusChanged -= HandleApplicationFocusChanged;
#endif
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
            _tutorials.ProgressChanged += HandleTutorialsChanged;
            _unitLevel.OnExpChanged += HandlePlayerChanged;
            _unitLevel.OnLevelUp += HandlePlayerLevelUp;
            _unitLevel.OnSkillPointsChanged += HandleSkillPointsChanged;
            _playerWallet.OnGoldChanged += HandleGoldChanged;
            _enemySpawner.OnLevelChanged += HandleProgressChanged;
            _enemySpawner.OnWaveCleared += HandleWaveCleared;
            _skillTree.OnSkillTreeChanged += HandleSkillTreeChanged;
            _playerInventory.OnInventoryChanged += HandleInventoryChanged;
            _shopService.OnShopPurchasesChanged += HandleShopPurchasesChanged;
        }

        private void Unsubscribe()
        {
            _tutorials.ProgressChanged -= HandleTutorialsChanged;
            _unitLevel.OnExpChanged -= HandlePlayerChanged;
            _unitLevel.OnLevelUp -= HandlePlayerLevelUp;
            _unitLevel.OnSkillPointsChanged -= HandleSkillPointsChanged;
            _playerWallet.OnGoldChanged -= HandleGoldChanged;
            _enemySpawner.OnLevelChanged -= HandleProgressChanged;
            _enemySpawner.OnWaveCleared -= HandleWaveCleared;
            _skillTree.OnSkillTreeChanged -= HandleSkillTreeChanged;
            _playerInventory.OnInventoryChanged -= HandleInventoryChanged;
            _shopService.OnShopPurchasesChanged -= HandleShopPurchasesChanged;
        }

        private void LoadActiveProfile()
        {
            _tutorials.Suspend();
            _isApplyingSaveState = true;
            _saveBlocked = true;
            bool importLegacy = false;
            try
            {
                string path = SavePaths.GetProfileSnapshotFile(_activeProfile.ProfileId);
                if (_storage.TryLoadDocument(path, SaveDocumentType.ProfileSnapshot, SnapshotDocumentVersion,
                        _snapshotMigrations, out ProfileSnapshotSaveData snapshot,
                        data => data.IsCompleteFor(_activeProfile.ProfileId)))
                {
                    if (snapshot.resetRequested)
                    {
                        ApplySnapshot(new ProfileSnapshotSaveData());
                        importLegacy = true; // Commit the game's defaults after a completed reset.
                    }
                    else
                        ApplySnapshot(snapshot);
                }
                else
                {
                    if (_storage.DocumentExists(path))
                        throw new InvalidDataException("No readable profile snapshot or backup. Existing saves will not be overwritten.");

                    // Read and validate every legacy document before changing the live character.
                    var legacy = new ProfileSnapshotSaveData
                    {
                        profileId = _activeProfile.ProfileId,
                        player = LoadLegacy<PlayerSaveData>(SavePaths.GetPlayerFile(_activeProfile.ProfileId), SaveDocumentType.Player, PlayerDocumentVersion, _playerMigrations),
                        progress = LoadLegacy<ProgressSaveData>(SavePaths.GetProgressFile(_activeProfile.ProfileId), SaveDocumentType.Progress, ProgressDocumentVersion, _progressMigrations),
                        skillTree = LoadLegacy<SkillTreeSaveData>(SavePaths.GetSkillTreeFile(_activeProfile.ProfileId), SaveDocumentType.SkillTree, SkillTreeDocumentVersion, _skillTreeMigrations),
                        inventory = LoadLegacy<InventorySaveData>(SavePaths.GetInventoryFile(_activeProfile.ProfileId), SaveDocumentType.Inventory, InventoryDocumentVersion, _inventoryMigrations)
                    };
                    ApplySnapshot(legacy);
                    importLegacy = true;
                }
                ClearDirtyFlags();
                _saveBlocked = false;
            }
            finally
            {
                _isApplyingSaveState = false;
            }

            // Also persists the node-ID migration. Legacy files remain untouched for recovery.
            if (importLegacy)
            {
                try { SaveAllDocuments(); }
                catch (Exception exception)
                {
                    // Gameplay can continue using the imported state; retry the pending snapshot.
                    _nextSaveAttemptTime = Time.unscaledTimeAsDouble + SaveRetryDelaySeconds;
                    Debug.LogError($"Profile loaded, but its new snapshot could not be written. Will retry: {exception.Message}");
                }
            }
        }

        private T LoadLegacy<T>(string path, SaveDocumentType type, int version, SaveMigrationPipeline<T> migrations)
        {
            if (_storage.TryLoadDocument(path, type, version, migrations, out T data)) return data;
            if (_storage.DocumentExists(path))
                throw new InvalidDataException($"No readable legacy {type} document or backup. Existing saves will not be overwritten.");
            return default;
        }

        private void ApplySnapshot(ProfileSnapshotSaveData snapshot)
        {
            IsFreshProfile = snapshot.player == null && snapshot.progress == null &&
                snapshot.skillTree == null && snapshot.inventory == null;
            _tutorials.Suspend();
            if (snapshot.player != null)
            {
                _unitLevel.ApplySaveData(snapshot.player);
                _playerWallet.ApplySaveData(snapshot.player.gold);
            }
            else
            {
                _unitLevel.ResetToDefaults();
                _playerWallet.ResetToDefaults();
            }

            if (snapshot.progress != null)
            {
                _enemySpawner.ApplySaveData(snapshot.progress);
                _shopService.ApplySaveData(snapshot.progress.shopPurchases);
            }
            else
            {
                _enemySpawner.ResetProgressToDefaults();
                _shopService.ResetToDefaults();
            }

            if (snapshot.skillTree != null) _skillTree.ApplySaveData(snapshot.skillTree, RestoreGemInstance);
            else _skillTree.ResetToDefaults(RestoreGemInstance);
            if (snapshot.inventory != null) _playerInventory.ApplySaveData(snapshot.inventory, RestoreGemInstance, RestoreItemDefinition);
            else _playerInventory.ResetToDefaults(RestoreGemInstance, RestoreItemDefinition);
            _tutorials.Restore(snapshot.tutorials);
        }

        private void ApplyDefaultsForFreshProfile()
        {
            IsFreshProfile = true;
            _tutorials.Suspend();
            _isApplyingSaveState = true;
            _saveBlocked = true;
            try
            {
                _unitLevel.ResetToDefaults();
                _playerWallet.ResetToDefaults();
                _enemySpawner.ResetProgressToDefaults();
                _shopService.ResetToDefaults();
                _skillTree.ResetToDefaults(RestoreGemInstance);
                _playerInventory.ResetToDefaults(RestoreGemInstance, RestoreItemDefinition);
                _tutorials.Restore(null);
                ClearDirtyFlags();
                _saveBlocked = false;
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

            return GemInstance.Restore(definition, saveData.instanceId);
        }

        private ItemDefinition RestoreItemDefinition(string definitionId)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                return null;

            if (!_itemDefinitionCatalog.TryResolve(definitionId, out ItemDefinition definition))
            {
                Debug.LogWarning($"Unable to resolve item definition '{definitionId}' during save load.");
                return null;
            }

            return definition;
        }

        private void HandleApplicationQuitting()
        {
            SaveDirtyDocuments();
            _cloudSettingsService.Save();
            _localSettingsService.Save();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private void HandleApplicationFocusChanged(bool hasFocus)
        {
            if (hasFocus)
                return;

            SaveDirtyDocuments();
            _cloudSettingsService.Save();
            _localSettingsService.Save();
        }
#endif

        private void HandlePlayerChanged()
        {
            MarkPlayerDirty();
        }

        private void HandlePlayerLevelUp(int _)
        {
            MarkPlayerDirty();
        }

        private void HandleSkillPointsChanged(float _)
        {
            MarkPlayerDirty();
        }

        private void HandleGoldChanged(int _)
        {
            MarkPlayerDirty();
        }

        private void HandleProgressChanged()
        {
            MarkProgressDirty();
        }

        private void HandleShopPurchasesChanged()
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

            RecordDirtyTime();
            _playerDirty = true;
        }

        private void MarkProgressDirty()
        {
            if (_isApplyingSaveState)
                return;

            RecordDirtyTime();
            _progressDirty = true;
        }

        private void MarkSkillTreeDirty()
        {
            if (_isApplyingSaveState)
                return;

            RecordDirtyTime();
            _skillTreeDirty = true;
        }

        private void MarkInventoryDirty()
        {
            if (_isApplyingSaveState)
                return;

            RecordDirtyTime();
            _inventoryDirty = true;
        }

        private void RecordDirtyTime()
        {
            double now = Time.unscaledTimeAsDouble;
            if (!HasDirtyDocuments()) _firstDirtyTime = now;
            _lastDirtyTime = now;
        }

        private bool HasDirtyDocuments()
        {
            return _playerDirty || _progressDirty || _skillTreeDirty || _inventoryDirty || _tutorialsDirty;
        }

        private void HandleTutorialsChanged()
        {
            if (_isApplyingSaveState) return;
            RecordDirtyTime();
            _tutorialsDirty = true;
        }

        private void SaveAllDocuments()
        {
            RecordDirtyTime();
            _playerDirty = true;
            _progressDirty = true;
            _skillTreeDirty = true;
            _inventoryDirty = true;
            SaveDirtyDocuments();
        }

        private void SaveDirtyDocuments()
        {
            CompletePendingSave(wait: true);
            if (_activeProfile == null)
                return;
            if (_saveBlocked)
                throw new InvalidOperationException("Saving is blocked because the profile did not load successfully.");
            if (_isApplyingSaveState || !HasDirtyDocuments()) return;

            string profileId = _activeProfile.ProfileId;
            ProfileSnapshotSaveData snapshot = CaptureSnapshot(profileId);
            _storage.SaveDocument(SavePaths.GetProfileSnapshotFile(profileId),
                SaveDocumentType.ProfileSnapshot, SnapshotDocumentVersion, snapshot,
                data => data.IsCompleteFor(profileId));
            ClearDirtyFlags();
            TouchSavedProfile(profileId);
        }

        private ProfileSnapshotSaveData CaptureSnapshot(string profileId)
        {
            PlayerSaveData player = _unitLevel.CaptureSaveData();
            player.gold = _playerWallet.Gold;
            ProgressSaveData progress = _enemySpawner.CaptureSaveData();
            progress.shopPurchases = _shopService.CaptureSaveData();
            return new ProfileSnapshotSaveData
            {
                profileId = profileId,
                player = player,
                progress = progress,
                skillTree = _skillTree.CaptureSaveData(),
                inventory = _playerInventory.CaptureSaveData(),
                tutorials = _tutorials.Capture()
            };
        }

        private void StartBackgroundSave()
        {
            if (_activeProfile == null)
                return;
            string profileId = _activeProfile.ProfileId;
            ProfileSnapshotSaveData snapshot = CaptureSnapshot(profileId);
            _pendingSave = _storage.SaveDocumentInBackground(
                SavePaths.GetProfileSnapshotFile(profileId), SaveDocumentType.ProfileSnapshot,
                SnapshotDocumentVersion, snapshot, data => data.IsCompleteFor(profileId));
            _pendingSaveProfileId = profileId;
            // This generation is now owned by _pendingSave. Later events start a new
            // dirty window, which completion of this write must not clear.
            ClearDirtyFlags();
        }

        private bool CompletePendingSave(bool wait)
        {
            if (_pendingSave == null)
                return true;
            if (!wait && !_pendingSave.IsCompleted)
                return false;

            Task pending = _pendingSave;
            string profileId = _pendingSaveProfileId;
            _pendingSave = null;
            _pendingSaveProfileId = null;
            try
            {
                _storage.WaitForPendingWrite();
                // A different storage operation may already have joined the worker.
                pending.GetAwaiter().GetResult();
            }
            catch
            {
                RecordDirtyTime();
                _playerDirty = _progressDirty = _skillTreeDirty = _inventoryDirty = _tutorialsDirty = true;
                throw;
            }
            TouchSavedProfile(profileId);
            return true;
        }

        private void TouchSavedProfile(string profileId)
        {
            try
            {
                _profileManager.TouchProfile(profileId);
            }
            catch (Exception exception)
            {
                // Manifest metadata is not part of character state; the snapshot already committed.
                Debug.LogWarning($"Profile saved, but its manifest could not be updated: {exception.Message}");
            }
        }

        private void ClearDirtyFlags()
        {
            _tutorialsDirty = false;
            _playerDirty = false;
            _progressDirty = false;
            _skillTreeDirty = false;
            _inventoryDirty = false;
            _nextSaveAttemptTime = 0d;
        }

        private static string GetDefaultProfileDisplayName()
        {
            return GameLocalization.Get("save.profile.defaultFirst", "Profile 1");
        }
    }
}
