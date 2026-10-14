using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Pure VR coordinate conversions; usable by editor tests without starting an
/// OpenXR session. Dungeon tiles span 64 game units, while XR tracking is in
/// physical metres. Until the headset is available, unity-to-metre scale is
/// deliberately configurable rather than guessed from dungeon tile size.
/// </summary>
public static class UWVRWorldSpace
{
    public const float DefaultUnitsPerMeter = 1f;
    public const float MinUnitsPerMeter = 0.25f;
    public const float MaxUnitsPerMeter = 64f;
    public const string ScaleArgument = "-vr-units-per-meter=";
    public const float DefaultUiDistanceMeters = 2.25f;
    public const string UiDistanceArgument = "-vr-ui-distance=";
    public const float DefaultInventoryDistanceMeters = 1.8f;
    public const string InventoryDistanceArgument = "-vr-inventory-distance=";

    public static float ParseUnitsPerMeter(string[] pOArgs)
    {
        if (pOArgs == null)
            return DefaultUnitsPerMeter;

        foreach (string lsArg in pOArgs)
        {
            if (lsArg == null || !lsArg.StartsWith(ScaleArgument, StringComparison.OrdinalIgnoreCase))
                continue;

            string lsNumber = lsArg.Substring(ScaleArgument.Length);
            if (float.TryParse(lsNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float lfScale)
                && !float.IsNaN(lfScale) && !float.IsInfinity(lfScale)
                && lfScale >= MinUnitsPerMeter && lfScale <= MaxUnitsPerMeter)
                return lfScale;

            Debug.LogWarning("[UWVR] Invalid world scale option; using 1 game unit per metre: " + lsArg);
            return DefaultUnitsPerMeter;
        }

        return DefaultUnitsPerMeter;
    }

    /// <summary>
    /// VR menus are laid out using the old pixel coordinates, but must remain
    /// a comfortable physical distance away regardless of world-space scale.
    /// </summary>
    public static float ParseUiDistanceMeters(string[] pOArgs)
    {
        if (pOArgs != null)
            foreach (string lsArg in pOArgs)
            {
                if (lsArg == null || !lsArg.StartsWith(UiDistanceArgument, StringComparison.OrdinalIgnoreCase))
                    continue;

                string lsNumber = lsArg.Substring(UiDistanceArgument.Length);
                if (float.TryParse(lsNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float lfMeters)
                    && !float.IsNaN(lfMeters) && !float.IsInfinity(lfMeters)
                    && lfMeters >= 1.0f && lfMeters <= 5.0f)
                    return lfMeters;

                Debug.LogWarning("[UWVR] Invalid UI distance option; using 2.25 metres: " + lsArg);
                break;
            }

        return DefaultUiDistanceMeters;
    }

    /// <summary>
    /// The two modern inventory canvases can be placed on a closer stereo
    /// plane than the main HUD, while keeping all existing screen-space hit
    /// tests and moving them together to avoid depth conflicts.
    /// </summary>
    public static float ParseInventoryDistanceMeters(string[] pOArgs)
    {
        if (pOArgs != null)
            foreach (string lsArg in pOArgs)
            {
                if (lsArg == null || !lsArg.StartsWith(InventoryDistanceArgument, StringComparison.OrdinalIgnoreCase))
                    continue;

                string lsNumber = lsArg.Substring(InventoryDistanceArgument.Length);
                if (float.TryParse(lsNumber, NumberStyles.Float, CultureInfo.InvariantCulture, out float lfMeters)
                    && !float.IsNaN(lfMeters) && !float.IsInfinity(lfMeters)
                    && lfMeters >= 1f && lfMeters <= 5f)
                    return lfMeters;

                Debug.LogWarning("[UWVR] Invalid inventory distance; using 1.8 metres: " + lsArg);
                break;
            }

        return DefaultInventoryDistanceMeters;
    }

    /// <summary>
    /// Turn VR stick input (x=right,y=forward in the headset's yaw) into the
    /// player's body-local forward/right axes. Ignore pitch and roll so that
    /// looking down never makes the player walk downward or slow to a stop.
    /// </summary>
    public static Vector2 HeadRelativeStick(Vector2 pOStick, Quaternion pOBodyRotation, Vector3 pOHeadForward)
    {
        Vector3 lOFlatForward = Vector3.ProjectOnPlane(pOHeadForward, Vector3.up);
        if (lOFlatForward.sqrMagnitude < 0.0001f)
            return pOStick;

        lOFlatForward.Normalize();
        Vector3 lOLocalHeadForward = Quaternion.Inverse(pOBodyRotation) * lOFlatForward;
        float lfHeadYaw = Mathf.Atan2(lOLocalHeadForward.x, lOLocalHeadForward.z);
        float lfSin = Mathf.Sin(lfHeadYaw);
        float lfCos = Mathf.Cos(lfHeadYaw);

        return new Vector2(
            pOStick.x * lfCos + pOStick.y * lfSin,
            pOStick.y * lfCos - pOStick.x * lfSin);
    }
}
