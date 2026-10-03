using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[CustomEditor(typeof(ProceduralMagicRootUI)), CanEditMultipleObjects]
public sealed class ProceduralMagicRootUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        DrawDefaultInspector();
        if (EditorGUI.EndChangeCheck())
            foreach (Object item in targets)
            {
                var root = (ProceduralMagicRootUI)item;
                Undo.RecordObject(root, "Edit Magic Root");
                root.ApplyToMaterial();
                EditorUtility.SetDirty(root);
                PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            }
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Edit parameters for the selected Light or Darkness style. Each style remembers its own settings when switching; the buttons select your saved settings. Seed and Fill are shared.", MessageType.Info);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Light")) Apply(0);
            if (GUILayout.Button("Darkness")) Apply(1);
            if (GUILayout.Button("New Seed")) Apply(2);
        }
        EditorGUILayout.HelpBox("Branch Count controls available branches (up to 96); Branch Density controls how many grow. The root tapers to the current fill edge. Texture and sprite alpha are ignored.", MessageType.Info);
        var component = (ProceduralMagicRootUI)target;
        var graphic = serializedObject.FindProperty("targetGraphic").objectReferenceValue as Graphic;
        if (graphic == null) graphic = component.GetComponent<Graphic>();
        if (graphic == null)
            EditorGUILayout.HelpBox("Assign Target Graphic or add this component to a RawImage/Image.", MessageType.Error);
        if (graphic is Image filled && filled.type == Image.Type.Filled)
            EditorGUILayout.HelpBox("For a horizontal Image filled from the left, its Fill Amount also controls the root's tapered tip. Leave this component's Fill Amount at 1 to follow the Image.", MessageType.Info);
        else if (graphic is Image image && (image.type != Image.Type.Simple || image.useSpriteMesh))
            EditorGUILayout.HelpBox("For a complete bar use Image Type = Simple and disable Use Sprite Mesh. Image geometry can otherwise cut away the root.", MessageType.Warning);
    }

    private void Apply(int action)
    {
        foreach (Object item in targets)
        {
            var root = (ProceduralMagicRootUI)item;
            Undo.RecordObject(root, "Magic Root Preset");
            if (action == 0) root.ApplyLightPreset();
            else if (action == 1) root.ApplyDarknessPreset();
            else root.RandomizeSeed();
            EditorUtility.SetDirty(root);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
        }
    }
}
