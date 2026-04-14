using System;
using System.Collections.Generic;

namespace SaveSystem
{
    public interface ISaveDataMigration<T>
    {
        int FromVersion { get; }
        int ToVersion { get; }
        T Migrate(T data);
    }

    public sealed class SaveMigrationPipeline<T>
    {
        private readonly Dictionary<int, ISaveDataMigration<T>> _migrations;

        public SaveMigrationPipeline(IEnumerable<ISaveDataMigration<T>> migrations)
        {
            _migrations = new Dictionary<int, ISaveDataMigration<T>>();
            if (migrations == null)
                return;

            foreach (ISaveDataMigration<T> migration in migrations)
            {
                if (migration == null)
                    continue;

                _migrations[migration.FromVersion] = migration;
            }
        }

        public T Migrate(T data, int sourceVersion, int targetVersion)
        {
            if (sourceVersion > targetVersion)
                throw new InvalidOperationException($"Cannot migrate {typeof(T).Name} from future version {sourceVersion} to {targetVersion}.");

            int currentVersion = sourceVersion;
            T currentData = data;

            while (currentVersion < targetVersion)
            {
                if (!_migrations.TryGetValue(currentVersion, out ISaveDataMigration<T> migration))
                    throw new InvalidOperationException($"Missing migration for {typeof(T).Name} from version {currentVersion}.");

                currentData = migration.Migrate(currentData);
                currentVersion = migration.ToVersion;
            }

            return currentData;
        }
    }
}
