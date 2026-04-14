using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace SaveSystem
{
    public sealed class SaveFileStorage
    {
        private const string BackupOneSuffix = ".bak1";
        private const string BackupTwoSuffix = ".bak2";
        private const string TempSuffix = ".tmp";

        private readonly SaveFileCodec _codec;

        public SaveFileStorage(SaveFileCodec codec)
        {
            _codec = codec;
        }

        public void SaveDocument<T>(string path, SaveDocumentType documentType, int documentVersion, T data)
        {
            string rawText = _codec.Encode(documentType, documentVersion, data);
            WriteAllTextWithBackups(path, rawText);
        }

        public bool TryLoadDocument<T>(
            string path,
            SaveDocumentType documentType,
            int currentDocumentVersion,
            SaveMigrationPipeline<T> migrationPipeline,
            out T data)
        {
            string[] candidatePaths =
            {
                path,
                path + BackupOneSuffix,
                path + BackupTwoSuffix
            };

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                string candidatePath = candidatePaths[i];
                if (!File.Exists(candidatePath))
                    continue;

                try
                {
                    string rawText = File.ReadAllText(candidatePath, Encoding.UTF8);
                    data = _codec.Decode(rawText, documentType, currentDocumentVersion, migrationPipeline);
                    if (i > 0)
                        Debug.LogWarning($"Recovered {documentType} from backup file: {candidatePath}");

                    return true;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning($"Failed to load {documentType} from {candidatePath}: {exception.Message}");
                }
            }

            data = default;
            return false;
        }

        public void DeleteFile(string path)
        {
            DeleteIfExists(path);
            DeleteIfExists(path + BackupOneSuffix);
            DeleteIfExists(path + BackupTwoSuffix);
            DeleteIfExists(path + TempSuffix);
        }

        private static void WriteAllTextWithBackups(string path, string rawText)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            RotateBackups(path);

            string tempPath = path + TempSuffix;
            File.WriteAllText(tempPath, rawText, new UTF8Encoding(false));
            if (File.Exists(path))
                File.Delete(path);

            File.Move(tempPath, path);
        }

        private static void RotateBackups(string path)
        {
            string backupOnePath = path + BackupOneSuffix;
            string backupTwoPath = path + BackupTwoSuffix;

            DeleteIfExists(backupTwoPath);
            if (File.Exists(backupOnePath))
                File.Move(backupOnePath, backupTwoPath);

            if (File.Exists(path))
                File.Copy(path, backupOnePath, true);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
