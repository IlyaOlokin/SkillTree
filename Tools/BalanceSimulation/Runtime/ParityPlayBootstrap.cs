using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad] public static class ParityPlayBootstrap
{
    static ParityPlayBootstrap() { EditorApplication.delayCall += Begin; }
    static void Begin() {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Begin; return; }
        if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("BalanceParityStarted", false)) return;
        SessionState.SetBool("BalanceParityStarted", true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("BalanceParityHarness").AddComponent<ParityPlayHarness>();
        EditorApplication.EnterPlaymode();
    }
}
