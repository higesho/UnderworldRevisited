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

    public static bool IsRequested => msRequested;
    public static bool IsHeadsetActive => msRequested && XRSettings.isDeviceActive;

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
            return;

        if (!mbReported)
        {
            Debug.Log("[UWVR] XR headset active; legacy mouse look disabled. VR UI and controller interaction remain incomplete.");
            mbReported = true;
        }

        if (mTrackedCamera != null)
            return;

        // The player is not necessarily present on the main menu. Attach the
        // tracking component only to the actual player camera when it appears.
        GameObject lOPlayer = UWScene.Player;
        if (lOPlayer == null)
            return;

        Camera lOCamera = lOPlayer.GetComponentInChildren<Camera>(true);
        if (lOCamera == null)
            return;

        if (lOCamera.GetComponent<UWVRHeadTracking>() == null)
            lOCamera.gameObject.AddComponent<UWVRHeadTracking>();

        mTrackedCamera = lOCamera;
        Debug.Log("[UWVR] Head pose tracking attached to player camera.");
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
