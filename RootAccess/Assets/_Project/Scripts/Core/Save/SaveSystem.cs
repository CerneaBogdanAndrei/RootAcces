using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace RootAccess.Core
{
    public enum SaveLoadStatus
    {
        Success,
        NotFound,
        Corrupted,
        VersionMismatch,
        IoError
    }

    /// <summary>On-disk wrapper; the payload is the JSON of the actual save model.</summary>
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int version;
        public string savedAtUtc;
        public string payload;
    }

    /// <summary>
    /// Versioned JSON persistence. Writes are atomic (temp file + replace). Older saves are upgraded through
    /// registered migrations; newer or unmigratable saves are reported as <see cref="SaveLoadStatus.VersionMismatch"/>.
    /// </summary>
    public sealed class SaveSystem
    {
        public const int CurrentVersion = 1;
        public const string FileExtension = ".json";
        private const string TempSuffix = ".tmp";
        private const string DefaultFolder = "Saves";

        private readonly string _directory;
        private readonly int _version;
        private readonly Dictionary<int, Func<string, string>> _migrations = new();

        public SaveSystem(string directory, int version = CurrentVersion)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is required.", nameof(directory));
            _directory = directory;
            _version = version;
        }

        public static SaveSystem CreateDefault() => new(Path.Combine(Application.persistentDataPath, DefaultFolder));

        public int Version => _version;
        public string Directory => _directory;

        /// <summary>Registers a payload transform that upgrades data saved with <paramref name="fromVersion"/> to fromVersion + 1.</summary>
        public void RegisterMigration(int fromVersion, Func<string, string> migrate)
        {
            if (migrate == null) throw new ArgumentNullException(nameof(migrate));
            _migrations[fromVersion] = migrate;
        }

        public string GetPath(string slot) => Path.Combine(_directory, slot + FileExtension);

        public bool Exists(string slot) => File.Exists(GetPath(slot));

        public bool Save<T>(string slot, T data)
        {
            if (data == null)
            {
                Debug.LogError($"[SaveSystem] Refusing to save null data to slot '{slot}'.");
                return false;
            }

            try
            {
                System.IO.Directory.CreateDirectory(_directory);
                var envelope = new SaveEnvelope
                {
                    version = _version,
                    savedAtUtc = DateTime.UtcNow.ToString("o"),
                    payload = JsonUtility.ToJson(data)
                };

                string path = GetPath(slot);
                string tempPath = path + TempSuffix;
                File.WriteAllText(tempPath, JsonUtility.ToJson(envelope, true));
                if (File.Exists(path)) File.Delete(path);
                File.Move(tempPath, path);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Failed to save slot '{slot}': {exception.Message}");
                return false;
            }
        }

        public SaveLoadStatus TryLoad<T>(string slot, out T data)
        {
            data = default;
            string path = GetPath(slot);
            if (!File.Exists(path)) return SaveLoadStatus.NotFound;

            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[SaveSystem] Could not read '{path}': {exception.Message}");
                return SaveLoadStatus.IoError;
            }

            SaveEnvelope envelope;
            try
            {
                envelope = JsonUtility.FromJson<SaveEnvelope>(json);
            }
            catch (Exception)
            {
                return SaveLoadStatus.Corrupted;
            }

            if (envelope == null || string.IsNullOrEmpty(envelope.payload)) return SaveLoadStatus.Corrupted;
            if (envelope.version > _version) return SaveLoadStatus.VersionMismatch;

            string payload = envelope.payload;
            for (int version = envelope.version; version < _version; version++)
            {
                if (!_migrations.TryGetValue(version, out Func<string, string> migrate)) return SaveLoadStatus.VersionMismatch;
                payload = migrate(payload);
            }

            try
            {
                data = JsonUtility.FromJson<T>(payload);
            }
            catch (Exception)
            {
                return SaveLoadStatus.Corrupted;
            }

            return data == null ? SaveLoadStatus.Corrupted : SaveLoadStatus.Success;
        }

        public bool Delete(string slot)
        {
            string path = GetPath(slot);
            if (!File.Exists(path)) return false;
            File.Delete(path);
            return true;
        }
    }
}
