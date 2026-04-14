using System;
using System.Collections.Generic;
using Gems;
using InventorySystem;

namespace SaveSystem
{
    [Serializable]
    public class SaveProfilesIndexData
    {
        public string activeProfileId;
        public List<SaveProfileListEntryData> profiles = new();
    }

    [Serializable]
    public class SaveProfileListEntryData
    {
        public string profileId;
        public string displayName;
    }

    [Serializable]
    public class ProfileManifestData
    {
        public string profileId;
        public string displayName;
        public string createdAtUtc;
        public string lastSavedAtUtc;
        public int saveCount;
    }

    [Serializable]
    public class PlayerSaveData
    {
        public int level = 1;
        public double currentExp;
        public int skillPoints = 1;
    }

    [Serializable]
    public class ProgressSaveData
    {
        public int selectedLevel = 1;
        public int maxUnlockedLevel = 1;
        public int currentClearedWaves;
    }

    [Serializable]
    public class SkillTreeSaveData
    {
        public List<string> allocatedNodeIds = new();
        public List<SocketedGemSaveData> socketedGems = new();

        public HashSet<string> ToAllocatedNodeSet()
        {
            return allocatedNodeIds != null
                ? new HashSet<string>(allocatedNodeIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }
    }

    [Serializable]
    public class SocketedGemSaveData
    {
        public string socketNodeId;
        public GemInstanceSaveData gem;
    }

    [Serializable]
    public class InventorySaveData
    {
        public int slotCount;
        public List<InventorySlotSaveData> slots = new();
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public int slotIndex;
        public InventoryItemSaveData item;
    }

    [Serializable]
    public class InventoryItemSaveData
    {
        public InventoryItemType itemType;
        public GemInstanceSaveData gem;

        public static InventoryItemSaveData FromInventoryItem(InventoryItem item)
        {
            if (item == null || item.IsEmpty)
                return null;

            return new InventoryItemSaveData
            {
                itemType = item.ItemType,
                gem = item.Gem?.CaptureSaveData()
            };
        }

        public InventoryItem ToInventoryItem(Func<GemInstanceSaveData, GemInstance> gemResolver)
        {
            return itemType switch
            {
                InventoryItemType.Gem when gem != null => InventoryItem.FromGem(gemResolver?.Invoke(gem)),
                _ => null
            };
        }
    }

    [Serializable]
    public class GemInstanceSaveData
    {
        public string instanceId;
        public string definitionId;
        public List<float> rolledValues = new();
    }

    [Serializable]
    public class CloudSettingsSaveData
    {
        public string languageCode = "en";
    }

    [Serializable]
    public class LocalSettingsSaveData
    {
        public float masterVolume = 1f;
        public string qualityPreset = string.Empty;
        public int resolutionWidth;
        public int resolutionHeight;
        public bool fullscreen = true;
    }

    public sealed class SaveProfileDescriptor
    {
        public SaveProfileDescriptor(string profileId, string displayName, string createdAtUtc, string lastSavedAtUtc, int saveCount)
        {
            ProfileId = profileId;
            DisplayName = displayName;
            CreatedAtUtc = createdAtUtc;
            LastSavedAtUtc = lastSavedAtUtc;
            SaveCount = saveCount;
        }

        public string ProfileId { get; }
        public string DisplayName { get; }
        public string CreatedAtUtc { get; }
        public string LastSavedAtUtc { get; }
        public int SaveCount { get; }
    }
}
