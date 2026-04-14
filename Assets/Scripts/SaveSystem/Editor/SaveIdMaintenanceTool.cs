#if UNITY_EDITOR
using System.Collections.Generic;
using Gems;
using SkillTree;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SaveSystem.Editor
{
    public static class SaveIdMaintenanceTool
    {
        [MenuItem("Tools/Save System/Assign Save IDs")]
        public static void AssignSaveIds()
        {
            bool anyChanges = false;

            Node[] nodes = Resources.FindObjectsOfTypeAll<Node>();
            var seenNodeIds = new HashSet<string>();
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
                    node.RegenerateSaveId();
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
}
#endif
