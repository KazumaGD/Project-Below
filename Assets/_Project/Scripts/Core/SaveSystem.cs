using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace ProjectBelow.Core
{
    public enum SaveReadResult { Missing, Primary, Backup, Invalid }

    public sealed class SaveSystem
    {
        [Serializable]
        private sealed class SlotFile
        {
            public string payload;
            public string checksum;
        }

        private readonly string _folder;
        private readonly string _slot;
        private readonly string _backup;

        public SaveSystem() : this(Application.persistentDataPath) { }

        internal SaveSystem(string folder)
        {
            _folder = folder;
            _slot = Path.Combine(folder, "progress.json");
            _backup = _slot + ".bak";
        }

        public SaveReadResult Read(Func<SaveData, bool> validate, out SaveData data)
        {
            if (TryReadFile(_slot, validate, out data))
                return SaveReadResult.Primary;
            if (TryReadFile(_backup, validate, out data))
                return SaveReadResult.Backup;
            return File.Exists(_slot) || File.Exists(_backup) ? SaveReadResult.Invalid : SaveReadResult.Missing;
        }

        public bool TryWrite(SaveData data, Func<SaveData, bool> validate)
        {
            if (data == null || data.version != SaveData.CurrentVersion || !validate(data))
                return false;

            string temporary = _slot + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                string payload = JsonUtility.ToJson(data);
                var contents = new SlotFile { payload = payload, checksum = Hash(payload) };
                byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(contents));
                Directory.CreateDirectory(_folder);
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (!File.Exists(_slot))
                    File.Move(temporary, _slot);
                else
                {
                    // A damaged primary must never replace the last known-good backup.
                    bool keepPrevious = TryReadFile(_slot, validate, out _);
                    File.Replace(temporary, _slot, keepPrevious ? _backup : null);
                }
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                Debug.LogWarning($"Could not write the progress slot: {exception.Message}");
                return false;
            }
            finally
            {
                try
                {
                    if (File.Exists(temporary))
                        File.Delete(temporary);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Could not remove the temporary save: {exception.Message}");
                }
            }
        }

        private static bool TryReadFile(string path, Func<SaveData, bool> validate, out SaveData data)
        {
            data = null;
            try
            {
                var file = JsonUtility.FromJson<SlotFile>(File.ReadAllText(path));
                if (string.IsNullOrEmpty(file?.payload) || file.checksum != Hash(file.payload))
                    return false;

                var candidate = JsonUtility.FromJson<SaveData>(file.payload);
                if (candidate == null || candidate.version != SaveData.CurrentVersion || !validate(candidate))
                    return false;

                data = candidate;
                return true;
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
            {
                return false;
            }
        }

        private static string Hash(string payload)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(payload)));
        }
    }
}
