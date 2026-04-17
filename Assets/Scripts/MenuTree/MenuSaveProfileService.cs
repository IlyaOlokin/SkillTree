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
            if (slotIndex < 0)
                return null;

            var profiles = _profileManager.GetProfiles();
            if (slotIndex < profiles.Count)
            {
                SaveProfileDescriptor profile = profiles[slotIndex];
                return _profileManager.TrySetActiveProfile(profile.ProfileId)
                    ? profile
                    : null;
            }

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
