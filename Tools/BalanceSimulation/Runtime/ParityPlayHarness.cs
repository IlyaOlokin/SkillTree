using System;
using System.Collections;
using System.IO;
using BalanceParity;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ParityPlayHarness : MonoBehaviour
{
    IEnumerator Start()
    {
        if (!Application.isPlaying) { Fail(new InvalidOperationException("Native fixture requires Play Mode.")); yield break; }
        foreach (var name in Scenario.Names) {
            Scenario scenario = null;
            try { scenario = new Scenario(name, 1f / 60f, false); }
            catch (Exception error) { Fail(error); yield break; }
            // Give the native runtime its Awake/Start and deferred destruction frames.
            yield return null; yield return null;
            try { scenario.Prepare(); } catch (Exception error) { scenario.Dispose(); Fail(error); yield break; }
            float started = Time.realtimeSinceStartup;
            while (!scenario.Complete && Time.realtimeSinceStartup - started < 30) yield return null;
            if (!scenario.Complete) { scenario.Dispose(); Fail(new TimeoutException(name)); yield break; }
            File.WriteAllText(Path.Combine(Output(), name + ".json"), JsonUtility.ToJson(scenario.trace, true));
            scenario.Dispose(); yield return null; yield return null;
        }
        File.WriteAllText(Path.Combine(Output(), "completed.json"), "{\"nativePlayMode\":true,\"cases\":4}");
#if UNITY_EDITOR
        EditorApplication.Exit(0);
#endif
    }
    static string Output() => Environment.GetEnvironmentVariable("BALANCE_PARITY_OUTPUT");
    static void Fail(Exception error) {
        File.WriteAllText(Path.Combine(Output(), "error.txt"), error.ToString());
#if UNITY_EDITOR
        EditorApplication.Exit(1);
#endif
    }
}
