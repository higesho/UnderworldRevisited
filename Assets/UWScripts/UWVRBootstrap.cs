using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Management;

/// <summary>
/// Opt-in VR bootstrap. Pass -vr when launching a build. This deliberately does not
/// initialize XR in the normal desktop mode: XR initialization is manual and
/// only attempted after Start when -vr was explicitly passed.
/// Non-VR launches are unaffected, including systems without any XR runtime.
/// </summary>
public sealed class UWVRBootstrap : MonoBehaviour
{
    private static bool msRequested;
    private static float msWorldUnitsPerMeter = UWVRWorldSpace.DefaultUnitsPerMeter;
    private static float msUiDistanceMeters = UWVRWorldSpace.DefaultUiDistanceMeters;
    private static float msMenuDistanceMeters = UWVRWorldSpace.DefaultMenuDistanceMeters;
    private static float msInventoryDistanceMeters = UWVRWorldSpace.DefaultInventoryDistanceMeters;
    private static bool msSpatialInventoryRequested;
    private static bool msNearGrabRequested;
    private static bool msDirectGrabRequested;
    private static bool msHandAimRequested;
    private static bool msGestureMeleeRequested;
    private static bool msBladeContactRequested;
    private static bool msHandOriginRequested;
    private static bool msTwoHandBowRequested;
    private static bool msHandMagicFxRequested;
    private static bool msCombatHapticsRequested;
    private static bool msCombatDiagnosticsRequested;
    private static bool msLegacyMenuCanvasRequested;
    private static bool msLeftEyeShiftRequested;
    private static UWVRCombatCalibration.Settings msCombatCalibration = UWVRCombatCalibration.Defaults;
    private static UWVRBootstrap msInstance;
    private bool mbReported;
    private bool mbStartedSubsystems;
    private Camera mTrackedCamera;
    private Camera mMenuCamera;
    private AudioListener mMenuListener;
    private Camera mCurrentVRCamera;
    private bool mbOriginalRunInBackground;
    private bool mbReportedCameras;

    private void Awake()
    {
        // Virtual Desktop can move the Windows focus to its streamer while
        // the Quest remains the active XR session. Keep HMD input + menu
        // updates alive; this applies ONLY to explicitly requested VR mode.
        mbOriginalRunInBackground = Application.runInBackground;
        if (msRequested)
            Application.runInBackground = true;
    }

    public static bool IsRequested => msRequested;
    public static float WorldUnitsPerMeter => msWorldUnitsPerMeter;
    public static float UiDistanceMeters => msUiDistanceMeters;
    public static float MenuDistanceMeters => msMenuDistanceMeters;
    public static float InventoryDistanceMeters => msInventoryDistanceMeters;
    public static bool SpatialInventoryRequested => msSpatialInventoryRequested;
    public static bool NearGrabRequested => msNearGrabRequested;
    public static bool DirectGrabRequested => msDirectGrabRequested;
    public static bool HandAimRequested => msHandAimRequested;
    public static bool GestureMeleeRequested => msGestureMeleeRequested;
    public static bool BladeContactRequested => msBladeContactRequested;
    public static bool HandOriginRequested => msHandOriginRequested;
    public static bool TwoHandBowRequested => msTwoHandBowRequested;
    public static bool HandMagicFxRequested => msHandMagicFxRequested;
    public static bool CombatHapticsRequested => msCombatHapticsRequested;
    public static bool CombatDiagnosticsRequested => msCombatDiagnosticsRequested;
    public static bool LegacyMenuCanvasRequested => msLegacyMenuCanvasRequested;
    public static bool LeftEyeShiftRequested => msLeftEyeShiftRequested;
    public static UWVRCombatCalibration.Settings CombatCalibration => msCombatCalibration;
    public static bool HeadRelativeMovement { get; private set; } = true;
    public static bool IsHeadsetActive => msRequested && XRSettings.isDeviceActive;
    public static Camera CurrentVRCamera => msInstance != null ? msInstance.mCurrentVRCamera : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        string[] lOArgs = Environment.GetCommandLineArgs();
        msRequested = Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr", StringComparison.OrdinalIgnoreCase));

        msWorldUnitsPerMeter = UWVRWorldSpace.DefaultUnitsPerMeter;
        msUiDistanceMeters = UWVRWorldSpace.DefaultUiDistanceMeters;
        msMenuDistanceMeters = UWVRWorldSpace.DefaultMenuDistanceMeters;
        msInventoryDistanceMeters = UWVRWorldSpace.DefaultInventoryDistanceMeters;
        msSpatialInventoryRequested = false;
        msNearGrabRequested = false;
        msDirectGrabRequested = false;
        msHandAimRequested = false;
        msGestureMeleeRequested = false;
        msBladeContactRequested = false;
        msHandOriginRequested = false;
        msTwoHandBowRequested = false;
        msHandMagicFxRequested = false;
        msCombatHapticsRequested = false;
        msCombatDiagnosticsRequested = false;
        msLegacyMenuCanvasRequested = false;
        msLeftEyeShiftRequested = false;
        msCombatCalibration = UWVRCombatCalibration.Defaults;
        HeadRelativeMovement = true;

        if (!msRequested || msInstance != null)
            return;

        msWorldUnitsPerMeter = UWVRWorldSpace.ParseUnitsPerMeter(lOArgs);
        msUiDistanceMeters = UWVRWorldSpace.ParseUiDistanceMeters(lOArgs);
        msMenuDistanceMeters = UWVRWorldSpace.ParseMenuDistanceMeters(lOArgs);
        msInventoryDistanceMeters = UWVRWorldSpace.ParseInventoryDistanceMeters(lOArgs);
        msSpatialInventoryRequested = Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-spatial-inventory", StringComparison.OrdinalIgnoreCase));
        msNearGrabRequested = msSpatialInventoryRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-near-grab", StringComparison.OrdinalIgnoreCase));
        msDirectGrabRequested = msNearGrabRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-direct-grab", StringComparison.OrdinalIgnoreCase));
        msHandAimRequested = Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-hand-aim", StringComparison.OrdinalIgnoreCase));
        msGestureMeleeRequested = msHandAimRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-gesture-melee", StringComparison.OrdinalIgnoreCase));
        msBladeContactRequested = msGestureMeleeRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-blade-contact", StringComparison.OrdinalIgnoreCase));
        msHandOriginRequested = msHandAimRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-hand-origin", StringComparison.OrdinalIgnoreCase));
        msTwoHandBowRequested = msHandOriginRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-two-hand-bow", StringComparison.OrdinalIgnoreCase));
        msHandMagicFxRequested = msHandOriginRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-hand-magic-fx", StringComparison.OrdinalIgnoreCase));
        msCombatHapticsRequested = msHandOriginRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-combat-haptics", StringComparison.OrdinalIgnoreCase));
        msCombatDiagnosticsRequested = msTwoHandBowRequested && Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-combat-diagnostics", StringComparison.OrdinalIgnoreCase));
        msLegacyMenuCanvasRequested = Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-legacy-menu-canvas", StringComparison.OrdinalIgnoreCase));
        msLeftEyeShiftRequested = Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-left-eye-shift", StringComparison.OrdinalIgnoreCase));
        msCombatCalibration = UWVRCombatCalibration.Parse(lOArgs);
        HeadRelativeMovement = !Array.Exists(lOArgs,
            pSArg => string.Equals(pSArg, "-vr-body-relative", StringComparison.OrdinalIgnoreCase));

        Debug.Log("[UWVR] Spatial calibration: " + msWorldUnitsPerMeter
            + " game units per metre; walking relative to "
            + (HeadRelativeMovement ? "head" : "body") + "; UI distance "
            + msUiDistanceMeters + " metres; 2D menu distance "
            + msMenuDistanceMeters + " metres; inventory "
            + msInventoryDistanceMeters + " metres; spatial inventory "
            + (msSpatialInventoryRequested ? "enabled" : "off")
            + "; near grab " + (msNearGrabRequested ? "enabled" : "off")
            + "; direct grab " + (msDirectGrabRequested ? "enabled" : "off")
            + "; hand aim " + (msHandAimRequested ? "enabled" : "off")
            + "; gesture melee " + (msGestureMeleeRequested ? "enabled" : "off")
            + "; blade contact " + (msBladeContactRequested ? "enabled" : "off")
            + "; hand origin " + (msHandOriginRequested ? "enabled" : "off")
            + "; two-hand bow " + (msTwoHandBowRequested ? "enabled" : "off")
            + "; hand magic visuals " + (msHandMagicFxRequested ? "enabled" : "off")
            + "; combat haptics " + (msCombatHapticsRequested ? "enabled" : "off")
            + "; combat diagnostics " + (msCombatDiagnosticsRequested ? "enabled" : "off") + ".");
        Debug.Log("[UWVR-MENU] Title canvas: " + (msLegacyMenuCanvasRequested
            ? "legacy ScreenSpaceCamera diagnostic comparison"
            : "single world-space sheet for both XR eyes") + ".");
        if (msLeftEyeShiftRequested)
            Debug.Log("[UWVR-EYE-SHIFT] Per-eye framebuffer diagnostic requested; "
                + "RIGHT unchanged, LEFT horizontal shift only, Left/Right arrows +/-4px, F6=reset.");
        Debug.Log("[UWVR-CAL] bow nock=" + msCombatCalibration.NockRadiusMetres.ToString("0.00")
            + "m; minimum pull=" + msCombatCalibration.MinPullMetres.ToString("0.00")
            + "m; full draw=" + msCombatCalibration.FullPullMetres.ToString("0.00")
            + "m; haptic multiplier=" + msCombatCalibration.HapticScale.ToString("0.00") + ".");

        GameObject lOHost = new GameObject("UW VR Bootstrap");
        DontDestroyOnLoad(lOHost);
        msInstance = lOHost.AddComponent<UWVRBootstrap>();
        lOHost.AddComponent<UWVRHandAimGuides>();
        lOHost.AddComponent<UWVRHandWeaponVisual>();
        lOHost.AddComponent<UWVRBowVisual>();
        lOHost.AddComponent<UWVRMagicHandFx>();
        lOHost.AddComponent<UWVRSpatialInventory>();
        lOHost.AddComponent<UWVRInventoryTokens>();
        lOHost.AddComponent<UWVRNearGrab>();
        lOHost.AddComponent<UWVRCanvasBridge>();
        lOHost.AddComponent<UWVRPointerReticle>();
        if (msLeftEyeShiftRequested)
            lOHost.AddComponent<UWVRLeftEyeShift>();
    }

    private IEnumerator Start()
    {
        // Unity's graphics initialization must be finished before manually
        // initializing an XR loader (XR Plug-in Management requirement).
        yield return null;

        XRGeneralSettings lOSettings = XRGeneralSettings.Instance;
        XRManagerSettings lOManager = lOSettings != null ? lOSettings.Manager : null;
        if (lOManager == null)
        {
            Debug.LogError("[UWVR] XR manager missing. Run Underworld/VR/Configure Windows OpenXR in the Unity Editor.");
            yield break;
        }

        yield return lOManager.InitializeLoader();

        if (lOManager.activeLoader == null)
        {
            Debug.LogError("[UWVR] OpenXR could not start. Check the active Windows OpenXR runtime and headset connection.");
            yield break;
        }

        lOManager.StartSubsystems();
        mbStartedSubsystems = true;
        Debug.Log("[UWVR] OpenXR subsystems started (loader: " + lOManager.activeLoader.name + ").");
    }

    private void Update()
    {
        if (!IsHeadsetActive)
        {
            mCurrentVRCamera = null;
            if (mMenuCamera != null)
                mMenuCamera.gameObject.SetActive(false);
            return;
        }

        if (!mbReported)
        {
            Debug.Log("[UWVR] XR headset active; legacy mouse look disabled. VR UI and controller interaction remain incomplete.");
            Debug.Log("[UWVR] Eye textures " + XRSettings.eyeTextureWidth + " x "
                + XRSettings.eyeTextureHeight + "; stereo rendering: " + XRSettings.stereoRenderingMode);
            mbReported = true;
        }

        if (!mbReportedCameras && UWMainMenu.IsOpen)
        {
            mbReportedCameras = true;
            foreach (Camera lODiagnosticCamera in UnityEngine.Object.FindObjectsByType<Camera>(
                FindObjectsInactive.Exclude))
            {
                if (!lODiagnosticCamera.enabled)
                    continue;
                Debug.Log("[UWVR-STEREO] active camera=" + lODiagnosticCamera.name
                    + "; stereo eye=" + lODiagnosticCamera.stereoTargetEye
                    + "; depth=" + lODiagnosticCamera.depth
                    + "; culling=" + lODiagnosticCamera.cullingMask);
            }
        }

        // The player is not necessarily present on the main menu. Attach the
        // tracking component only to the actual player camera when it appears.
        GameObject lOPlayer = UWScene.Player;
        Camera lOCamera = lOPlayer != null ? lOPlayer.GetComponentInChildren<Camera>(true) : null;

        if (lOCamera != null && lOCamera.isActiveAndEnabled)
        {
            if (lOPlayer.GetComponent<UWVRComfortTurn>() == null)
                lOPlayer.AddComponent<UWVRComfortTurn>();

            if (mTrackedCamera != lOCamera)
            {
                if (lOCamera.GetComponent<UWVRHeadTracking>() == null)
                    lOCamera.gameObject.AddComponent<UWVRHeadTracking>();
                mTrackedCamera = lOCamera;
                Debug.Log("[UWVR] Head pose tracking attached to player camera.");
            }

            mCurrentVRCamera = lOCamera;
            if (mMenuCamera != null)
                mMenuCamera.gameObject.SetActive(false);
            return;
        }

        // The original main menu is an overlay and may have no player camera.
        // Supply a stereo camera so it can be displayed before the world loads.
        if (mMenuCamera == null)
        {
            // The tracking component must be added *after* parenting the menu
            // camera. Otherwise its OnEnable creates a rig at the scene root,
            // and the later SetParent silently detaches the camera from it.
            GameObject lOMenu = new GameObject("UW VR Menu Camera", typeof(Camera));
            lOMenu.transform.SetParent(transform, false);
            mMenuCamera = lOMenu.GetComponent<Camera>();
            mMenuListener = lOMenu.AddComponent<AudioListener>();
            mMenuListener.enabled = false;
            mMenuCamera.clearFlags = CameraClearFlags.SolidColor;
            mMenuCamera.backgroundColor = Color.black;
            // Include UI layers: ScreenSpaceCamera canvas objects must not be
            // removed by the camera's culling mask.
            mMenuCamera.cullingMask = ~0;
            mMenuCamera.depth = -100f;
            mMenuCamera.nearClipPlane = 0.05f;
            mMenuCamera.farClipPlane = 20f;
            mMenuCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            lOMenu.AddComponent<UWVRHeadTracking>();
            Debug.Log("[UWVR] Created temporary stereo camera for the main menu.");
        }

        if (!mMenuCamera.gameObject.activeSelf)
            mMenuCamera.gameObject.SetActive(true);
        // Only one active AudioListener is allowed. The normal menu can
        // already contain one; never add a second active audio sink.
        bool lbOtherListener = false;
        foreach (AudioListener lOListener in UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            if (lOListener != mMenuListener && lOListener.enabled)
            {
                lbOtherListener = true;
                break;
            }
        }
        if (mMenuListener != null)
            mMenuListener.enabled = !lbOtherListener;
        mCurrentVRCamera = mMenuCamera;
    }

    private void OnDestroy()
    {
        Application.runInBackground = mbOriginalRunInBackground;
        if (mbStartedSubsystems)
        {
            XRManagerSettings lOManager = XRGeneralSettings.Instance != null
                ? XRGeneralSettings.Instance.Manager : null;
            if (lOManager != null)
            {
                lOManager.StopSubsystems();
                lOManager.DeinitializeLoader();
            }
        }

        if (msInstance == this)
            msInstance = null;
    }
}
