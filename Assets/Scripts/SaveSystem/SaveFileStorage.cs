using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace SaveSystem
{
    public sealed class SaveFileStorage
    {
        private const string BackupOneSuffix = ".bak1";
        private const string BackupTwoSuffix = ".bak2";
        private const string TempSuffix = ".tmp";

        private readonly SaveFileCodec _codec;
        // Public entry points are called on the main thread. Only the private write
        // core runs on a worker; it must never call back into these entry points.
        private Task _pendingWrite;

        public SaveFileStorage(SaveFileCodec codec)
        {
            _codec = codec;
        }

        public void SaveDocument<T>(string path, SaveDocumentType documentType, int documentVersion, T data, Func<T, bool> validate = null)
        {
            WaitForPendingWrite();
            if (validate != null && !validate(data))
                throw new InvalidDataException("Cannot save an incomplete document.");
            string rawText = _codec.Encode(documentType, documentVersion, data);
            WriteEncodedDocument(path, documentType, documentVersion, rawText, validate);
            WebGLPersistentStorageSync.Flush();
        }

        /// <summary>
        /// Call on the main thread. Serializes data before returning so the worker
        /// never observes live collections. The validator must be thread-safe and
        /// capture immutable values only (for example, the expected profile ID).
        /// </summary>
        public Task SaveDocumentInBackground<T>(string path, SaveDocumentType documentType,
            int documentVersion, T data, Func<T, bool> validate = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            SaveDocument(path, documentType, documentVersion, data, validate);
            return Task.CompletedTask;
#else
            WaitForPendingWrite();
            if (validate != null && !validate(data))
                throw new InvalidDataException("Cannot save an incomplete document.");
            string payloadJson = JsonUtility.ToJson(data);
            _pendingWrite = Task.Run(() =>
            {
                string rawText = _codec.EncodeJson(documentType, documentVersion, payloadJson);
                WriteEncodedDocument(path, documentType, documentVersion, rawText, validate);
            });
            return _pendingWrite;
#endif
        }

        public void WaitForPendingWrite()
        {
            Task pending = _pendingWrite;
            if (pending == null)
                return;
            try { pending.GetAwaiter().GetResult(); }
            finally { _pendingWrite = null; }
        }

        private void WriteEncodedDocument<T>(string path, SaveDocumentType documentType,
            int documentVersion, string rawText, Func<T, bool> validate)
        {
            WriteAllTextWithBackups(path, rawText, candidate =>
            {
                try
                {
                    T previous = _codec.Decode<T>(File.ReadAllText(candidate, Encoding.UTF8), documentType,
                        documentVersion, new SaveMigrationPipeline<T>(Array.Empty<ISaveDataMigration<T>>()));
                    return validate == null || validate(previous);
                }
                catch { return false; }
            });
        }

        public bool TryLoadDocument<T>(
            string path,
            SaveDocumentType documentType,
            int currentDocumentVersion,
            SaveMigrationPipeline<T> migrationPipeline,
            out T data,
            Func<T, bool> validate = null)
        {
            WaitForPendingWrite();
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
                    if (validate != null && !validate(data))
                        throw new InvalidDataException("Save payload is incomplete or belongs to another profile.");
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
            WaitForPendingWrite();
            DeleteBackups(path);
            DeleteIfExists(path);
            WebGLPersistentStorageSync.Flush();
        }

        public void DeleteBackups(string path)
        {
            WaitForPendingWrite();
            DeleteIfExists(path + BackupOneSuffix);
            DeleteIfExists(path + BackupTwoSuffix);
            DeleteIfExists(path + TempSuffix);
            DeleteIfExists(path + BackupOneSuffix + TempSuffix);
            DeleteIfExists(path + BackupTwoSuffix + TempSuffix);
            WebGLPersistentStorageSync.Flush();
        }

        public bool DocumentExists(string path)
        {
            WaitForPendingWrite();
            return File.Exists(path) || File.Exists(path + BackupOneSuffix) || File.Exists(path + BackupTwoSuffix);
        }

        private static void WriteAllTextWithBackups(string path, string rawText, Func<string, bool> isValid)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            string tempPath = path + TempSuffix;
            byte[] bytes = new UTF8Encoding(false).GetBytes(rawText);
            using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
#if UNITY_WEBGL && !UNITY_EDITOR
                stream.Flush();
#else
                stream.Flush(true);
#endif
            }

            // Prepare the entire new document before touching any committed generation.
            // A damaged primary must never displace a good recovery copy.
            if (File.Exists(path) && isValid(path))
                RotateBackups(path, isValid);

            ReplacePreparedFile(tempPath, path);
        }

        private static void RotateBackups(string path, Func<string, bool> isValid)
        {
            string backupOnePath = path + BackupOneSuffix;
            string backupTwoPath = path + BackupTwoSuffix;

            if (File.Exists(backupOnePath) && isValid(backupOnePath))
                CopyCommittedFile(backupOnePath, backupTwoPath);

            if (File.Exists(path))
                CopyCommittedFile(path, backupOnePath);
        }

        private static void CopyCommittedFile(string source, string destination)
        {
            string tempPath = destination + TempSuffix;
            File.Copy(source, tempPath, true);
            using (var stream = new FileStream(tempPath, FileMode.Open, FileAccess.Write, FileShare.None))
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                stream.Flush();
#else
                stream.Flush(true);
#endif
            }
            ReplacePreparedFile(tempPath, destination);
        }

        private static void ReplacePreparedFile(string tempPath, string path)
        {
            if (!File.Exists(path))
            {
                File.Move(tempPath, path);
                return;
            }

#if !UNITY_WEBGL || UNITY_EDITOR
            try
            {
                File.Replace(tempPath, path, null);
                return;
            }
            catch (PlatformNotSupportedException) { }
            catch (NotSupportedException) { }
#endif
            // WebGL's virtual filesystem has no File.Replace. Backups are already committed.
            File.Delete(path);
            File.Move(tempPath, path);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
