using System;

namespace SaveSystem
{
    public sealed class CloudSettingsService
    {
        private const int DocumentVersion = 1;

        private readonly SaveFileStorage _storage;
        private readonly SaveMigrationPipeline<CloudSettingsSaveData> _migrations = new(Array.Empty<ISaveDataMigration<CloudSettingsSaveData>>());

        public CloudSettingsService(SaveFileStorage storage)
        {
            _storage = storage;
            Current = new CloudSettingsSaveData();
        }

        public CloudSettingsSaveData Current { get; private set; }

        public void Load()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetCloudSettingsFile(),
                    SaveDocumentType.CloudSettings,
                    DocumentVersion,
                    _migrations,
                    out CloudSettingsSaveData data))
            {
                Current = data ?? new CloudSettingsSaveData();
            }
        }

        public void Save()
        {
            _storage.SaveDocument(SavePaths.GetCloudSettingsFile(), SaveDocumentType.CloudSettings, DocumentVersion, Current);
        }
    }

    public sealed class LocalSettingsService
    {
        private const int DocumentVersion = 1;

        private readonly SaveFileStorage _storage;
        private readonly SaveMigrationPipeline<LocalSettingsSaveData> _migrations = new(Array.Empty<ISaveDataMigration<LocalSettingsSaveData>>());

        public LocalSettingsService(SaveFileStorage storage)
        {
            _storage = storage;
            Current = new LocalSettingsSaveData();
        }

        public LocalSettingsSaveData Current { get; private set; }

        public void Load()
        {
            if (_storage.TryLoadDocument(
                    SavePaths.GetLocalSettingsFile(),
                    SaveDocumentType.LocalSettings,
                    DocumentVersion,
                    _migrations,
                    out LocalSettingsSaveData data))
            {
                Current = data ?? new LocalSettingsSaveData();
            }
        }

        public void Save()
        {
            _storage.SaveDocument(SavePaths.GetLocalSettingsFile(), SaveDocumentType.LocalSettings, DocumentVersion, Current);
        }
    }
}
