#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Gems;
using SkillTree;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaveSystem.Editor
{
    // IDs belong to scene instances, not prefab assets. Never generate IDs at runtime:
    // they must remain serialized with the scene across game sessions.
    [InitializeOnLoad]
    internal static class MissingNodeSaveIdAssignment
    {
        static MissingNodeSaveIdAssignment()
        {
            // InitializeOnLoad installs these once per Editor domain, including across Play Mode.
#pragma warning disable UDR0001
            EditorApplication.hierarchyChanged += ScheduleAssignment;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#pragma warning restore UDR0001
            ScheduleAssignment();
        }

        private static void ScheduleAssignment()
        {
            EditorApplication.delayCall -= AssignMissingIds;
            EditorApplication.delayCall += AssignMissingIds;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                AssignMissingIds();
        }

        private static void AssignMissingIds()
        {
            if (EditorApplication.isPlaying) return;
            foreach (Node node in Resources.FindObjectsOfTypeAll<Node>())
            {
                Scene scene = node.gameObject.scene;
                if (EditorUtility.IsPersistent(node) || !scene.IsValid() || !scene.isLoaded
                    || EditorSceneManager.IsPreviewScene(scene)
                    || PrefabStageUtility.GetPrefabStage(node.gameObject) != null
                    || !string.IsNullOrWhiteSpace(node.ExplicitSaveId))
                    continue;

                Undo.RecordObject(node, "Assign permanent node save ID");
                node.EnsureSaveId();
                PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                EditorUtility.SetDirty(node);
                EditorSceneManager.MarkSceneDirty(scene);
            }
        }
    }

    public static class SaveIdMaintenanceTool
    {
        [MenuItem("Tools/Save System/Assign Save IDs")]
        public static void AssignSaveIds()
        {
            bool anyChanges = false;

            Node[] nodes = Resources.FindObjectsOfTypeAll<Node>();
            var seenNodeIds = new HashSet<string>();
            // Do not guess which of two copies owns progress saved under an existing ID.
            foreach (Node node in nodes)
            {
                if (EditorUtility.IsPersistent(node) || !node.gameObject.scene.IsValid()) continue;
                if (!string.IsNullOrWhiteSpace(node.ExplicitSaveId) && !seenNodeIds.Add(node.ExplicitSaveId))
                    throw new InvalidOperationException($"Duplicate node save ID on '{node.name}'. Clear saveId and legacySaveIds on the new copy before assigning IDs.");
            }
            seenNodeIds.Clear();
            for (int i = 0; i < nodes.Length; i++)
            {
                Node node = nodes[i];
                if (EditorUtility.IsPersistent(node) || !node.gameObject.scene.IsValid())
                    continue;

                string explicitId = node.ExplicitSaveId;
                bool needsId = string.IsNullOrWhiteSpace(explicitId);
                bool isDuplicate = !needsId && seenNodeIds.Contains(explicitId);
                if (needsId || isDuplicate)
                {
                    Undo.RecordObject(node, "Assign permanent node save ID");
                    node.RegenerateSaveId();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(node);
                    EditorUtility.SetDirty(node);
                    anyChanges = true;
                    seenNodeIds.Add(node.ExplicitSaveId);
                }
                else
                {
                    seenNodeIds.Add(explicitId);
                }

            }

            string[] gemGuids = AssetDatabase.FindAssets("t:GemDefinition");
            var seenGemIds = new HashSet<string>();
            for (int i = 0; i < gemGuids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(gemGuids[i]);
                GemDefinition definition = AssetDatabase.LoadAssetAtPath<GemDefinition>(assetPath);
                if (definition == null)
                    continue;

                string explicitId = definition.ExplicitSaveDefinitionId;
                bool needsId = string.IsNullOrWhiteSpace(explicitId);
                bool isDuplicate = !needsId && seenGemIds.Contains(explicitId);
                if (needsId || isDuplicate)
                {
                    definition.RegenerateSaveDefinitionId();
                    EditorUtility.SetDirty(definition);
                    anyChanges = true;
                    seenGemIds.Add(definition.ExplicitSaveDefinitionId);
                }
                else
                {
                    seenGemIds.Add(explicitId);
                }
            }

            if (anyChanges)
            {
                EditorSceneManager.MarkAllScenesDirty();
                AssetDatabase.SaveAssets();
                EditorSceneManager.SaveOpenScenes();
            }

            Debug.Log("Save ID assignment completed.");
        }
    }

    public sealed class NodeSaveIdBuildValidator : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // The same callback is also invoked for entering play mode.
            if (report == null && !BuildPipeline.isBuildingPlayer) return;
            Validate(scene);
        }

        public static void Validate(Scene scene)
        {
            var ids = new Dictionary<string, Node>(StringComparer.Ordinal);
            var aliases = new Dictionary<string, Node>(StringComparer.Ordinal);
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Node node in root.GetComponentsInChildren<Node>(true))
            {
                string id = node.ExplicitSaveId;
                if (string.IsNullOrWhiteSpace(id) || ids.ContainsKey(id))
                    throw new BuildFailedException($"Missing or duplicate save ID on '{node.name}' in {scene.path}. Use Tools/Save System/Assign Save IDs.");
                ids.Add(id, node);
                if (node.LegacySaveIds == null) continue;
                foreach (string legacy in node.LegacySaveIds)
                {
                    if (string.IsNullOrWhiteSpace(legacy)) continue;
                    if (aliases.TryGetValue(legacy, out Node owner) && owner != node)
                        throw new BuildFailedException($"Duplicate migration key on '{node.name}' in {scene.path}: {legacy}");
                    aliases[legacy] = node;
                }
            }
            foreach (var alias in aliases)
                if (ids.TryGetValue(alias.Key, out Node owner) && owner != alias.Value)
                    throw new BuildFailedException($"Migration key conflicts with node save ID in {scene.path}: {alias.Key}");
        }
    }
}
#endif
