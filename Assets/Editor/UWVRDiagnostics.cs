using System;
using UnityEditor;
using UnityEngine;

/// <summary>Batchmode XR diagnostic that does not rely on an active headset.</summary>
public static class UWVRDiagnostics
{
    [MenuItem("Underworld/VR/Report Configuration")]
    public static void ReportConfiguration()
    {
        bool lOManagementInstalled = Type.GetType(
            "UnityEngine.XR.Management.XRGeneralSettings, Unity.XR.Management") != null;
        bool lOOpenXRInstalled = Type.GetType(
            "UnityEngine.XR.OpenXR.OpenXRLoader, Unity.XR.OpenXR") != null;

        Debug.Log("[UWVR] XR Plug-in Management assembly available: " + lOManagementInstalled);
        Debug.Log("[UWVR] OpenXR loader assembly available: " + lOOpenXRInstalled);
        Debug.Log("[UWVR] Active build target: " + EditorUserBuildSettings.activeBuildTarget);
        if (!lOManagementInstalled || !lOOpenXRInstalled)
            Debug.LogWarning("[UWVR] XR packages are not yet resolved. Check Package Manager and manifest.json.");
    }
}
