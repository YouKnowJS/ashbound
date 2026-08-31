using System;
using System.IO;
using UnityEngine;

namespace Ashbound
{
    public sealed class MetaProgressionStore
    {
        public string SavePath { get; }
        public string LastError { get; private set; }
        public MetaProgressionStore(string savePath = null)
        {
            SavePath = string.IsNullOrWhiteSpace(savePath) ? Path.Combine(Application.persistentDataPath, "Profile", "meta-profile.json") : savePath;
        }
        public MetaProgressionProfile LoadOrCreate()
        {
            LastError = null;
            if (!File.Exists(SavePath)) return MetaProgressionProfile.CreateDefault();
            try
            {
                string json = File.ReadAllText(SavePath);
                var profile = JsonUtility.FromJson<MetaProgressionProfile>(json);
                if (profile == null || profile.schemaVersion <= 0 || string.IsNullOrWhiteSpace(profile.profileId)) throw new InvalidDataException("Profile header is invalid.");
                profile.Normalize(); return profile;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException || exception is InvalidDataException)
            {
                LastError = exception.Message;
                try { File.Move(SavePath, SavePath + ".invalid-" + DateTime.UtcNow.Ticks); } catch (Exception) { }
                return MetaProgressionProfile.CreateDefault();
            }
        }
        public bool Save(MetaProgressionProfile profile)
        {
            LastError = null;
            try
            {
                profile.Normalize();
                string directory = Path.GetDirectoryName(SavePath); if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
                string temporary = SavePath + ".tmp"; File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
                if (File.Exists(SavePath)) File.Delete(SavePath); File.Move(temporary, SavePath); return true;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            { LastError = exception.Message; return false; }
        }
        public MetaProgressionProfile Reset()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp"); } catch (Exception exception) { LastError=exception.Message; }
            var profile = MetaProgressionProfile.CreateDefault(); Save(profile); return profile;
        }
    }
}
