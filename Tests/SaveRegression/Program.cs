using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Battle;
using CurrencySystem;
using InventorySystem;
using SaveSystem;
using SkillTree;

internal static class Program
{
    private static int _tests;
    private static readonly SaveFileCodec Codec = new();
    private static readonly SaveFileStorage Storage = new(Codec);
    private static readonly SaveMigrationPipeline<ProfileSnapshotSaveData> Migrations = new(Array.Empty<ISaveDataMigration<ProfileSnapshotSaveData>>());
    private static string Root;
    private static void Main(string[] args)
    {
        Root=args[0]; Directory.CreateDirectory(Root);
        Run("complete snapshots and whole-generation recovery", SnapshotRecovery);
        Run("failed write preserves committed files and ignores temp", FailedWrite);
        Run("corrupt primary does not evict good backups", CorruptPrimary);
        Run("incomplete/wrong-profile snapshots rejected", InvalidSnapshot);
        Run("all node state migrates and survives hierarchy changes", NodeMigration);
        Run("duplicate/missing node IDs reject capture", InvalidNodeIds);
        Run("legacy import and authoritative snapshot", LegacyImport);
        Run("unreadable saves cannot be overwritten", UnreadableProfile);
        Run("bounded autosave, debounce, and retry after IO failure", Autosave);
        Run("gem transfer committed with both inventories", GemTransfer);
        Run("clear profile commits reset and removes old backups", ClearProfile);
        Run("interrupted legacy cleanup keeps reset authoritative", InterruptedClear);
        Run("damaged profile metadata cannot silently replace profiles", ProfileMetadata);
        Console.WriteLine($"PASS: {_tests} save regression groups");
    }
    private static void Run(string name, Action test) { test(); _tests++; Console.WriteLine("PASS: "+name); }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void Throws(Action action) { try { action(); } catch { return; } throw new Exception("Expected failure"); }
    private static string PathFor(string name) => Path.Combine(Root,name);
    private static ProfileSnapshotSaveData Snapshot(int generation) => new()
    {
        profileId="test", player=new() { gold=generation }, progress=new() { selectedLocationId=generation.ToString() },
        skillTree=new() { allocatedNodeIds=new() { "node-"+generation } }, inventory=new() { slotCount=generation }
    };
    private static void Save(string path, int generation) => Storage.SaveDocument(path,SaveDocumentType.ProfileSnapshot,1,Snapshot(generation),s=>s.IsCompleteFor("test"));
    private static ProfileSnapshotSaveData Load(string path, string id="test")
    {
        Check(Storage.TryLoadDocument(path,SaveDocumentType.ProfileSnapshot,1,Migrations,out ProfileSnapshotSaveData data,s=>s.IsCompleteFor(id)),"Load failed");
        return data;
    }
    private static void Generation(ProfileSnapshotSaveData data, int expected)
    {
        Check(data.player.gold==expected && data.inventory.slotCount==expected && data.progress.selectedLocationId==expected.ToString()
            && data.skillTree.allocatedNodeIds.Single()=="node-"+expected,"Mixed save generations");
    }
    private static void SnapshotRecovery()
    {
        string path=PathFor("recovery.sav"); Save(path,1); Save(path,2); Save(path,3); Generation(Load(path),3);
        File.WriteAllText(path,"broken"); Generation(Load(path),2);
        File.WriteAllText(path+".bak1","broken"); Generation(Load(path),1);
    }
    private static void FailedWrite()
    {
        string path=PathFor("failure.sav"); Save(path,1); Save(path,2);
        string main=File.ReadAllText(path), backup=File.ReadAllText(path+".bak1");
        using (new FileStream(path+".tmp",FileMode.Create,FileAccess.ReadWrite,FileShare.None)) Throws(()=>Save(path,3));
        Check(main==File.ReadAllText(path) && backup==File.ReadAllText(path+".bak1"),"Failed preparation modified committed state");
        File.WriteAllText(path+".tmp",Codec.Encode(SaveDocumentType.ProfileSnapshot,1,Snapshot(99)));
        Generation(Load(path),2);
        File.Delete(path); Generation(Load(path),1);
    }
    private static void CorruptPrimary()
    {
        string path=PathFor("corrupt.sav"); Save(path,1); Save(path,2);
        File.WriteAllText(path,"broken"); Save(path,3); File.WriteAllText(path,"broken again"); Generation(Load(path),1);
    }
    private static void InvalidSnapshot()
    {
        string path=PathFor("invalid.sav"); Save(path,1); Save(path,2);
        var incomplete=Snapshot(3); incomplete.inventory=null;
        Throws(()=>Storage.SaveDocument(path,SaveDocumentType.ProfileSnapshot,1,incomplete,s=>s.IsCompleteFor("test")));
        File.WriteAllText(path,Codec.Encode(SaveDocumentType.ProfileSnapshot,1,incomplete)); Generation(Load(path),1);
        var wrong=Snapshot(3); wrong.profileId="other";
        File.WriteAllText(path,Codec.Encode(SaveDocumentType.ProfileSnapshot,1,wrong)); Generation(Load(path),1);
    }
    private static void NodeMigration()
    {
        var node=new SocketNode { ExplicitSaveId="stable", name="renamed and reparented", LegacySaveIds=new() { "scene:old[0]/node[2]" } };
        string old=node.LegacySaveIds[0];
        var data=new SkillTreeSaveData
        {
            allocatedNodeIds=new() { old }, independentlyAllocatedNodeIds=new() { old }, allocationQueueNodeIds=new() { old }, discoveredFogNodeIds=new() { old },
            nodePowers=new() { new() { nodeId=old, permanentPower=0.4f } },
            socketedGems=new() { new() { socketNodeId=old, gem=new() { instanceId="unique",definitionId="gem" } } }
        };
        var service=new SkillTreeSaveService();
        service.ApplyNodeState(data,new[] {node}, g=>new Gems.GemInstance {Id=g.instanceId},out var lookup,out var ids);
        Check(node.IsAllocated && node.IsIndependentlyAllocated && node.PermanentPower==0.4f && node.SocketedGem.Id=="unique","State lost in migration");
        Check(data.allocationQueueNodeIds.Single()=="stable" && data.discoveredFogNodeIds.Single()=="stable","Queue or fog not migrated");
        var captured=service.Capture(new[] {node},new[] {node},null);
        Check(captured.allocatedNodeIds.Single()=="stable" && captured.socketedGems.Single().socketNodeId=="stable","Legacy IDs written again");
        service.ApplyNodeState(data,new[] {node},g=>new Gems.GemInstance {Id=g.instanceId},out lookup,out ids);
        Check(node.SocketedGem.Id=="unique","Migration not idempotent");
    }
    private static void InvalidNodeIds()
    {
        var service=new SkillTreeSaveService();
        Throws(()=>service.Capture(new[] {new Node()},null,null));
        Throws(()=>service.Capture(new[] {new Node {ExplicitSaveId="same"},new Node {ExplicitSaveId="same"}},null,null));
        var nodes=new[] {new Node {ExplicitSaveId="one",LegacySaveIds=new(){"alias"}},new Node {ExplicitSaveId="two",LegacySaveIds=new(){"alias"}}};
        Throws(()=>service.ApplyNodeState(new(),nodes,null,out _,out _));
    }
    private sealed class Host : IDisposable
    {
        public readonly UnitLevel Level=new(); public readonly PlayerWallet Wallet=new();
        public readonly EnemySpawner Progress=new(); public readonly MainSkillTree Tree=new(); public readonly PlayerInventory Inventory=new();
        public readonly SaveProfileManager Profiles=new(Storage); public readonly GameSaveCoordinator Coordinator;
        public Host(string name)
        {
            UnityEngine.Application.persistentDataPath=PathFor(name); UnityEngine.Time.unscaledTimeAsDouble=0;
            Coordinator=new(Level,Wallet,Progress,Tree,Inventory,new ShopSystem.ShopService(),Storage,Profiles,
                new Gems.GemDefinitionCatalog(),new Items.ItemDefinitionCatalog(),new CloudSettingsService(),new LocalSettingsService());
        }
        public string Path => SavePaths.GetProfileSnapshotFile(Coordinator.ActiveProfile.ProfileId);
        public ProfileSnapshotSaveData Read() => Load(Path,Coordinator.ActiveProfile.ProfileId);
        public void Dispose() => Coordinator.Dispose();
    }
    private static void LegacyImport()
    {
        using var h=new Host("legacy"); string id=h.Profiles.GetOrCreateActiveProfile("old").ProfileId;
        string legacy=SavePaths.GetPlayerFile(id);
        Storage.SaveDocument(legacy,SaveDocumentType.Player,1,new PlayerSaveData {gold=25,skillPoints=7});
        Storage.SaveDocument(SavePaths.GetSkillTreeFile(id),SaveDocumentType.SkillTree,1,new SkillTreeSaveData {allocatedNodeIds=new(){"node"}});
        string before=File.ReadAllText(legacy); h.Coordinator.Initialize();
        Check(h.Read().player.gold==25 && h.Read().player.skillPoints==7 && File.ReadAllText(legacy)==before,"Legacy import altered or lost data");
        h.Wallet.SetGold(50); h.Coordinator.SaveNow();
        using var reopened=new Host("legacy"); reopened.Coordinator.Initialize();
        Check(reopened.Wallet.Gold==50,"Loaded stale legacy state");
    }
    private static void UnreadableProfile()
    {
        using var h=new Host("unreadable"); var id=h.Profiles.GetOrCreateActiveProfile("old").ProfileId;
        string path=SavePaths.GetProfileSnapshotFile(id); File.WriteAllText(path,"bad snapshot");
        Storage.SaveDocument(SavePaths.GetPlayerFile(id),SaveDocumentType.Player,1,new PlayerSaveData {gold=500});
        Throws(h.Coordinator.Initialize); Throws(h.Coordinator.SaveNow);
        Check(File.ReadAllText(path)=="bad snapshot","Unreadable snapshot overwritten");
        using var legacy=new Host("unreadable-legacy"); id=legacy.Profiles.GetOrCreateActiveProfile("old").ProfileId;
        File.WriteAllText(SavePaths.GetPlayerFile(id),"bad legacy"); Throws(legacy.Coordinator.Initialize);
        Check(!Storage.DocumentExists(SavePaths.GetProfileSnapshotFile(id)),"Corrupt legacy was imported as defaults");
    }
    private static void Autosave()
    {
        using var h=new Host("autosave"); h.Coordinator.Initialize();
        for(int i=0;i<=50;i++) { UnityEngine.Time.unscaledTimeAsDouble=i*0.1; h.Wallet.SetGold(i+1); h.Coordinator.Tick(); }
        Check(h.Read().player.gold==51,"Continuous changes postponed save beyond 5s");
        UnityEngine.Time.unscaledTimeAsDouble=6; h.Wallet.SetGold(60);
        UnityEngine.Time.unscaledTimeAsDouble=6.74; h.Coordinator.Tick(); Check(h.Read().player.gold==51,"Debounce ignored");
        UnityEngine.Time.unscaledTimeAsDouble=6.75; h.Coordinator.Tick(); Check(h.Read().player.gold==60,"Debounced save missed");
        h.Wallet.SetGold(70);
        using(new FileStream(h.Path+".tmp",FileMode.Create,FileAccess.ReadWrite,FileShare.None))
        { UnityEngine.Time.unscaledTimeAsDouble=8; h.Coordinator.Tick(); }
        Check(h.Read().player.gold==60,"Failed save replaced old data");
        UnityEngine.Time.unscaledTimeAsDouble=13; h.Coordinator.Tick(); Check(h.Read().player.gold==70,"Dirty flags lost after failure");
    }
    private static void GemTransfer()
    {
        using var h=new Host("gem-transfer"); h.Coordinator.Initialize();
        var gem=new GemInstanceSaveData {instanceId="only-one",definitionId="gem"};
        h.Inventory.Data.slots.Add(new(){slotIndex=0,item=new(){itemType=InventoryItemType.Gem,gem=gem}});
        h.Inventory.Changed(); h.Coordinator.SaveNow();
        h.Inventory.Data.slots.Clear(); h.Tree.Data.socketedGems.Add(new(){socketNodeId="socket",gem=gem});
        // One dirty event is enough: the snapshot must capture all four systems.
        h.Tree.Changed(); h.Coordinator.SaveNow();
        Check(h.Read().inventory.slots.Count==0 && h.Read().skillTree.socketedGems.Count==1,"Transfer not atomic");
        File.WriteAllText(h.Path,"broken"); var recovered=h.Read();
        Check(recovered.inventory.slots.Count==1 && recovered.skillTree.socketedGems.Count==0,"Recovery duplicated/lost gem");
    }
    private static void ClearProfile()
    {
        using var h=new Host("clear"); h.Coordinator.Initialize(); h.Coordinator.SaveNow(); h.Coordinator.SaveNow();
        string id=h.Coordinator.ActiveProfile.ProfileId;
        Storage.SaveDocument(SavePaths.GetPlayerFile(id),SaveDocumentType.Player,1,new PlayerSaveData());
        h.Profiles.ClearProfileSaveData(id);
        Check(h.Read().resetRequested && !File.Exists(h.Path+".bak1") && !File.Exists(h.Path+".bak2")
            && !Storage.DocumentExists(SavePaths.GetPlayerFile(id)),"Clear left recoverable state");
        using var reopened=new Host("clear"); reopened.Coordinator.Initialize();
        Check(!reopened.Read().resetRequested && reopened.Wallet.Gold==0,"Reset did not commit game defaults");
    }

    private static void InterruptedClear()
    {
        using var h=new Host("interrupted-clear"); h.Coordinator.Initialize();
        h.Wallet.SetGold(99); h.Coordinator.SaveNow();
        string id=h.Coordinator.ActiveProfile.ProfileId, legacy=SavePaths.GetPlayerFile(id);
        Storage.SaveDocument(legacy,SaveDocumentType.Player,1,new PlayerSaveData {gold=500});
        using(new FileStream(legacy,FileMode.Open,FileAccess.Read,FileShare.None))
            Throws(()=>h.Profiles.ClearProfileSaveData(id));
        Check(h.Read().resetRequested,"Reset was not committed before cleanup");
        using var reopened=new Host("interrupted-clear"); reopened.Coordinator.Initialize();
        Check(reopened.Wallet.Gold==0 && !reopened.Read().resetRequested,"Interrupted clear resurrected legacy state");
        File.WriteAllText(reopened.Path,"broken");
        Check(reopened.Read().resetRequested,"Recovery resurrected pre-reset character");
    }

    private static void ProfileMetadata()
    {
        using var h=new Host("metadata"); var profile=h.Profiles.GetOrCreateActiveProfile("original");
        string index=SavePaths.ProfilesIndexFile, manifest=SavePaths.GetProfileManifestFile(profile.ProfileId);
        File.WriteAllText(index,"broken");
        Throws(()=>h.Profiles.GetOrCreateActiveProfile("replacement"));
        Check(File.ReadAllText(index)=="broken","Damaged index overwritten");
        Storage.SaveDocument(index,SaveDocumentType.ProfilesIndex,1,new SaveProfilesIndexData
        {
            activeProfileId="missing", profiles=new() { new() {profileId="missing"},new() {profileId=profile.ProfileId} }
        });
        Check(h.Profiles.GetOrCreateActiveProfile("replacement").ProfileId==profile.ProfileId,"Did not find remaining profile");
        // Valid envelopes with another profile's payload are invalid manifests too.
        File.WriteAllText(manifest,Codec.Encode(SaveDocumentType.ProfileManifest,1,new ProfileManifestData {profileId="other"}));
        Throws(()=>h.Profiles.GetOrCreateActiveProfile("replacement"));
        Check(Directory.GetDirectories(SavePaths.ProfilesDirectory).Length==1,"Damaged manifest caused silent profile creation");
    }
}
