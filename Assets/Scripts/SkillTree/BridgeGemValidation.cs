#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Battle;
using Gems;
using InventorySystem;
using SaveSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkillTree
{
    // Uses isolated preview scenes; never edits the player's tree, inventory or save files.
    public static class BridgeGemValidation
    {
        private static string Report => Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/BridgeGemValidation.txt"));

        [MenuItem("Tools/Skill Tree/Add Bridge Gem (Play Mode)")]
        public static void GiveGem()
        {
            if (!Application.isPlaying) { Debug.LogWarning("Enter Play Mode first."); return; }
            PlayerInventory inventory = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            GemDefinition definition = Resources.Load<GemDefinition>("Items/BridgeGem");
            if (inventory == null || definition == null || !inventory.TryAddItem(InventoryItem.FromGem(definition.CreateInstance()), out _))
                Debug.LogWarning("Cannot add bridge gem: inventory missing or full.");
        }

        [MenuItem("Tools/Skill Tree/Validate Bridge Gem")]
        public static void RunChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Bridge validation runs outside Play Mode in isolated preview scenes.");
                return;
            }
            var results = new List<string>();
            Run("Cancel replacement leaves displaced gem in inventory", CancelReplacement, results);
            Run("Two ends consume one item; graph, save and queue round trip", CompleteAndSave, results);
            Run("Dependent removal and replacement fail; alternate route allows extraction", Dependencies, results);
            Run("Full inventory extraction is atomic", FullInventory, results);
            Run("Replacing a bridge uses freed inventory space", ReplaceBridge, results);
            Run("Cascading refund keeps both gems socketed but inactive", Refund, results);
            Run("Disconnected cycles cannot allocate without root", DisconnectedCycle, results);
            Run("Switching selection cancels unfinished bridge", SwitchSelection, results);
            Run("Invalid bridge save is rejected before changing nodes", InvalidSave, results);
            Run("Removing bridge prunes queued path", QueueRemoval, results);
            Run("Another bridge can provide the alternate root path", MultipleBridges, results);
            Run("Serialized copies with the same instance ID remain one bridge", SerializedCopies, results);
            Run("Failed second replacement keeps first end pending", FailedSecondEnd, results);
            Run("Cursor shows second end, clears on cancel, selects extracted pair", CursorState, results);
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllLines(Report, results);
            Debug.Log("Bridge gem validation:\n" + string.Join("\n", results));
            if (Application.isBatchMode && results.Any(line => line.StartsWith("FAIL ")))
                EditorApplication.Exit(1);
        }

        private static void Run(string name, Action test, List<string> results)
        {
            try { test(); results.Add("PASS " + name); }
            catch (Exception error) { results.Add("FAIL " + name + "\n" + error); }
        }
        private static void Check(bool value, string message)
        {
            if (!value) throw new InvalidOperationException(message);
        }

        private static void CancelReplacement()
        {
            using var f = new Fixture();
            f.A.TryInsertGem(f.Normal.CreateInstance());
            f.SelectBridge();
            Check(f.Placement.TryPlaceSelectedGem(f.A), "first end rejected");
            Check(f.A.HasPendingBridge && f.A.BridgePartner == null, "pending end created an edge");
            Check(f.Count(f.Bridge) == 1 && f.Count(f.Normal) == 1, "reserved/displaced items incorrect");
            Check(f.Save().bridgeGems.Count == 0 && f.Save().socketedGems.Count == 0, "pending end saved");
            f.Placement.ClearSelection();
            Check(!f.A.HasGem && !f.Selection.HasSelectedItem && f.Count(f.Bridge) == 1 && f.Count(f.Normal) == 1, "cancel lost/duplicated an item");
        }

        private static void CompleteAndSave()
        {
            using var f = new Fixture(6, 2);
            f.A.TryInsertGem(f.Normal.CreateInstance());
            f.B.TryInsertGem(f.Normal.CreateInstance());
            f.Complete();
            Check(f.Count(f.Bridge) == 1 && f.Count(f.Normal) == 2, "wrong inventory counts");
            Check(!f.Selection.HasSelectedItem && f.A.BridgePartner == f.B && ReferenceEquals(f.A.SocketedGem, f.B.SocketedGem), "pair/selection incorrect");
            Check(!f.B.CanBeAllocated(), "both unallocated ends created a root");
            Check(f.A.Allocate() && f.B.CanBeAllocated(), "bridge does not grant access to far socket");
            Check(!f.A.ConnectedNodes.Contains(f.B), "bridge polluted base graph");
            var graph = new SkillTreeAllocationGraphService(() => f.Nodes);
            List<Node> path = graph.FindShortestAllocationPath(f.C, Array.Empty<Node>());
            Check(path.Count == 2 && path[0] == f.B && path[1] == f.C, "shortest path ignored bridge");
            SkillTreeSaveData saved = f.Save();
            saved.allocationQueueNodeIds.Add(f.B.SaveId);
            saved.allocationQueueNodeIds.Add(f.C.SaveId);
            Check(saved.bridgeGems.Count == 1 && saved.socketedGems.Count == 0, "pair serialized twice");
            saved = JsonUtility.FromJson<SkillTreeSaveData>(JsonUtility.ToJson(saved));
            f.Saves.ApplyNodeState(saved, f.Nodes, f.Resolve, out var lookup, out _);
            Check(f.A.BridgePartner == f.B && f.B.CanBeAllocated(), "bridge lost on restore");
            var allocation = new SkillTreeAllocationService(() => f.Nodes, null, null);
            allocation.RestoreAllocationQueue(saved, lookup);
            Check(allocation.QueuedNodes.Count == 2, "queue lost bridge path after restore");
            allocation.Tick(1f); allocation.Tick(1f);
            Check(f.B.IsAllocated && f.C.IsAllocated, "restored queue failed to allocate");
        }

        private static void Dependencies()
        {
            using var f = new Fixture();
            f.Complete(); f.A.Allocate(); f.B.Allocate(); f.C.Allocate();
            Check(!f.Placement.TryExtractGemAndSelect(f.A), "dependent bridge extracted");
            f.Inventory.TryAddItem(InventoryItem.FromGem(f.Normal.CreateInstance()), out int normalSlot);
            f.Placement.TrySelectSlot(normalSlot);
            Check(!f.Placement.TryPlaceSelectedGem(f.A) && f.A.BridgePartner == f.B, "dependent bridge replaced");
            f.B.SetActiveFromLimitZone(false); f.C.SetActiveFromLimitZone(false);
            Check(!f.Placement.TryExtractGemAndSelect(f.B), "inactive dependent allocation lost its future route");
            Fixture.Connect(f.Root, f.B);
            Check(f.Placement.TryExtractGemAndSelect(f.B), "alternate route did not permit extraction");
            Check(!f.A.HasGem && !f.B.HasGem && f.Count(f.Bridge) == 1 && f.Selection.SelectedGem.Kind == GemKind.Bridge, "pair extraction incorrect");
        }

        private static void FullInventory()
        {
            using var f = new Fixture(1);
            f.Complete();
            f.Inventory.TryAddItem(InventoryItem.FromGem(f.Normal.CreateInstance()), out _);
            Check(!f.Placement.TryExtractGemAndSelect(f.A), "extracted into full inventory");
            Check(f.A.BridgePartner == f.B && f.Count(f.Bridge) == 0 && f.Count(f.Normal) == 1, "failed extraction mutated state");
        }

        private static void ReplaceBridge()
        {
            using var f = new Fixture(1);
            f.Complete();
            f.Inventory.TryAddItem(InventoryItem.FromGem(f.Normal.CreateInstance()), out int slot);
            f.Placement.TrySelectSlot(slot);
            Check(f.Placement.TryPlaceSelectedGem(f.A), "replacement did not use freed slot");
            Check(f.A.SocketedGem.Kind == GemKind.LocalModifiers && !f.B.HasGem && f.Count(f.Bridge) == 1, "replacement left an end or duplicated bridge");
        }

        private static void Refund()
        {
            using var f = new Fixture();
            f.Complete(); f.A.Allocate(); f.B.Allocate(); f.C.Allocate();
            var allocation = new SkillTreeAllocationService(() => f.Nodes, null, null);
            Check(allocation.TryDeallocateWithDependents(f.A), "filled socket blocked refund");
            for (int i = 0; i < 5; i++) allocation.Tick(1f);
            Check(!f.A.IsAllocated && !f.B.IsAllocated && !f.C.IsAllocated, "bridge dependents not refunded");
            Check(f.Level.SkillPoints == 100 && f.A.BridgePartner == f.B && !f.A.IsGemActive && !f.B.IsGemActive, "refund lost gems or skill points");
        }

        private static void DisconnectedCycle()
        {
            using var f = new Fixture();
            f.Complete();
            Fixture.Disconnect(f.Root, f.A);
            f.A.SetAllocatedFromSave(true); f.B.SetAllocatedFromSave(true);
            Check(!f.C.CanBeAllocated(), "cycle became its own root");
        }

        private static void SwitchSelection()
        {
            using var f = new Fixture();
            f.Inventory.TryAddItem(InventoryItem.FromGem(f.Normal.CreateInstance()), out int slot);
            f.SelectBridge(); f.Placement.TryPlaceSelectedGem(f.A);
            f.Placement.TrySelectSlot(slot);
            Check(!f.A.HasGem && !f.Placement.IsPlacingBridge && f.Selection.SelectedGem.Definition == f.Normal, "switch did not cancel/select next item");
        }

        private static void InvalidSave()
        {
            using var f = new Fixture();
            f.Complete();
            SkillTreeSaveData saved = f.Save();
            saved.bridgeGems[0].secondSocketId = "missing";
            bool rejected = false;
            try { f.Saves.ApplyNodeState(saved, f.Nodes, f.Resolve, out _, out _); }
            catch (InvalidOperationException) { rejected = true; }
            Check(rejected && f.A.BridgePartner == f.B, "invalid save modified live sockets");
        }

        private static void QueueRemoval()
        {
            using var f = new Fixture();
            f.Complete(); f.A.Allocate();
            var allocation = new SkillTreeAllocationService(() => f.Nodes, null, null);
            Check(allocation.TryQueueNodeForAllocation(f.B) && allocation.TryQueueNodeForAllocation(f.C), "queue setup failed");
            Check(f.Placement.TryExtractGemAndSelect(f.A), "unallocated queued nodes incorrectly blocked removal");
            allocation.ProcessQueuedAllocations();
            Check(allocation.QueuedNodes.Count == 0, "broken queued path was retained");
        }

        private static void MultipleBridges()
        {
            using var f = new Fixture(6, 2);
            f.Complete(); f.A.Allocate(); f.B.Allocate();
            var extraRootSocket = f.ExtraSocket("extraRoot");
            var extraFarSocket = f.ExtraSocket("extraFar");
            Fixture.Connect(f.Root, extraRootSocket); Fixture.Connect(f.B, extraFarSocket);
            f.SelectBridge();
            Check(f.Placement.TryPlaceSelectedGem(extraRootSocket) && f.Placement.TryPlaceSelectedGem(extraFarSocket), "second bridge failed");
            Check(extraRootSocket.Allocate() && extraFarSocket.Allocate(), "extra sockets not allocated");
            Check(f.Placement.TryExtractGemAndSelect(f.A), "second bridge was not considered an alternate route");
            Check(!f.Placement.TryExtractGemAndSelect(extraFarSocket), "last root connection was removed");
        }

        private static void SerializedCopies()
        {
            using var f = new Fixture();
            f.Complete();
            f.B.SetGemState(GemInstance.Restore(f.Bridge, f.A.SocketedGem.InstanceId), f.A);
            Check(f.A.BridgePartner == f.B && f.Save().bridgeGems.Count == 1, "domain reload split one bridge into two items");
            Check(f.Placement.TryExtractGemAndSelect(f.B) && f.Count(f.Bridge) == 1 && !f.A.HasGem, "serialized copies extracted twice");
        }

        private static void FailedSecondEnd()
        {
            using var f = new Fixture(1, 2);
            f.B.TryInsertGem(f.Normal.CreateInstance());
            f.SelectBridge(); Check(f.Placement.TryPlaceSelectedGem(f.A), "first end failed");
            Check(!f.Placement.TryPlaceSelectedGem(f.B), "replacement succeeded without space for displaced item");
            Check(f.A.HasPendingBridge && f.B.SocketedGem.Definition == f.Normal && f.Count(f.Bridge) == 2, "failed second end mutated inventory/sockets");
            f.Placement.ClearSelection();
            Check(!f.A.HasGem && f.Count(f.Bridge) == 2, "cancel after failure lost reserved item");
        }

        private static void CursorState()
        {
            using var f = new Fixture();
            var cursor = f.CreateComponent<UI.SelectedGemCursorUI>("cursor");
            var root = new GameObject("cursor content", typeof(RectTransform));
            root.transform.SetParent(cursor.transform, false);
            var label = new GameObject("count", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
            label.transform.SetParent(root.transform, false);
            var count = label.GetComponent<TMPro.TextMeshProUGUI>();
            Set(cursor, "root", root.GetComponent<RectTransform>());
            Set(cursor, "stackCountText", count);
            Set(cursor, "_selectionState", f.Selection);
            Set(cursor, "_placement", f.Placement);
            typeof(UI.SelectedGemCursorUI).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(cursor, null);
            Check(!root.activeSelf, "empty cursor visible");
            f.SelectBridge();
            Check(root.activeSelf, "selected bridge not visible");
            f.Placement.TryPlaceSelectedGem(f.A);
            Check(!count.gameObject.activeSelf && root.activeSelf, "second end should show the gem without text");
            f.Placement.ClearSelection();
            Check(!root.activeSelf, "cancel left cursor visible");
            f.Complete();
            Check(!root.activeSelf, "completed pair left cursor visible");
            Check(f.Placement.TryExtractGemAndSelect(f.B) && root.activeSelf, "extracted pair was not selected on cursor");
        }

        private sealed class Fixture : IDisposable
        {
            private readonly Scene _scene = EditorSceneManager.NewPreviewScene();
            public readonly GemDefinition Bridge;
            public readonly GemDefinition Normal;
            public readonly PlayerInventory Inventory;
            public readonly InventorySelectionState Selection;
            public readonly GemPlacementService Placement;
            public readonly UnitLevel Level;
            public readonly RootNode Root;
            public readonly SocketNode A, B;
            public readonly Node Middle, C;
            public readonly List<Node> Nodes;
            public readonly SkillTreeSaveService Saves = new();
            public Fixture(int slots = 6, int bridgeCount = 1)
            {
                Bridge = ScriptableObject.CreateInstance<GemDefinition>();
                Normal = ScriptableObject.CreateInstance<GemDefinition>();
                Set(Bridge, "kind", GemKind.Bridge); Set(Bridge, "maxStack", 99);
                Bridge.name = "TestBridge"; Normal.name = "TestNormal";
                Level = Create<UnitLevel>("level");
                Level.ApplySaveData(new PlayerSaveData { level = 1, skillPoints = 100 });
                Inventory = Create<PlayerInventory>("inventory");
                Inventory.ApplySaveData(new InventorySaveData { slotCount = slots }, null, null);
                Inventory.TryAddItem(InventoryItem.FromGem(Bridge.CreateInstance(), bridgeCount), out _);
                Selection = new InventorySelectionState(Inventory);
                Placement = new GemPlacementService(Inventory, Selection, new InventorySocketService());
                Root = Create<RootNode>("root"); A = Create<SocketNode>("A"); B = Create<SocketNode>("B");
                Middle = Create<Node>("middle"); C = Create<Node>("C");
                Nodes = new List<Node> { Root, A, Middle, B, C };
                foreach (Node node in Nodes)
                {
                    Set(node, "_unitLevel", Level);
                    Set(node, "<Modifiers>k__BackingField", new List<Modifier>());
                    node.EnsureSaveId();
                }
                Connect(Root, A); Connect(A, Middle); Connect(Middle, B); Connect(B, C);
            }
            private T Create<T>(string name) where T : Component
            {
                var obj = new GameObject(name);
                SceneManager.MoveGameObjectToScene(obj, _scene);
                return obj.AddComponent<T>();
            }
            public T CreateComponent<T>(string name) where T : Component => Create<T>(name);
            public void SelectBridge()
            {
                int slot = Inventory.Slots.ToList().FindIndex(s => s.Item?.Gem?.Definition == Bridge);
                Check(Placement.TrySelectSlot(slot), "cannot select bridge");
            }
            public SocketNode ExtraSocket(string name)
            {
                SocketNode node = Create<SocketNode>(name);
                Set(node, "_unitLevel", Level);
                Set(node, "<Modifiers>k__BackingField", new List<Modifier>());
                node.EnsureSaveId(); Nodes.Add(node);
                return node;
            }
            public void Complete()
            {
                SelectBridge();
                Check(Placement.TryPlaceSelectedGem(A), "first end failed");
                Check(Placement.TryPlaceSelectedGem(B), "second end failed");
            }
            public int Count(GemDefinition definition) => Inventory.Slots.Where(s => s.Item?.Gem?.Definition == definition).Sum(s => s.Item.StackCount);
            public SkillTreeSaveData Save() => Saves.Capture(Nodes, Array.Empty<Node>(), null);
            public GemInstance Resolve(GemInstanceSaveData saved) => GemInstance.Restore(saved.definitionId == Bridge.SaveDefinitionId ? Bridge : Normal, saved.instanceId);
            public static void Connect(Node a, Node b) { ((List<Node>)a.ConnectedNodes).Add(b); ((List<Node>)b.ConnectedNodes).Add(a); }
            public static void Disconnect(Node a, Node b) { ((List<Node>)a.ConnectedNodes).Remove(b); ((List<Node>)b.ConnectedNodes).Remove(a); }
            public void Dispose()
            {
                Placement.Dispose(); Selection.Dispose();
                EditorSceneManager.ClosePreviewScene(_scene);
                UnityEngine.Object.DestroyImmediate(Bridge); UnityEngine.Object.DestroyImmediate(Normal);
            }
        }

        private static void Set(object obj, string name, object value)
        {
            for (Type type = obj.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
                if (field == null) continue;
                field.SetValue(obj, value);
                return;
            }
            throw new MissingFieldException(name);
        }
    }
}
#endif
