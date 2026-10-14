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
        UWVRDiagnostics.RunOfflineChecks();

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

        // Always recreate the launch scripts and Japanese test guide. They
        // must not depend on files left over from a previous test build.
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -logFile \"%~dp0VR_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchDesktop.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -logFile \"%~dp0Desktop_Test.log\"\r\n");

        // The reference game measures a tile in 64 game units, not metres.
        // Offer an intentionally EXPLICIT alternate scale instead of silently
        // picking one; only an HMD test can determine a comfortable value.
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_Scale16.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-units-per-meter=16 -logFile \"%~dp0VR_Scale16_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BodyRelative.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-body-relative -logFile \"%~dp0VR_BodyRelative_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_UIFar.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-ui-distance=3.5 -logFile \"%~dp0VR_UIFar_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_InventoryFar.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-inventory-distance=2.5 -logFile \"%~dp0VR_InventoryFar_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_SpatialInventory.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-spatial-inventory -logFile \"%~dp0VR_SpatialInventory_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_HandGrab.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-spatial-inventory -vr-near-grab -logFile \"%~dp0VR_HandGrab_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_DirectGrab.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-spatial-inventory -vr-near-grab -vr-direct-grab -logFile \"%~dp0VR_DirectGrab_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_HandAim.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -logFile \"%~dp0VR_HandAim_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_GestureMelee.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-gesture-melee -logFile \"%~dp0VR_GestureMelee_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BladeContact.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-gesture-melee -vr-blade-contact -logFile \"%~dp0VR_BladeContact_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BladeContact_Scale16.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-gesture-melee -vr-blade-contact -vr-units-per-meter=16 -logFile \"%~dp0VR_BladeContact_Scale16_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_HandOrigin.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -logFile \"%~dp0VR_HandOrigin_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_HandOrigin_Scale16.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-units-per-meter=16 -logFile \"%~dp0VR_HandOrigin_Scale16_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BladeAndHandOrigin.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-gesture-melee -vr-blade-contact -logFile \"%~dp0VR_BladeAndHandOrigin_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_TwoHandBow.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -logFile \"%~dp0VR_TwoHandBow_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_HandMagicFX.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-hand-magic-fx -logFile \"%~dp0VR_HandMagicFX_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowAndMagic.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -logFile \"%~dp0VR_BowAndMagic_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_AllCombat.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-gesture-melee -vr-blade-contact -vr-two-hand-bow -vr-hand-magic-fx -logFile \"%~dp0VR_AllCombat_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowMagicHaptics.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -logFile \"%~dp0VR_BowMagicHaptics_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_AllCombatHaptics.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-gesture-melee -vr-blade-contact -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -logFile \"%~dp0VR_AllCombatHaptics_Test.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowCal_Default.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -vr-combat-diagnostics -logFile \"%~dp0VR_BowCal_Default.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowCal_EasyReach.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -vr-combat-diagnostics -vr-bow-nock-radius=0.60 -vr-bow-min-pull=0.14 -vr-bow-full-pull=0.42 -vr-haptic-scale=0.65 -logFile \"%~dp0VR_BowCal_EasyReach.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowCal_Compact.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -vr-combat-diagnostics -vr-bow-nock-radius=0.32 -vr-bow-min-pull=0.18 -vr-bow-full-pull=0.40 -vr-haptic-scale=0.35 -logFile \"%~dp0VR_BowCal_Compact.log\"\r\n");
        File.WriteAllText(Path.Combine(lSBuildDirectory, "LaunchVR_BowCal_Silent.bat"),
            "@echo off\r\ncd /d \"%~dp0\"\r\nstart \"\" \"%~dp0UR.exe\" -vr -vr-hand-aim -vr-hand-origin -vr-two-hand-bow -vr-hand-magic-fx -vr-combat-haptics -vr-combat-diagnostics -vr-haptic-scale=0 -logFile \"%~dp0VR_BowCal_Silent.log\"\r\n");

        string lSGuide = Path.Combine(lSProjectRoot, "Docs", "VR_TEST_JA.txt");
        if (File.Exists(lSGuide))
            File.Copy(lSGuide, Path.Combine(lSBuildDirectory, "README_VR_TEST_JA.txt"), true);

        Debug.Log("[UWVR] VR and desktop launch scripts and test guide refreshed.");
    }
}
