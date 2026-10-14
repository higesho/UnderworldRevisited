using System;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Trial-only calibration of physical two-hand bow travel and combat vibration.
/// Values are in metres, never original dungeon world units. Invalid options
/// fall back to the previous fixed values, so a typo cannot make a bow fire
/// without drawing or send an unbounded XR impulse.
/// </summary>
public static class UWVRCombatCalibration
{
    public const string NockArgument = "-vr-bow-nock-radius=";
    public const string MinPullArgument = "-vr-bow-min-pull=";
    public const string FullPullArgument = "-vr-bow-full-pull=";
    public const string HapticScaleArgument = "-vr-haptic-scale=";

    public const float DefaultNockRadius = 0.46f;
    public const float DefaultMinPull = 0.20f;
    public const float DefaultFullPull = 0.48f;
    public const float DefaultHapticScale = 1f;

    public struct Settings
    {
        public float NockRadiusMetres;
        public float MinPullMetres;
        public float FullPullMetres;
        public float HapticScale;
    }

    public static Settings Defaults => new Settings
    {
        NockRadiusMetres = DefaultNockRadius,
        MinPullMetres = DefaultMinPull,
        FullPullMetres = DefaultFullPull,
        HapticScale = DefaultHapticScale
    };

    public static Settings Parse(string[] pOArgs, bool pbWarn = true)
    {
        Settings lOSettings = Defaults;
        lOSettings.NockRadiusMetres = ReadRange(pOArgs, NockArgument,
            DefaultNockRadius, 0.20f, 0.70f, pbWarn);
        lOSettings.MinPullMetres = ReadRange(pOArgs, MinPullArgument,
            DefaultMinPull, 0.10f, 0.32f, pbWarn);
        lOSettings.FullPullMetres = ReadRange(pOArgs, FullPullArgument,
            DefaultFullPull, 0.35f, 0.75f, pbWarn);
        lOSettings.HapticScale = ReadRange(pOArgs, HapticScaleArgument,
            DefaultHapticScale, 0f, 1.25f, pbWarn);

        if (lOSettings.FullPullMetres - lOSettings.MinPullMetres < 0.08f)
        {
            if (pbWarn)
                Debug.LogWarning("[UWVR-CAL] Full bow draw must exceed the minimum pull "
                    + "by at least 8cm. Reverting full draw to 0.48 metres.");
            lOSettings.FullPullMetres = DefaultFullPull;
        }

        return lOSettings;
    }

    private static float ReadRange(string[] pOArgs, string psArgument,
        float pfDefault, float pfMin, float pfMax, bool pbWarn)
    {
        if (pOArgs == null)
            return pfDefault;

        foreach (string lsArg in pOArgs)
        {
            if (lsArg == null || !lsArg.StartsWith(psArgument,
                StringComparison.OrdinalIgnoreCase))
                continue;

            string lsValue = lsArg.Substring(psArgument.Length);
            if (float.TryParse(lsValue, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float lfValue)
                && !float.IsNaN(lfValue) && !float.IsInfinity(lfValue)
                && lfValue >= pfMin && lfValue <= pfMax)
                return lfValue;

            if (pbWarn)
                Debug.LogWarning("[UWVR-CAL] Invalid VR combat option " + lsArg
                    + "; using the unchanged default " + pfDefault.ToString(
                        "0.00", CultureInfo.InvariantCulture) + ".");
            return pfDefault;
        }

        return pfDefault;
    }

    /// <summary>The physical controller receives a fraction [0,1] only.
    /// A user-supplied scale of zero disables combat impulses, while the
    /// upper limit stays conservative until a real Quest comfort trial.</summary>
    public static float ScaleAmplitude(float pfBase, float pfScale)
    {
        if (float.IsNaN(pfBase) || float.IsInfinity(pfBase)
            || float.IsNaN(pfScale) || float.IsInfinity(pfScale))
            return 0f;
        return Mathf.Clamp(pfBase * Mathf.Clamp(pfScale, 0f, 1.25f), 0f, 0.60f);
    }
}
