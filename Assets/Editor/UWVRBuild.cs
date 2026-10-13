using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Development-only Windows OpenXR build. Never overwrites the public release.</summary>
public static class UWVRBuild
{
    [MenuItem("Underworld/VR/Build Windows Prototype")]
    public static void BuildWindowsPrototype()
    {
        UWVRSetup.ConfigureWindowsOpenXR();

        string[] lOScenes = EditorBuildSettings.scenes
            .Where(pOScene => pOScene.enabled)
            .Select(pOScene => pOScene.path)
            .ToArray();
        if (lOScenes.Length == 0)
            throw new InvalidOperationException("[UWVR] No enabled scenes in Build Settings.");

        string lSProjectRoot = Directory.GetParent(Application.dataPath).FullName;
        string lSBuildDirectory = Path.Combine(lSProjectRoot, "Build-vr-prototype");
        Directory.CreateDirectory(lSBuildDirectory);

        var lOOptions = new BuildPlayerOptions
        {
            scenes = lOScenes,
            locationPathName = Path.Combine(lSBuildDirectory, "UR.exe"),
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.Development
        };

        BuildReport lOReport = BuildPipeline.BuildPlayer(lOOptions);
        Debug.Log("[UWVR] Build result: " + lOReport.summary.result +
                  "; errors: " + lOReport.summary.totalErrors +
                  "; output: " + lOOptions.locationPathName);
        if (lOReport.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("[UWVR] Windows VR prototype build failed.");
    }
}
