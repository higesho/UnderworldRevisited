using System;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Opt-in VR bootstrap. Pass -vr when launching a build. This deliberately does not
/// force-enable XR: OpenXR must be configured in Unity Player Settings first.
/// Non-VR launches are unaffected, including systems without any XR runtime.
/// </summary>
public sealed class UWVRBootstrap : MonoBehaviour
{
    private static bool msRequested;
    private static UWVRBootstrap msInstance;
    private bool mbReported;

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

    private void Update()
    {
        if (!msRequested || mbReported)
            return;

        if (XRSettings.isDeviceActive)
        {
            Debug.Log("[UWVR] XR headset active. Disabling legacy look input; validating rendering and UI is still required.");
            mbReported = true;
        }
    }

    private void OnDestroy()
    {
        if (msInstance == this)
            msInstance = null;
    }
}
