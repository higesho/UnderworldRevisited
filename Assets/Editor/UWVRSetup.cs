using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

/// <summary>
/// Creates the official XR settings assets via their editor APIs. This is
/// idempotent and targets only Windows Standalone; no Android configuration
/// and no existing desktop controls are changed.
/// </summary>
public static class UWVRSetup
{
    [MenuItem("Underworld/VR/Configure Windows OpenXR")]
    public static void ConfigureWindowsOpenXR()
    {
        const BuildTargetGroup lETarget = BuildTargetGroup.Standalone;

        XRGeneralSettingsPerBuildTarget lOAllSettings;
        if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out lOAllSettings)
            || lOAllSettings == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/XR"))
                AssetDatabase.CreateFolder("Assets", "XR");

            lOAllSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(lOAllSettings, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, lOAllSettings, true);
        }

        if (!lOAllSettings.HasManagerSettingsForBuildTarget(lETarget))
            lOAllSettings.CreateDefaultManagerSettingsForBuildTarget(lETarget);

        XRGeneralSettings lOSettings = lOAllSettings.SettingsForBuildTarget(lETarget);
        if (lOSettings == null || lOSettings.Manager == null)
            throw new InvalidOperationException("[UWVR] Could not create Standalone XR settings.");

        // The user must explicitly pass -vr to the player; non-VR launches
        // retain their existing behavior and do not start OpenXR automatically.
        lOSettings.InitManagerOnStart = false;

        if (!XRPackageMetadataStore.AssignLoader(lOSettings.Manager,
                typeof(OpenXRLoader).FullName, lETarget)
            && lOSettings.Manager.activeLoaders.Count == 0)
            throw new InvalidOperationException("[UWVR] Could not assign the OpenXR loader.");

        // Unity's OpenXR package keeps its settings provider internal. Use its
        // published editor factory instead of manually authoring package YAML.
        Type lOType = Type.GetType("UnityEditor.XR.OpenXR.OpenXRPackageSettings, Unity.XR.OpenXR.Editor");
        MethodInfo lOMethod = lOType != null ? lOType.GetMethod("GetOrCreateInstance",
            BindingFlags.Public | BindingFlags.Static) : null;
        if (lOMethod == null)
            throw new InvalidOperationException("[UWVR] OpenXR settings editor was not loaded.");

        lOMethod.Invoke(null, null);

        OpenXRSettings lOOpenXR = OpenXRSettings.GetSettingsForBuildTargetGroup(lETarget);
        if (lOOpenXR == null)
            throw new InvalidOperationException("[UWVR] OpenXR settings are unavailable after creation.");

        OculusTouchControllerProfile lOControllers = lOOpenXR.GetFeature<OculusTouchControllerProfile>();
        if (lOControllers != null)
        {
            lOControllers.enabled = true;
            EditorUtility.SetDirty(lOControllers);
        }
        else
        {
            Debug.LogWarning("[UWVR] Oculus Touch profile unavailable; enable it in Project Settings > OpenXR.");
        }

        EditorUtility.SetDirty(lOSettings);
        EditorUtility.SetDirty(lOSettings.Manager);
        EditorUtility.SetDirty(lOAllSettings);
        EditorUtility.SetDirty(lOOpenXR);
        AssetDatabase.SaveAssets();

        Debug.Log("[UWVR] Windows OpenXR loader configured. Automatic startup OFF; use -vr for testing.");
    }
}
