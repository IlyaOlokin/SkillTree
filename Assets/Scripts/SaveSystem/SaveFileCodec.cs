using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace SaveSystem
{
    public sealed class SaveFileCodec
    {
        private static readonly byte[] ObfuscationKey = Encoding.UTF8.GetBytes("SkillTree.Save.ObfuscationKey");

        public string Encode<T>(SaveDocumentType documentType, int documentVersion, T data)
        {
            string payloadJson = JsonUtility.ToJson(data);
            byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
            byte[] compressedBytes = Compress(payloadBytes);
            byte[] encodedBytes = Xor(compressedBytes);

            SaveEnvelope envelope = new SaveEnvelope
            {
                documentType = documentType.ToString(),
                documentVersion = documentVersion,
                transactionId = Guid.NewGuid().ToString("N"),
                createdAtUtc = DateTime.UtcNow.ToString("O"),
                payloadHash = ComputeHash(payloadBytes),
                payload = Convert.ToBase64String(encodedBytes)
            };

            return JsonUtility.ToJson(envelope, true);
        }

        public T Decode<T>(
            string rawText,
            SaveDocumentType expectedDocumentType,
            int currentDocumentVersion,
            SaveMigrationPipeline<T> migrationPipeline)
        {
            SaveEnvelope envelope = JsonUtility.FromJson<SaveEnvelope>(rawText);
            if (envelope == null)
                throw new InvalidDataException("Save envelope is empty.");

            if (!string.Equals(envelope.magic, SaveEnvelope.Magic, StringComparison.Ordinal))
                throw new InvalidDataException("Unknown save file format.");

            if (!string.Equals(envelope.documentType, expectedDocumentType.ToString(), StringComparison.Ordinal))
                throw new InvalidDataException($"Expected {expectedDocumentType} document, got {envelope.documentType}.");

            byte[] encodedBytes = Convert.FromBase64String(envelope.payload ?? string.Empty);
            byte[] compressedBytes = Xor(encodedBytes);
            byte[] payloadBytes = Decompress(compressedBytes);

            string actualHash = ComputeHash(payloadBytes);
            if (!string.Equals(actualHash, envelope.payloadHash, StringComparison.Ordinal))
                throw new InvalidDataException("Save checksum mismatch.");

            string payloadJson = Encoding.UTF8.GetString(payloadBytes);
            T data = JsonUtility.FromJson<T>(payloadJson);
            if (data == null)
                throw new InvalidDataException("Save payload is empty.");

            int sourceVersion = Mathf.Max(1, envelope.documentVersion);
            if (sourceVersion != currentDocumentVersion)
                data = migrationPipeline.Migrate(data, sourceVersion, currentDocumentVersion);

            return data;
        }

        private static byte[] Compress(byte[] payloadBytes)
        {
            using MemoryStream output = new();
            using (GZipStream gzip = new(output, System.IO.Compression.CompressionLevel.Optimal, true))
            {
                gzip.Write(payloadBytes, 0, payloadBytes.Length);
            }

            return output.ToArray();
        }

        private static byte[] Decompress(byte[] payloadBytes)
        {
            using MemoryStream input = new(payloadBytes);
            using GZipStream gzip = new(input, CompressionMode.Decompress);
            using MemoryStream output = new();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static byte[] Xor(byte[] input)
        {
            byte[] result = new byte[input.Length];
            for (int i = 0; i < input.Length; i++)
                result[i] = (byte)(input[i] ^ ObfuscationKey[i % ObfuscationKey.Length]);

            return result;
        }

        private static string ComputeHash(byte[] payloadBytes)
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(payloadBytes);
            StringBuilder builder = new(hashBytes.Length * 2);
            for (int i = 0; i < hashBytes.Length; i++)
                builder.Append(hashBytes[i].ToString("x2"));

            return builder.ToString();
        }
    }
}
