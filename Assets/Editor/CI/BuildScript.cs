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
            Path.Combine(root, "Builds", "Windows"),
            "Game.exe"
        );
    }

    public static void BuildWebGL()
    {
        Debug.Log("BUILD SCRIPT STARTED: WEBGL");

        string root = Directory.GetParent(Application.dataPath).FullName;

        Build(
            BuildTarget.WebGL,
            Path.Combine(root, "Builds", "WebGL"),
            null
        );
    }

    private static void Build(BuildTarget target, string outputDir, string exeName)
    {
        try
        {
            string version = GetArg("-buildVersion", "0.0.0");
            string buildNumber = GetArg("-buildNumber", "0");

            Debug.Log($"Build target: {target}");
            Debug.Log($"Build version: {version}");
            Debug.Log($"Build number: {buildNumber}");
            Debug.Log($"Output directory: {outputDir}");

            PlayerSettings.bundleVersion = version;

            if (Directory.Exists(outputDir))
            {
                Debug.Log($"Deleting old build directory: {outputDir}");
                Directory.Delete(outputDir, true);
            }

            Directory.CreateDirectory(outputDir);

            string locationPathName = target == BuildTarget.StandaloneWindows64
                ? Path.Combine(outputDir, exeName)
                : outputDir;

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                throw new Exception("No enabled scenes in Build Settings.");
            }

            Debug.Log("Scenes:");
            foreach (string scene in scenes)
            {
                Debug.Log(scene);
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = target,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"Build result: {summary.result}");
            Debug.Log($"Build size: {summary.totalSize}");
            Debug.Log($"Build time: {summary.totalTime}");
            Debug.Log($"Build output path: {summary.outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build failed with result: {summary.result}");
            }

            Debug.Log("BUILD SCRIPT FINISHED SUCCESSFULLY");
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogError("BUILD SCRIPT FAILED");
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static string GetArg(string name, string defaultValue)
    {
        string[] args = Environment.GetCommandLineArgs();

        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
            {
                return args[i + 1];
            }
        }

        return defaultValue;
    }
}