using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>Restore Pipeline discovery when its descriptor disappears while the server is live.</summary>
[InitializeOnLoad]
internal static class PipelineDiscoveryRecovery
{
    private static double nextCheck;
    private static bool reportedFailure;

    static PipelineDiscoveryRecovery()
    {
        EditorApplication.update += Check;
    }

    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < nextCheck || EditorApplication.isCompiling)
            return;
        nextCheck = EditorApplication.timeSinceStartup + 5;

        var projectPath = Path.GetDirectoryName(Application.dataPath);
        if (File.Exists(Path.Combine(projectPath, "Library", "Pipeline", ".unity-pipeline-port")))
        {
            reportedFailure = false;
            return;
        }

        try
        {
            // Pipeline's server is internal; use its own heartbeat writer so token handling
            // and owner-only permissions stay with the package. Never start a stopped server.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var startup = assembly.GetType("Unity.Pipeline.Editor.PipelineServerStartup");
                if (startup == null)
                    continue;
                var server = startup.GetProperty("Server", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                if (server == null || !(server.GetType().GetProperty("IsRunning")?.GetValue(server) is bool running) || !running)
                    return;
                var heartbeat = server.GetType().GetMethod("UpdateHeartBeat", BindingFlags.Instance | BindingFlags.NonPublic);
                if (heartbeat == null)
                    throw new MissingMethodException("Pipeline UpdateHeartBeat is unavailable; review package compatibility.");
                heartbeat.Invoke(server, null);
                if (File.Exists(Path.Combine(projectPath, "Library", "Pipeline", ".unity-pipeline-port")))
                    Debug.Log("Restored the missing Unity Pipeline discovery descriptor.");
                return;
            }
        }
        catch (Exception exception)
        {
            if (reportedFailure)
                return;
            reportedFailure = true;
            Debug.LogWarning("Pipeline discovery recovery failed: " + exception.GetBaseException().Message);
        }
    }
}
