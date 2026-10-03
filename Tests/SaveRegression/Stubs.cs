// Only the surrounding game/Unity host is substituted. Save codec, storage, models,
// coordinator, profile manager, and node-state migration are linked production sources.
using System;
using System.Collections.Generic;
using System.Text.Json;
using SaveSystem;

namespace UnityEngine
{
    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object value, bool pretty = false) => JsonSerializer.Serialize(value, value.GetType(), Options);
        public static T FromJson<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);
    }
    public static class Application
    {
        public static string persistentDataPath;
        public static event Action quitting;
        public static event Action<bool> focusChanged;
    }
    public static class Time { public static double unscaledTimeAsDouble; }
    public static class Debug
    {
        public static readonly List<string> Errors = new();
        public static void LogWarning(object text) { }
        public static void LogError(object text) => Errors.Add(text.ToString());
    }
    public static class Mathf
    {
        public static int Max(int a, int b) => Math.Max(a,b);
        public static bool Approximately(float a, float b) => Math.Abs(a-b) < 0.00001f;
    }
}
namespace Zenject
{
    public interface IInitializable { void Initialize(); }
    public interface ITickable { void Tick(); }
}
namespace LocalizationSupport
{
    public static class GameLocalization { public static string Get(string key, string fallback) => fallback; }
}
namespace Gems
{
    public class GemDefinition { }
    public class GemInstance
    {
        public string Id;
        public GemInstanceSaveData CaptureSaveData() => new() { instanceId=Id, definitionId="gem" };
        public static GemInstance Restore(GemDefinition definition, string id) => new() { Id=id };
    }
    public class GemDefinitionCatalog
    { public bool TryResolve(string id, out GemDefinition definition) { definition=new(); return true; } }
}
namespace Items
{
    public class ItemDefinition { public string SaveDefinitionId; }
    public class ItemDefinitionCatalog
    { public bool TryResolve(string id, out ItemDefinition definition) { definition=new(); return true; } }
}
namespace InventorySystem
{
    public enum InventoryItemType { Gem, Generic }
    public class InventoryItem
    {
        public bool IsEmpty;
        public InventoryItemType ItemType;
        public Gems.GemInstance Gem;
        public Items.ItemDefinition ItemDefinition;
        public int StackCount;
        public static InventoryItem FromGem(Gems.GemInstance gem, int count) => new() { Gem=gem, StackCount=count };
        public static InventoryItem FromItemDefinition(Items.ItemDefinition item, int count) => new() { ItemDefinition=item, StackCount=count };
    }
    public class PlayerInventory
    {
        public InventorySaveData Data = new();
        public event Action OnInventoryChanged;
        public void Changed() => OnInventoryChanged?.Invoke();
        public InventorySaveData CaptureSaveData() => Data;
        public void ApplySaveData(InventorySaveData data, Func<GemInstanceSaveData,Gems.GemInstance> gems, Func<string,Items.ItemDefinition> items) => Data=data;
        public void ResetToDefaults(Func<GemInstanceSaveData,Gems.GemInstance> gems, Func<string,Items.ItemDefinition> items) => Data=new();
    }
}
namespace Battle
{
    public class UnitLevel
    {
        public PlayerSaveData Data = new();
        public event Action OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnSkillPointsChanged;
        public void Changed() => OnExpChanged?.Invoke();
        public PlayerSaveData CaptureSaveData() => Data;
        public void ApplySaveData(PlayerSaveData data) => Data=data;
        public void ResetToDefaults() => Data=new();
    }
    public class EnemySpawner
    {
        public ProgressSaveData Data = new();
        public event Action OnLevelChanged;
        public event Action OnWaveCleared;
        public ProgressSaveData CaptureSaveData() => Data;
        public void ApplySaveData(ProgressSaveData data) => Data=data;
        public void ResetProgressToDefaults() => Data=new();
    }
}
namespace CurrencySystem
{
    public class PlayerWallet
    {
        public int Gold;
        public event Action<int> OnGoldChanged;
        public void SetGold(int amount) { Gold=amount; OnGoldChanged?.Invoke(amount); }
        public void ApplySaveData(int gold) => Gold=gold;
        public void ResetToDefaults() => Gold=0;
    }
}
namespace ShopSystem
{
    public class ShopService
    {
        public List<ShopPurchaseSaveData> Data = new();
        public event Action OnShopPurchasesChanged;
        public List<ShopPurchaseSaveData> CaptureSaveData() => Data;
        public void ApplySaveData(List<ShopPurchaseSaveData> data) => Data=data;
        public void ResetToDefaults() => Data=new();
    }
}
namespace SkillTree
{
    public class MainSkillTree
    {
        public SkillTreeSaveData Data = new();
        public event Action OnSkillTreeChanged;
        public void Changed() => OnSkillTreeChanged?.Invoke();
        public SkillTreeSaveData CaptureSaveData() => Data;
        public void ApplySaveData(SkillTreeSaveData data, Func<GemInstanceSaveData,Gems.GemInstance> resolver) => Data=data;
        public void ResetToDefaults(Func<GemInstanceSaveData,Gems.GemInstance> resolver) => Data=new();
    }
    public class Node
    {
        public string name="node", ExplicitSaveId;
        public List<string> LegacySaveIds = new();
        public bool IsAllocated, IsIndependentlyAllocated, DefaultIsAllocated;
        public float PermanentPower, DefaultPermanentPower;
        public void SetPermanentPowerFromSave(float value) => PermanentPower=value;
        public void SetAllocatedFromSave(bool allocated, bool independent) { IsAllocated=allocated; IsIndependentlyAllocated=independent; }
    }
    public class SocketNode : Node
    {
        public Gems.GemInstance SocketedGem, DefaultSocketedGem;
        public bool HasGem => SocketedGem != null;
        public void SetSocketedGemFromSave(Gems.GemInstance value) => SocketedGem=value;
    }
    public static class LimitedZone { public static void BeginSaveDataRestore() { } public static void EndSaveDataRestore() { } }
    public class SkillTreeFogOfWarController { public IEnumerable<Node> GetDiscoveredNodes() => Array.Empty<Node>(); }
}
namespace SaveSystem
{
    public class CloudSettingsService { public void Load() { } public void Save() { } }
    public class LocalSettingsService { public void Load() { } public void Save() { } }
}
