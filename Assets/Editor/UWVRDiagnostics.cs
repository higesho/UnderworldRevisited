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

        fRequire(UWVRWorldSpace.ParseUnitsPerMeter(null) == 1f,
            "VR default scale changed unexpectedly");
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseUnitsPerMeter(
            new[] { "UR.exe", "-vr-units-per-meter=16" }) - 16f) < 0.001f,
            "VR per-metre calibration argument was not parsed");
        fRequire(Mathf.Abs(UWVRWorldSpace.ParseUiDistanceMeters(
            new[] { "UR.exe", "-vr-ui-distance=3.5" }) - 3.5f) < 0.001f,
            "VR interface distance argument was not parsed");

        // Looking 90 degrees right: forward on the stick must become right
        // relative to the unturned body. Head pitch is explicitly ignored.
        Vector2 lORelative = UWVRWorldSpace.HeadRelativeStick(Vector2.up,
            Quaternion.identity, Quaternion.Euler(0f, 90f, 0f) * Vector3.forward);
        fRequire((lORelative - Vector2.right).sqrMagnitude < 0.0001f,
            "Head-relative locomotion maps yaw incorrectly");

        lORelative = UWVRWorldSpace.HeadRelativeStick(Vector2.up,
            Quaternion.Euler(0f, 90f, 0f), Quaternion.Euler(25f, 90f, 0f) * Vector3.forward);
        fRequire((lORelative - Vector2.up).sqrMagnitude < 0.0001f,
            "Head-relative locomotion does not ignore head pitch or body yaw");

        GameObject lOCameraObject = new GameObject("VR offline ray check", typeof(Camera));
        GameObject lOCanvasObject = new GameObject("VR offline canvas check", typeof(RectTransform), typeof(Canvas));
        GameObject lOReticleHost = new GameObject("VR offline reticle check");
        GameObject lOBody = new GameObject("VR offline avatar body");
        GameObject lOEye = new GameObject("VR offline avatar eye", typeof(Camera));
        try
        {
            lOBody.transform.position = new Vector3(16f, 3f, 8f);
            lOEye.transform.SetParent(lOBody.transform, false);
            lOEye.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            Vector3 lOOriginalEyePosition = lOEye.transform.position;

            UWVRHeadTracking lOTrackedEye = lOEye.AddComponent<UWVRHeadTracking>();
            MethodInfo lOCreateTrackingSpace = typeof(UWVRHeadTracking).GetMethod(
                "EnsureTrackingSpace", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo lORestoreTrackingSpace = typeof(UWVRHeadTracking).GetMethod(
                "RestoreTrackingSpace", BindingFlags.NonPublic | BindingFlags.Instance);
            fRequire(lOCreateTrackingSpace != null && lORestoreTrackingSpace != null,
                "VR tracking-space lifecycle methods unavailable");

            lOCreateTrackingSpace.Invoke(lOTrackedEye, new object[] { 16f });
            fRequire((lOEye.transform.position - lOOriginalEyePosition).sqrMagnitude < 0.0001f,
                "VR rig attachment moved the original eye anchor");
            fRequire(Mathf.Abs(lOEye.transform.lossyScale.x - 16f) < 0.0001f,
                "VR rig did not scale stereo camera transform");
            float lfScaledUi = UWVRCanvasBridge.CalculatePlaneDistance(
                lOEye.GetComponent<Camera>(), 2.25f);
            fRequire(Mathf.Abs(lfScaledUi - 36f) < 0.01f,
                "VR panels do not preserve a physical distance across tracking scale changes");

            lOEye.transform.localPosition = new Vector3(0.25f, 0f, 0f);
            fRequire(Mathf.Abs(lOEye.transform.position.x - lOOriginalEyePosition.x - 4f) < 0.0001f,
                "VR rig does not scale the physical HMD movement consistently");

            lORestoreTrackingSpace.Invoke(lOTrackedEye, null);
            fRequire(lOEye.transform.parent == lOBody.transform
                && (lOEye.transform.position - lOOriginalEyePosition).sqrMagnitude < 0.0001f,
                "VR rig does not restore desktop camera hierarchy and position");
            fRequire(Mathf.Abs(UWVRCanvasBridge.CalculatePlaneDistance(
                lOEye.GetComponent<Camera>(), 2.25f) - 2.25f) < 0.01f,
                "VR panel distance does not return to original units with the rig");

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
            UnityEngine.Object.DestroyImmediate(lOBody);
            UnityEngine.Object.DestroyImmediate(lOReticleHost);
            UnityEngine.Object.DestroyImmediate(lOCanvasObject);
            UnityEngine.Object.DestroyImmediate(lOCameraObject);
        }

        Debug.Log("[UWVR-OFFLINE] PASS: OpenXR setup, Touch, head-relative walking, XR scale/rig restoration, UI physical distance, stereo UI and cursor input pass-through.");
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
