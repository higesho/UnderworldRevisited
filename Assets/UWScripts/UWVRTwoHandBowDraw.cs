using UnityEngine;

/// <summary>
/// Explicitly opt-in experimental bow nock and draw classifier for tracked
/// Touch hands. The right controller is the bow grip and aim; the left hand
/// pulls the imaginary string backwards along the right controller's forward
/// axis. The game's existing charge and ranged target threshold MUST still
/// pass before Interaction sends the original projectile on release.
/// No ammo is consumed here.
/// </summary>
public sealed class UWVRTwoHandBowDraw
{
    public const float NockRadiusMeters = 0.46f;
    public const float MinPullMeters = 0.20f;
    public const float FullPullMeters = 0.48f;
    public const float MaxLateralMeters = 0.48f;
    public const float MaxFrameJumpMeters = 0.34f;
    private const float MaxSampleSeconds = 0.11f;
    private const float MinForwardAlignment = 0.70f;

    private bool mbNocked;
    private bool mbRejected;
    private bool mbHasPrevious;
    private Vector3 mPreviousRight;
    private Vector3 mPreviousLeft;
    private Vector3 mForwardOnNock;
    private float mfDepthAtNock;
    private float mfDrawMeters;

    public static bool IsEnabled => UWVRBootstrap.IsHeadsetActive
        && UWVRBootstrap.TwoHandBowRequested && UWVRHandLaunch.IsEnabled;

    public bool IsNocked => mbNocked && !mbRejected;
    public float DrawMeters => IsNocked ? mfDrawMeters : 0f;
    public float DrawFraction => Mathf.InverseLerp(MinPullMeters,
        FullPullMeters, DrawMeters);

    public void Reset()
    {
        mbNocked = false;
        mbRejected = false;
        mbHasPrevious = false;
        mfDepthAtNock = 0f;
        mfDrawMeters = 0f;
    }

    /// <summary>All positions are calibrated Unity-world coordinates;
    /// convert to metres BEFORE evaluating gesture thresholds. For simplicity
    /// the player must begin with the left hand close to the right grip.
    /// Missing tracking / teleport invalidates the WHOLE held trigger; moving
    /// the hand back in later cannot silently reset the safety gate.</summary>
    public void Sample(bool pbBothTracked, Vector3 pORight, Vector3 pOLeft,
        Vector3 pORightForward, float pfUnitsPerMetre, float pfSeconds)
    {
        if (mbRejected)
            return;

        if (!pbBothTracked || !IsFinite(pORight) || !IsFinite(pOLeft)
            || !IsFinite(pORightForward) || pORightForward.sqrMagnitude < 0.01f
            || float.IsNaN(pfUnitsPerMetre) || float.IsInfinity(pfUnitsPerMetre)
            || pfUnitsPerMetre < UWVRWorldSpace.MinUnitsPerMeter
            || pfUnitsPerMetre > UWVRWorldSpace.MaxUnitsPerMeter
            || pfSeconds <= 0f || pfSeconds > MaxSampleSeconds)
        {
            Invalidate();
            return;
        }

        Vector3 lOForward = pORightForward.normalized;
        float lfDistance = Vector3.Distance(pORight, pOLeft) / pfUnitsPerMetre;
        Vector3 lORelative = (pORight - pOLeft) / pfUnitsPerMetre;
        float lfDepth = Vector3.Dot(lORelative, lOForward);
        float lfLateral = Vector3.ProjectOnPlane(lORelative, lOForward).magnitude;

        if (!mbNocked)
        {
            // Looking for an initial nocking position is permitted only
            // before we ever see a valid nock. Once tracked and armed, any
            // pose loss or unrealistic jump rejects the entire trigger hold.
            if (lfDistance > NockRadiusMeters || lfLateral > MaxLateralMeters
                || Mathf.Abs(lfDepth) > 0.17f)
                return;

            mbNocked = true;
            mbHasPrevious = true;
            mForwardOnNock = lOForward;
            mfDepthAtNock = lfDepth;
            mfDrawMeters = 0f;
            mPreviousRight = pORight;
            mPreviousLeft = pOLeft;
            return;
        }

        if (!mbHasPrevious || Vector3.Dot(mForwardOnNock, lOForward) < MinForwardAlignment
            || (pORight - mPreviousRight).magnitude / pfUnitsPerMetre > MaxFrameJumpMeters
            || (pOLeft - mPreviousLeft).magnitude / pfUnitsPerMetre > MaxFrameJumpMeters
            || lfLateral > MaxLateralMeters || lfDistance > 1.1f)
        {
            Invalidate();
            return;
        }

        mPreviousRight = pORight;
        mPreviousLeft = pOLeft;
        mfDrawMeters = Mathf.Max(0f, lfDepth - mfDepthAtNock);
    }

    public bool TryRelease(bool pbBothStillTracked, out float pfDrawFraction)
    {
        pfDrawFraction = DrawFraction;
        bool lbReady = pbBothStillTracked && !mbRejected && mbNocked
            && mfDrawMeters >= MinPullMeters;
        Reset();
        return lbReady;
    }

    private void Invalidate()
    {
        mbRejected = true;
        mbNocked = false;
        mbHasPrevious = false;
        mfDrawMeters = 0f;
    }

    private static bool IsFinite(Vector3 pOValue)
    {
        return !float.IsNaN(pOValue.x) && !float.IsInfinity(pOValue.x)
            && !float.IsNaN(pOValue.y) && !float.IsInfinity(pOValue.y)
            && !float.IsNaN(pOValue.z) && !float.IsInfinity(pOValue.z);
    }
}
