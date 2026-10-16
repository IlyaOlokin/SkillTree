using System;
using System.IO;
using System.Reflection;
using Battle.MiniGames;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

// Explicit Pipeline run_script entry points. Never runs on import or modifies the active scene.
public static class MiniGameVisualAuthoring
{
    private const string Prefabs = "Assets/Prefabs/UI/MiniGames/";

    public static string Apply()
    {
        string[] names = { "DamageBuffMiniGame", "AttackSpeedBuffMiniGame", "MiniGameActivator" };
        var reference = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "AttackSpeedBuffMiniGame.prefab");
        var existingText = reference.GetComponentInChildren<TMP_Text>(true);
        var font = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;
        foreach (var name in names)
        {
            string path = Prefabs + name + ".prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var feedback = root.GetComponent<MiniGameFeedback>();
                if (feedback == null) feedback = root.AddComponent<MiniGameFeedback>();
                var serialized = new SerializedObject(feedback);
                serialized.FindProperty("font").objectReferenceValue = font;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        var table = AssetDatabase.LoadAssetAtPath<StringTable>("Assets/Localization/GameUI_en.asset");
        string[,] entries = {
            {"damage", "DAMAGE"}, {"speed", "ATTACK SPEED"}, {"align", "CLICK WHEN RINGS ALIGN"}, {"wait", "WAIT"},
            {"now", "CLICK NOW"}, {"rapid", "CLICK RAPIDLY"}, {"perfect", "PERFECT"},
            {"success", "SUCCESS"}, {"early", "TOO EARLY"}, {"late", "TOO LATE"},
            {"timeout", "TIME'S UP"}, {"damageReward", "DAMAGE BOOSTED"},
            {"speedReward", "ATTACK SPEED BOOSTED"}, {"noReward", "NO BONUS"}
        };
        for (int i = 0; i < entries.GetLength(0); i++) table.AddEntry("minigame." + entries[i, 0], entries[i, 1]);
        EditorUtility.SetDirty(table);
        EditorUtility.SetDirty(table.SharedData);
        AssetDatabase.SaveAssetIfDirty(table);
        AssetDatabase.SaveAssetIfDirty(table.SharedData);
        return "Feedback attached to three prefabs; 14 English GameUI entries synchronized. Scene and event settings unchanged.";
    }

    public static string Preview()
    {
        var scene = EditorSceneManager.NewPreviewScene();
        RenderTexture target = null;
        Texture2D readback = null;
        Camera camera = null;
        try
        {
            var cameraGo = new GameObject("PreviewCamera", typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraGo, scene);
            camera = cameraGo.GetComponent<Camera>();
            camera.scene = scene;
            camera.orthographic = true; camera.orthographicSize = 390f;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.035f, .043f, .052f);
            var canvasGo = new GameObject("PreviewCanvas", typeof(RectTransform), typeof(Canvas));
            SceneManager.MoveGameObjectToScene(canvasGo, scene);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            ((RectTransform)canvas.transform).sizeDelta = new Vector2(1500, 780);
            Header(canvas.transform, "DAMAGE  /  WAIT", new Vector2(-470, 342));
            Header(canvas.transform, "DAMAGE  /  CLICK", new Vector2(0, 342));
            Header(canvas.transform, "ATTACK SPEED", new Vector2(470, 342));
            for (int type = 0; type < 3; type++)
            {
                for (int state = 0; state < 2; state++)
                {
                    var name = type == 2 ? "AttackSpeedBuffMiniGame" : "DamageBuffMiniGame";
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
                    var instance = UnityEngine.Object.Instantiate(prefab);
                    SceneManager.MoveGameObjectToScene(instance, scene);
                    instance.transform.SetParent(canvas.transform, false);
                    ((RectTransform)instance.transform).anchoredPosition = new Vector2(-470 + type * 470, state == 0 ? 150 : -175);
                    var feedback = instance.GetComponent<MiniGameFeedback>();
                    feedback.Initialize(type == 2, false);
                    if (type == 0) feedback.Timing(1.65f, false);
                    else if (type == 1) feedback.Timing(1f, true);
                    else feedback.Rapid(.75f, 1, 5);
                    if (state == 1)
                    {
                        feedback.Resolve(type == 1 ? BattleMiniGameResult.Fail() : BattleMiniGameResult.Success(), type == 0, type == 1);
                        Set(feedback, "_resultAge", .22f);
                    }
                    Render(feedback);
                }
            }
            Canvas.ForceUpdateCanvases();
            target = new RenderTexture(1800, 936, 24) { antiAliasing = 4 };
            target.Create(); camera.targetTexture = target; camera.Render();
            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = target;
                readback = new Texture2D(1800, 936, TextureFormat.RGBA32, false);
                readback.ReadPixels(new Rect(0, 0, 1800, 936), 0, 0); readback.Apply();
                Directory.CreateDirectory("Temp/MiniGames");
                File.WriteAllBytes("Temp/MiniGames/visual-preview.png", readback.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; }
            return Path.GetFullPath("Temp/MiniGames/visual-preview.png");
        }
        finally
        {
            if (camera != null) camera.targetTexture = null;
            if (target != null) { target.Release(); UnityEngine.Object.DestroyImmediate(target); }
            if (readback != null) UnityEngine.Object.DestroyImmediate(readback);
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void Header(Transform parent, string text, Vector2 position)
    {
        var go = new GameObject("Caption", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<TextMeshProUGUI>(); label.text = text;
        label.fontSize = 16; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.65f, .67f, .65f);
        label.rectTransform.sizeDelta = new Vector2(450, 40); label.rectTransform.anchoredPosition = position;
    }
    private static void Set(object target, string name, object value) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    private static void Render(MiniGameFeedback feedback)
    {
        // Frozen preview frames skip Update; show the completed entry fade explicitly.
        feedback.GetComponentInChildren<CanvasGroup>(true).alpha = 1f;
        typeof(MiniGameFeedback).GetMethod("Render", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(feedback, null);
    }
}
