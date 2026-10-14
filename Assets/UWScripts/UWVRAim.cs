using UnityEngine;

/// <summary>
/// Shared world aim for gaze, right-handed combat and left-handed magic.
/// Desktop and default VR still use their original rays. Explicit
/// -vr-hand-aim changes ONLY combat/magic rays; look, use and UI remain gaze
/// or cursor based. The launch position stays at the original camera/player
/// except when the separate -vr-hand-origin prototype is explicitly enabled.
/// </summary>
public static class UWVRAim
{
    public static bool IsGazeActive => UWVRBootstrap.IsHeadsetActive;
    public static bool IsHandAimActive => UWVRBootstrap.IsHeadsetActive
        && UWVRBootstrap.HandAimRequested;

    /// <summary>Right Touch direction for swords and bows, from the original
    /// eye/actor origin. Real 6DoF physical attacks require a separate phase.</summary>
    public static Ray Combat(Camera pOCamera)
    {
        return FromHand(pOCamera, UnityEngine.XR.XRNode.RightHand);
    }

    /// <summary>Left Touch direction for launched magic and world target
    /// selection. Inventory-target spells still use the inventory UI.</summary>
    public static Ray Magic(Camera pOCamera)
    {
        return FromHand(pOCamera, UnityEngine.XR.XRNode.LeftHand);
    }

    private static Ray FromHand(Camera pOCamera, UnityEngine.XR.XRNode peHand)
    {
        Ray lOGaze = Forward(pOCamera);
        if (!IsHandAimActive || pOCamera == null)
            return lOGaze;

        UWVRHeadTracking lOTracking = pOCamera.GetComponent<UWVRHeadTracking>();
        Ray lOHand = default;
        bool lbTracked = lOTracking != null
            && lOTracking.TryGetControllerRay(peHand, out lOHand);
        return WithHandDirection(lOGaze, lbTracked ? lOHand : default, lbTracked);
    }

    /// <summary>Pure eye-origin / hand-direction composition. Tests use a
    /// simulated pose; no headset or Input System is needed. An untracked or
    /// invalid pose must fall back to the normal gaze ray.</summary>
    public static Ray WithHandDirection(Ray pOGaze, Ray pOHand, bool pbHandTracked)
    {
        Vector3 lODirection = pOHand.direction;
        if (!pbHandTracked || !HasValidDirection(lODirection))
            return pOGaze;

        return new Ray(pOGaze.origin, lODirection.normalized);
    }

    public static bool HasValidDirection(Vector3 pODirection)
    {
        return !float.IsNaN(pODirection.x) && !float.IsInfinity(pODirection.x)
            && !float.IsNaN(pODirection.y) && !float.IsInfinity(pODirection.y)
            && !float.IsNaN(pODirection.z) && !float.IsInfinity(pODirection.z)
            && pODirection.sqrMagnitude >= 0.0001f;
    }

    /// <summary>Positive above the horizon, negative below it. The melee
    /// attack variant can follow the right hand without changing damage.</summary>
    public static float PitchUpDegrees(Vector3 pODirection)
    {
        if (pODirection.sqrMagnitude < 0.0001f)
            return 0f;

        Vector3 lODirection = pODirection.normalized;
        return Mathf.Atan2(lODirection.y,
            Mathf.Sqrt(lODirection.x * lODirection.x + lODirection.z * lODirection.z))
            * Mathf.Rad2Deg;
    }

    public static Ray ScreenPoint(Camera pOCamera, Vector2 pOPoint)
    {
        if (pOCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        return IsGazeActive ? Forward(pOCamera) : pOCamera.ScreenPointToRay(pOPoint);
    }

    public static Ray Forward(Camera pOCamera)
    {
        if (pOCamera == null)
            return new Ray(Vector3.zero, Vector3.forward);

        // Forward from the tracked transform is unambiguous for both eyes,
        // avoiding dependence on a desktop viewport or on the mouse position.
        return IsGazeActive
            ? new Ray(pOCamera.transform.position, pOCamera.transform.forward)
            : pOCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
    }
}
