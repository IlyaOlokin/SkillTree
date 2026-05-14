using System;
using System.Collections.Generic;
using Gems;
using InventorySystem;
using Items;

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
        public string selectedLocationId;
        public List<LocationProgressSaveData> locations = new();
    }

    [Serializable]
    public class LocationProgressSaveData
    {
        public string locationId;
        public int selectedLevel = 1;
        public int maxUnlockedLevel = 1;
        public int completedLevelCount;
        public List<string> claimedRewardIds = new();
    }

    [Serializable]
    public class SkillTreeSaveData
    {
        public List<string> allocatedNodeIds = new();
        public List<string> independentlyAllocatedNodeIds = new();
        public List<string> allocationQueueNodeIds = new();
        public List<string> discoveredFogNodeIds = new();
        public List<SocketedGemSaveData> socketedGems = new();
        public List<NodePowerSaveData> nodePowers = new();

        public HashSet<string> ToAllocatedNodeSet()
        {
            return allocatedNodeIds != null
                ? new HashSet<string>(allocatedNodeIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        public HashSet<string> ToIndependentlyAllocatedNodeSet()
        {
            return independentlyAllocatedNodeIds != null
                ? new HashSet<string>(independentlyAllocatedNodeIds, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);
        }

        public Dictionary<string, float> ToNodePowerMap()
        {
            Dictionary<string, float> powers = new(StringComparer.Ordinal);
            if (nodePowers == null)
                return powers;

            for (int i = 0; i < nodePowers.Count; i++)
            {
                NodePowerSaveData nodePower = nodePowers[i];
                if (nodePower == null || string.IsNullOrWhiteSpace(nodePower.nodeId))
                    continue;

                powers[nodePower.nodeId] = nodePower.permanentPower;
            }

            return powers;
        }
    }

    [Serializable]
    public class NodePowerSaveData
    {
        public string nodeId;
        public float permanentPower;
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
        public string itemDefinitionId;
        public int stackCount = 1;

        public static InventoryItemSaveData FromInventoryItem(InventoryItem item)
        {
            if (item == null || item.IsEmpty)
                return null;

            return new InventoryItemSaveData
            {
                itemType = item.ItemType,
                gem = item.Gem?.CaptureSaveData(),
                itemDefinitionId = item.ItemDefinition != null ? item.ItemDefinition.SaveDefinitionId : string.Empty,
                stackCount = item.StackCount
            };
        }

        public InventoryItem ToInventoryItem(
            Func<GemInstanceSaveData, GemInstance> gemResolver,
            Func<string, ItemDefinition> itemResolver)
        {
            return itemType switch
            {
                InventoryItemType.Gem when gem != null => InventoryItem.FromGem(gemResolver?.Invoke(gem), stackCount),
                InventoryItemType.Generic when !string.IsNullOrWhiteSpace(itemDefinitionId) =>
                    InventoryItem.FromItemDefinition(itemResolver?.Invoke(itemDefinitionId), stackCount),
                _ => null
            };
        }
    }

    [Serializable]
    public class GemInstanceSaveData
    {
        public string instanceId;
        public string definitionId;
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
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
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
