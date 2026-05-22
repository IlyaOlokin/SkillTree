using LocalizationSupport;
using SaveSystem;

namespace MenuTree
{
    public sealed class MenuSaveProfileService
    {
        private readonly SaveProfileManager _profileManager;

        public MenuSaveProfileService()
        {
            SaveFileCodec codec = new();
            SaveFileStorage storage = new(codec);
            _profileManager = new SaveProfileManager(storage);
        }

        public SaveProfileDescriptor ActivateOrCreateProfileAtSlot(int slotIndex, bool createIfMissing, string displayNameKeyOrText = null)
        {
            SaveProfileDescriptor profile = GetOrCreateProfileAtSlot(slotIndex, createIfMissing, displayNameKeyOrText);
            if (profile == null)
                return null;

            return _profileManager.TrySetActiveProfile(profile.ProfileId)
                ? profile
                : null;
        }

        public SaveProfileDescriptor ClearProfileAtSlot(int slotIndex, bool createIfMissing, string displayNameKeyOrText = null)
        {
            SaveProfileDescriptor profile = GetOrCreateProfileAtSlot(slotIndex, createIfMissing, displayNameKeyOrText);
            if (profile == null)
                return null;

            _profileManager.ClearProfileSaveData(profile.ProfileId);
            _profileManager.TouchProfile(profile.ProfileId);
            return profile;
        }

        private SaveProfileDescriptor GetOrCreateProfileAtSlot(int slotIndex, bool createIfMissing, string displayNameKeyOrText)
        {
            if (slotIndex < 0)
                return null;

            var profiles = _profileManager.GetProfiles();
            if (slotIndex < profiles.Count)
                return profiles[slotIndex];

            if (!createIfMissing)
                return null;

            SaveProfileDescriptor createdProfile = null;
            for (int i = profiles.Count; i <= slotIndex; i++)
            {
                createdProfile = _profileManager.CreateProfile(GetProfileDisplayName(i, displayNameKeyOrText), true);
            }

            return createdProfile;
        }

        private static string GetProfileDisplayName(int slotIndex, string displayNameKeyOrText)
        {
            string displayPrefix = string.IsNullOrWhiteSpace(displayNameKeyOrText)
                ? GameLocalization.Get("save.profile.defaultName", "Profile")
                : GameLocalization.LocalizeMainMenuValueOrKey(displayNameKeyOrText);
            return $"{displayPrefix} {slotIndex + 1}";
        }
    }
}
