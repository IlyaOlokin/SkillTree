using System;
using System.Collections.Generic;
using System.IO;
using LocalizationSupport;

namespace SaveSystem
{
    public sealed class SaveProfileManager
    {
        private const int ProfilesIndexVersion = 1;
        private const int ProfileManifestVersion = 1;

        private readonly SaveFileStorage _storage;
        private readonly SaveMigrationPipeline<SaveProfilesIndexData> _profilesIndexMigrations = new(Array.Empty<ISaveDataMigration<SaveProfilesIndexData>>());
        private readonly SaveMigrationPipeline<ProfileManifestData> _profileManifestMigrations = new(Array.Empty<ISaveDataMigration<ProfileManifestData>>());

        public SaveProfileManager(SaveFileStorage storage)
        {
            _storage = storage;
        }

        public SaveProfileDescriptor GetOrCreateActiveProfile(string defaultDisplayName)
        {
            SaveProfilesIndexData indexData = LoadIndex();
            if (!string.IsNullOrWhiteSpace(indexData.activeProfileId))
            {
                SaveProfileDescriptor activeProfile = TryLoadProfile(indexData.activeProfileId);
                if (activeProfile != null)
                    return activeProfile;
            }

            if (indexData.profiles.Count > 0)
            {
                SaveProfileDescriptor firstProfile = TryLoadProfile(indexData.profiles[0].profileId);
                if (firstProfile != null)
                {
                    indexData.activeProfileId = firstProfile.ProfileId;
                    SaveIndex(indexData);
                    return firstProfile;
                }
            }

            return CreateProfile(defaultDisplayName, true);
        }

        public IReadOnlyList<SaveProfileDescriptor> GetProfiles()
        {
            SaveProfilesIndexData indexData = LoadIndex();
            List<SaveProfileDescriptor> profiles = new(indexData.profiles.Count);
            for (int i = 0; i < indexData.profiles.Count; i++)
            {
                SaveProfileDescriptor profile = TryLoadProfile(indexData.profiles[i].profileId);
                if (profile != null)
                    profiles.Add(profile);
            }

            return profiles;
        }

        public SaveProfileDescriptor CreateProfile(string displayName, bool makeActive)
        {
            string profileId = Guid.NewGuid().ToString("N");
            string utcNow = DateTime.UtcNow.ToString("O");
            string sanitizedDisplayName = string.IsNullOrWhiteSpace(displayName)
                ? GameLocalization.Get("save.profile.defaultName", "Profile")
                : displayName.Trim();

            ProfileManifestData manifestData = new()
            {
                profileId = profileId,
                displayName = sanitizedDisplayName,
                createdAtUtc = utcNow,
                lastSavedAtUtc = utcNow,
                saveCount = 0
            };

            Directory.CreateDirectory(SavePaths.GetProfileDirectory(profileId));
            _storage.SaveDocument(SavePaths.GetProfileManifestFile(profileId), SaveDocumentType.ProfileManifest, ProfileManifestVersion, manifestData);

            SaveProfilesIndexData indexData = LoadIndex();
            indexData.profiles.RemoveAll(p => p.profileId == profileId);
            indexData.profiles.Add(new SaveProfileListEntryData
            {
                profileId = profileId,
                displayName = sanitizedDisplayName
            });

            if (makeActive || string.IsNullOrWhiteSpace(indexData.activeProfileId))
                indexData.activeProfileId = profileId;

            SaveIndex(indexData);
            return ToDescriptor(manifestData);
        }

        public bool TrySetActiveProfile(string profileId)
        {
            if (TryLoadProfile(profileId) == null)
                return false;

            SaveProfilesIndexData indexData = LoadIndex();
            indexData.activeProfileId = profileId;
            SaveIndex(indexData);
            return true;
        }

        public void TouchProfile(string profileId)
        {
            ProfileManifestData manifestData = LoadManifest(profileId);
            if (manifestData == null)
                return;

            manifestData.lastSavedAtUtc = DateTime.UtcNow.ToString("O");
            manifestData.saveCount++;
            _storage.SaveDocument(SavePaths.GetProfileManifestFile(profileId), SaveDocumentType.ProfileManifest, ProfileManifestVersion, manifestData);
        }

        public SaveProfileDescriptor ClearActiveProfileSaveData(string defaultDisplayName)
        {
            SaveProfileDescriptor activeProfile = GetOrCreateActiveProfile(defaultDisplayName);
            ClearProfileSaveData(activeProfile.ProfileId);
            TouchProfile(activeProfile.ProfileId);
            return TryLoadProfile(activeProfile.ProfileId) ?? activeProfile;
        }

        public void ClearProfileSaveData(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return;

            _storage.DeleteFile(SavePaths.GetPlayerFile(profileId));
            _storage.DeleteFile(SavePaths.GetProgressFile(profileId));
            _storage.DeleteFile(SavePaths.GetSkillTreeFile(profileId));
            _storage.DeleteFile(SavePaths.GetInventoryFile(profileId));
        }

        private SaveProfilesIndexData LoadIndex()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.ProfilesIndexFile,
                    SaveDocumentType.ProfilesIndex,
                    ProfilesIndexVersion,
                    _profilesIndexMigrations,
                    out SaveProfilesIndexData indexData))
            {
                indexData.profiles ??= new List<SaveProfileListEntryData>();
                return indexData;
            }

            return new SaveProfilesIndexData();
        }

        private void SaveIndex(SaveProfilesIndexData indexData)
        {
            indexData.profiles ??= new List<SaveProfileListEntryData>();
            _storage.SaveDocument(SavePaths.ProfilesIndexFile, SaveDocumentType.ProfilesIndex, ProfilesIndexVersion, indexData);
        }

        private SaveProfileDescriptor TryLoadProfile(string profileId)
        {
            ProfileManifestData manifestData = LoadManifest(profileId);
            return manifestData != null ? ToDescriptor(manifestData) : null;
        }

        private ProfileManifestData LoadManifest(string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
                return null;

            if (_storage.TryLoadDocument(
                    SavePaths.GetProfileManifestFile(profileId),
                    SaveDocumentType.ProfileManifest,
                    ProfileManifestVersion,
                    _profileManifestMigrations,
                    out ProfileManifestData manifestData))
            {
                return manifestData;
            }

            return null;
        }

        private static SaveProfileDescriptor ToDescriptor(ProfileManifestData manifestData)
        {
            return new SaveProfileDescriptor(
                manifestData.profileId,
                manifestData.displayName,
                manifestData.createdAtUtc,
                manifestData.lastSavedAtUtc,
                manifestData.saveCount);
        }
    }
}
