using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class CommandLineBuild
{
    public static void BuildAndroid()
    {
        var scenes = new[]
        {
            "Assets/Scenes/LandingPage.unity",
            "Assets/Scenes/Circles.unity",
            "Assets/Scenes/Playground.unity",
            "Assets/Scenes/FractionsDemo.unity",
        };
        var options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = "build-file/playground.apk",
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Build failed: {report.summary.result}");
            EditorApplication.Exit(1);
        }
        else
        {
            Debug.Log("Build succeeded!");
        }
    }
}
