using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildScript
{
    public static void BuildWindows()
    {
        Debug.Log("BUILD SCRIPT STARTED: WINDOWS");
        
        string root = Directory.GetParent(Application.dataPath).FullName;

        Build(
            BuildTarget.StandaloneWindows64,
            Path.Combine(root, "Builds/Windows"),
            "Game.exe"
        );
    }

    public static void BuildWebGL()
    {
        Debug.Log("BUILD SCRIPT STARTED: WEBGL");
        
        string root = Directory.GetParent(Application.dataPath).FullName;
        
        Build(
            BuildTarget.WebGL,
            Path.Combine(root, "Builds/Windows"),
            null
        );
    }

    private static void Build(BuildTarget target, string outputDir, string exeName)
    {
        string version = GetArg("-buildVersion", "0.0.0");
        string buildNumber = GetArg("-buildNumber", "0");

        PlayerSettings.bundleVersion = version;

        string fullOutputPath = Path.GetFullPath(outputDir);

        if (Directory.Exists(fullOutputPath))
            Directory.Delete(fullOutputPath, true);

        Directory.CreateDirectory(fullOutputPath);

        string locationPathName = target == BuildTarget.StandaloneWindows64
            ? Path.Combine(fullOutputPath, exeName)
            : fullOutputPath;

        string[] scenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (scenes.Length == 0)
            throw new Exception("No enabled scenes in Build Settings.");

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = locationPathName,
            target = target,
            options = BuildOptions.None
        };

        Debug.Log($"Starting build: {target}");
        Debug.Log($"Version: {version}");
        Debug.Log($"Build number: {buildNumber}");
        Debug.Log($"Output: {locationPathName}");

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;

        Debug.Log($"Build result: {summary.result}");
        Debug.Log($"Build size: {summary.totalSize} bytes");
        Debug.Log($"Build time: {summary.totalTime}");

        if (summary.result != BuildResult.Succeeded)
            throw new Exception($"Build failed: {summary.result}");
    }

    private static string GetArg(string name, string defaultValue)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return defaultValue;
    }
}