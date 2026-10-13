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
        EditorGUILayout.HelpBox("Размеры и шрифты масштабируются от Reference Screen Height. Panel Width Pixels задаёт ширину в эталонных единицах; Max Screen Width Fraction ограничивает долю экрана.", MessageType.Info);
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
        var canvas = panel.GetComponentInParent<Canvas>().rootCanvas;
        Undo.RecordObject(canvas, "Preview tutorial scale");
        var scaler = canvas.GetComponent<UnityEngine.UI.CanvasScaler>();
        if (scaler != null) Undo.RecordObject(scaler, "Preview tutorial scale");
        ((TutorialWindow)target).UpdateResponsiveLayout(Handles.GetMainGameViewSize());
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


