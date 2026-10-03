using Tutorials;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[CustomEditor(typeof(TutorialWindow))]
internal sealed class TutorialWindowEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (Application.isPlaying) return;
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Предпросмотр окна", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Слева")) Preview(false);
            if (GUILayout.Button("Справа")) Preview(true);
            if (GUILayout.Button("Скрыть")) Hide();
        }
        EditorGUILayout.HelpBox("Предпросмотр не запускает события и не меняет прогресс обучения. Ширина указана в пикселях игрового экрана.", MessageType.Info);
    }

    private void Preview(bool right)
    {
        serializedObject.Update();
        var root = serializedObject.FindProperty("windowRoot").objectReferenceValue as GameObject;
        var panel = serializedObject.FindProperty("panel").objectReferenceValue as RectTransform;
        var dimmer = serializedObject.FindProperty("dimmer").objectReferenceValue as CanvasGroup;
        if (root == null || panel == null || dimmer == null) return;
        Undo.RecordObjects(new Object[] {root, panel, dimmer}, "Preview tutorial window");
        root.SetActive(true);
        dimmer.alpha = serializedObject.FindProperty("dimOpacity").floatValue;
        panel.anchorMin = new Vector2(right ? 1f : 0f, 0f);
        panel.anchorMax = new Vector2(right ? 1f : 0f, 1f);
        panel.pivot = new Vector2(right ? 1f : 0f, .5f);
        var canvas = panel.GetComponentInParent<Canvas>();
        float scale = canvas == null ? 1f : Mathf.Max(.01f, canvas.rootCanvas.scaleFactor);
        panel.sizeDelta = new Vector2(serializedObject.FindProperty("panelWidthPixels").floatValue / scale, 0f);
        panel.anchoredPosition = Vector2.zero;
        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    private void Hide()
    {
        var root = serializedObject.FindProperty("windowRoot").objectReferenceValue as GameObject;
        if (root == null) return;
        Undo.RecordObject(root, "Hide tutorial preview");
        root.SetActive(false);
        EditorSceneManager.MarkSceneDirty(root.scene);
        EditorApplication.QueuePlayerLoopUpdate();
    }
}


