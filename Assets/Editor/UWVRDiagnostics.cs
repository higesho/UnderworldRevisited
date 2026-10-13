using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.InputSystem;
using UnityEngine.UI;

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

    /// <summary>
    /// Checks VR project wiring and verifies the non-VR ray fallback without
    /// loading a headset, entering Play mode or modifying game scene assets.
    /// For batchmode: -executeMethod UWVRDiagnostics.RunOfflineChecks
    /// </summary>
    [MenuItem("Underworld/VR/Run Offline Checks")]
    public static void RunOfflineChecks()
    {
        XRGeneralSettings lOXR = XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        fRequire(lOXR != null && lOXR.Manager != null, "Standalone XR manager asset missing");
        fRequire(!lOXR.InitManagerOnStart, "XR must stay off in non-VR launches");

        bool lbLoader = false;
        foreach (XRLoader lOLoader in lOXR.Manager.activeLoaders)
            if (lOLoader is OpenXRLoader)
                lbLoader = true;
        fRequire(lbLoader, "Windows Standalone OpenXR loader not assigned");

        OpenXRSettings lOOpenXR = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        fRequire(lOOpenXR != null, "OpenXR build settings missing");

        bool lbQuestController = false;
        foreach (var lOFeature in lOOpenXR.GetFeatures<OculusTouchControllerProfile>())
            if (lOFeature is OculusTouchControllerProfile lOTouch && lOTouch.enabled)
                lbQuestController = true;
        fRequire(lbQuestController, "Oculus Touch interaction profile not enabled");

        UWControls lOControls = new UWControls();
        try
        {
            fRequire(fHasBinding(lOControls.Player.Move, "<XRController>{LeftHand}/thumbstick"),
                "Quest left hand movement binding missing");
            fRequire(fHasBinding(lOControls.Player.LookStick, "<XRController>{RightHand}/thumbstick"),
                "Quest right hand turning binding missing");
            fRequire(fHasBinding(lOControls.Player.ModernAttack, "<XRController>{RightHand}/triggerPressed"),
                "Quest trigger attack binding missing");
            fRequire(fHasBinding(lOControls.Player.ModernUse, "<XRController>{RightHand}/primaryButton"),
                "Quest action button binding missing");
            fRequire(fHasBinding(lOControls.Player.PadClick, "<XRController>{RightHand}/primaryButton"),
                "Quest menu primary click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadClick, "<XRController>{RightHand}/triggerPressed"),
                "Quest menu trigger click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadContext, "<XRController>{LeftHand}/gripPressed"),
                "Quest menu context click binding missing");
            fRequire(fHasBinding(lOControls.Player.PadPointer, "<XRController>{RightHand}/thumbstickClicked"),
                "Quest pointer toggle binding missing");
        }
        finally
        {
            lOControls.Dispose();
        }

        fRequire(!UWVRBootstrap.IsHeadsetActive, "Offline checks require no active XR session");

        GameObject lOCameraObject = new GameObject("VR offline ray check", typeof(Camera));
        GameObject lOCanvasObject = new GameObject("VR offline canvas check", typeof(RectTransform), typeof(Canvas));
        GameObject lOReticleHost = new GameObject("VR offline reticle check");
        try
        {
            Camera lOCamera = lOCameraObject.GetComponent<Camera>();
            lOCamera.pixelRect = new Rect(0f, 0f, 800f, 600f);
            lOCamera.transform.position = new Vector3(1f, 2f, 3f);
            lOCamera.transform.rotation = Quaternion.Euler(-20f, 35f, 0f);

            Vector2 lOPoint = new Vector2(173f, 449f);
            Ray lOOriginal = lOCamera.ScreenPointToRay(lOPoint);
            Ray lOVRCompatible = UWVRAim.ScreenPoint(lOCamera, lOPoint);
            fRequire(fRayMatches(lOOriginal, lOVRCompatible),
                "Desktop ScreenPointToRay compatibility has changed");

            lOOriginal = lOCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            lOVRCompatible = UWVRAim.Forward(lOCamera);
            fRequire(fRayMatches(lOOriginal, lOVRCompatible),
                "Desktop viewport center ray compatibility has changed");

            Canvas lOCanvas = lOCanvasObject.GetComponent<Canvas>();
            lOCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lOCanvas.sortingOrder = 42;
            UWVRCanvasBridge.ConfigureCanvas(lOCanvas, lOCamera);
            fRequire(lOCanvas.renderMode == RenderMode.ScreenSpaceCamera,
                "VR canvas must use a stereo-compatible camera rendering mode");
            fRequire(lOCanvas.worldCamera == lOCamera && lOCanvas.sortingOrder == 42,
                "VR canvas must retain its original sorting and camera link");
            fRequire(lOCanvas.planeDistance > lOCamera.nearClipPlane
                && lOCanvas.planeDistance < lOCamera.farClipPlane,
                "VR canvas distance outside the active camera clipping range");

            UWVRPointerReticle lOReticle = lOReticleHost.AddComponent<UWVRPointerReticle>();
            MethodInfo lOCreate = typeof(UWVRPointerReticle).GetMethod("CreateCanvas",
                BindingFlags.Instance | BindingFlags.NonPublic);
            fRequire(lOCreate != null, "VR cursor creator missing");
            lOCreate.Invoke(lOReticle, null);

            Canvas lOReticleCanvas = lOReticleHost.GetComponentInChildren<Canvas>(true);
            fRequire(lOReticleCanvas != null && lOReticleCanvas.sortingOrder == 32000,
                "VR cursor canvas not created at the UI foreground");
            Image[] lOMarks = lOReticleHost.GetComponentsInChildren<Image>(true);
            fRequire(lOMarks.Length == 3, "VR cursor geometry missing");
            foreach (Image lOMark in lOMarks)
                fRequire(!lOMark.raycastTarget, "VR cursor must not block input to menus");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(lOReticleHost);
            UnityEngine.Object.DestroyImmediate(lOCanvasObject);
            UnityEngine.Object.DestroyImmediate(lOCameraObject);
        }

        Debug.Log("[UWVR-OFFLINE] PASS: OpenXR loader, manual startup, Touch bindings, desktop rays, VR canvas projection, reticle input pass-through.");
        Debug.Log("[UWVR-OFFLINE] HMD stereo output, head tracking and UI visibility still require an actual headset.");
    }

    private static bool fRayMatches(Ray pOA, Ray pOB)
    {
        return (pOA.origin - pOB.origin).sqrMagnitude < 0.000001f
            && (pOA.direction - pOB.direction).sqrMagnitude < 0.000001f;
    }

    private static bool fHasBinding(InputAction pOAction, string psPath)
    {
        if (pOAction == null)
            return false;

        foreach (InputBinding lOBinding in pOAction.bindings)
            if (string.Equals(lOBinding.path, psPath, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }

    private static void fRequire(bool pbCondition, string psReason)
    {
        if (!pbCondition)
            throw new InvalidOperationException("[UWVR-OFFLINE] FAIL: " + psReason);
    }
}
