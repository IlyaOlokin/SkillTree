using System.IO;
using UnityEngine;

namespace SaveSystem
{
    public static class SavePaths
    {
        public static string RootDirectory => Path.Combine(Application.persistentDataPath, "Saves");
        public static string ProfilesDirectory => Path.Combine(RootDirectory, "Profiles");
        public static string SettingsDirectory => Path.Combine(RootDirectory, "Settings");
        public static string ProfilesIndexFile => Path.Combine(RootDirectory, "profiles_index.sav");

        public static string GetProfileDirectory(string profileId)
        {
            return Path.Combine(ProfilesDirectory, profileId);
        }

        public static string GetProfileManifestFile(string profileId)
        {
            return Path.Combine(GetProfileDirectory(profileId), "manifest.sav");
        }

        public static string GetPlayerFile(string profileId)
        {
            return Path.Combine(GetProfileDirectory(profileId), "player.sav");
        }

        public static string GetProgressFile(string profileId)
        {
            return Path.Combine(GetProfileDirectory(profileId), "progress.sav");
        }

        public static string GetSkillTreeFile(string profileId)
        {
            return Path.Combine(GetProfileDirectory(profileId), "skilltree.sav");
        }

        public static string GetInventoryFile(string profileId)
        {
            return Path.Combine(GetProfileDirectory(profileId), "inventory.sav");
        }

        public static string GetCloudSettingsFile()
        {
            return Path.Combine(SettingsDirectory, "cloud_settings.sav");
        }

        public static string GetLocalSettingsFile()
        {
            return Path.Combine(SettingsDirectory, "local_settings.sav");
        }
    }
}
