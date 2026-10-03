#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Battle;
using SaveSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SkillTree
{
    public static class InfiniteNodeValidation
    {
        [MenuItem("Tools/Skill Tree/Validate Infinite Node")]
        public static void RunChecks()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Run infinite node validation outside Play Mode.");
                return;
            }
            var scene = EditorSceneManager.NewPreviewScene();
            BaseModifier modifier = null;
            try
            {
                GameObject Create(string name)
                {
                    var obj = new GameObject(name);
                    SceneManager.MoveGameObjectToScene(obj, scene);
                    return obj;
                }
                var level = Create("Level").AddComponent<UnitLevel>();
                level.ApplySaveData(new PlayerSaveData { level = 1, skillPoints = 100 });
                var root = Create("Root").AddComponent<RootNode>();
                var node = Create("Infinite").AddComponent<InfiniteNode>();
                typeof(Node).GetField("_unitLevel", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, level);
                typeof(Node).GetField("connectedNodes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, new List<Node> { root });
                typeof(Node).GetField("connectedNodes", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(root, new List<Node> { node });
                modifier = ScriptableObject.CreateInstance<BaseModifier>();
                modifier.modifierContainer = new ModifierContainer(ModifierType.Increased, StatType.PhysicalDamage, 0.05f);
                typeof(Node).GetField("<Modifiers>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(node, new List<Modifier> { modifier });
                root.EnsureSaveId();
                node.EnsureSaveId();
                Check(!node.IsAllocated && node.InvestedSkillPoints == 0, "initial state");
                for (int i = 0; i < 100; i++) Check(node.Allocate(), "allocation " + i);
                Check(node.IsAllocated && node.IsActive && node.InvestedSkillPoints == 100 && level.SkillPoints == 0, "100 investments");
                Check(!node.Allocate(), "insufficient points");
                Check(Mathf.Approximately(ModifierPowerContext.FromNode(node).Scale(modifier.modifierContainer).value, 5f), "100 x 5% = 500%");
                Check(modifier.GetDescription(ModifierPowerContext.FromNode(node)).Contains(ModifierPowerContext.PoweredValueColorHex), "green tooltip");
                node.SetPermanentPowerFromSave(1f);
                node.ChangeRuntimePower(2f);
                Check(node.Power == 0f && !node.CanChangePower && ModifierPowerContext.FromNode(node).Multiplier == 100f, "power immunity");
                var saves = new SkillTreeSaveService();
                var nodes = new Node[] { root, node };
                var saved = JsonUtility.FromJson<SkillTreeSaveData>(JsonUtility.ToJson(saves.Capture(nodes, null, null)));
                node.SetAllocatedFromSave(false);
                saves.ApplyNodeState(saved, nodes, null, out _, out _);
                Check(node.InvestedSkillPoints == 100 && node.IsAllocated, "save round trip");
                Check(node.TryDeallocate() && node.InvestedSkillPoints == 99 && level.SkillPoints == 1, "single refund");
                var allocation = new SkillTreeAllocationService(() => nodes, () => { }, () => { });
                Check(allocation.TryAllocateOrQueue(node) && node.InvestedSkillPoints == 100, "left click service");
                Check(allocation.TryDeallocateWithDependents(node) && node.InvestedSkillPoints == 99, "right click service");
                Check(node.TryDeallocate(false) && !node.IsAllocated && node.InvestedSkillPoints == 0 && level.SkillPoints == 100, "branch refund all");
                node.SetAllocatedFromSave(true);
                Check(node.InvestedSkillPoints == 1, "legacy save default");
                Check(node.TryDeallocate() && !node.IsAllocated && node.InvestedSkillPoints == 0, "last point");
                modifier.modifierContainer.modifierType = ModifierType.More;
                Check(!node.CanBeAllocated(), "reject nonadditive modifier");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/InfiniteNodeValidation.txt", "PASS: allocation, scaling, tooltip, power, save round trip, single/all refunds, legacy save, modifier validation.");
                Debug.Log("Infinite node validation passed.");
            }
            catch (Exception exception)
            {
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/InfiniteNodeValidation.txt", "FAIL: " + exception);
                Debug.LogException(exception);
            }
            finally
            {
                if (modifier != null) UnityEngine.Object.DestroyImmediate(modifier);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
        }
    }
}
#endif
