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
    private static UWVRBootstrap msInstance;
    private bool mbReported;
    private bool mbStartedSubsystems;
    private Camera mTrackedCamera;
    private Camera mMenuCamera;
    private Camera mCurrentVRCamera;

    public static bool IsRequested => msRequested;
    public static bool IsHeadsetActive => msRequested && XRSettings.isDeviceActive;
    public static Camera CurrentVRCamera => msInstance != null ? msInstance.mCurrentVRCamera : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        msRequested = Array.Exists(Environment.GetCommandLineArgs(),
            pSArg => string.Equals(pSArg, "-vr", StringComparison.OrdinalIgnoreCase));

        if (!msRequested || msInstance != null)
            return;

        GameObject lOHost = new GameObject("UW VR Bootstrap");
        DontDestroyOnLoad(lOHost);
        msInstance = lOHost.AddComponent<UWVRBootstrap>();
        lOHost.AddComponent<UWVRCanvasBridge>();
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
            GameObject lOMenu = new GameObject("UW VR Menu Camera", typeof(Camera),
                typeof(AudioListener), typeof(UWVRHeadTracking));
            lOMenu.transform.SetParent(transform, false);
            mMenuCamera = lOMenu.GetComponent<Camera>();
            mMenuCamera.clearFlags = CameraClearFlags.SolidColor;
            mMenuCamera.backgroundColor = Color.black;
            // Include UI layers: ScreenSpaceCamera canvas objects must not be
            // removed by the camera's culling mask.
            mMenuCamera.cullingMask = ~0;
            mMenuCamera.depth = -100f;
            mMenuCamera.nearClipPlane = 0.05f;
            mMenuCamera.farClipPlane = 20f;
            mMenuCamera.stereoTargetEye = StereoTargetEyeMask.Both;
            Debug.Log("[UWVR] Created temporary stereo camera for the main menu.");
        }

        if (!mMenuCamera.gameObject.activeSelf)
            mMenuCamera.gameObject.SetActive(true);
        mCurrentVRCamera = mMenuCamera;
    }

    private void OnDestroy()
    {
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
