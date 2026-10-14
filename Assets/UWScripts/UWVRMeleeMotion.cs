using UnityEngine;

/// <summary>
/// Head-relative, metre-based right Touch motion classifier for opt-in
/// -vr-gesture-melee. This is not collision-driven sword combat: a recognised
/// slash/hack/stab authorises the EXISTING charged attack and animation to
/// deal damage using the original game's equipment, reach and damage rules.
/// The trigger must be held to arm it and released to commit the gesture.
/// Pure deterministic state machine; offline tests can submit synthetic poses.
/// </summary>
public sealed class UWVRMeleeMotion
{
    public enum Strike { None, Slash, Hack, Stab }

    public const float MinTravelMeters = 0.14f;
    public const float MinSpeedMetersPerSecond = 0.70f;
    public const float MaxStepMeters = 0.40f;
    public const float MaxSpeedMetersPerSecond = 6.0f;
    public const float WindowSeconds = 0.26f;
    public const float GestureFreshSeconds = 0.55f;

    private bool mbHasSample;
    private Vector3 mWindowStart;
    private Vector3 mLastPosition;
    private float mfWindowAge;
    private float mfGestureAge;
    private Strike meCandidate;
    private float mfCandidatePower;

    public Strike Candidate => meCandidate;

    public static bool IsEnabled => UWVRAim.IsHandAimActive
        && UWVRBootstrap.GestureMeleeRequested;

    public void Reset()
    {
        mbHasSample = false;
        mWindowStart = Vector3.zero;
        mLastPosition = Vector3.zero;
        mfWindowAge = 0f;
        mfGestureAge = 0f;
        meCandidate = Strike.None;
        mfCandidatePower = 0f;
    }

    public void Sample(bool pbTracked, Vector3 pOHandMeters,
        Vector3 pOForward, float pfSeconds)
    {
        if (!pbTracked || !IsFinite(pOHandMeters) || !IsFinite(pOForward)
            || pOForward.sqrMagnitude < 0.1f || pfSeconds <= 0f || pfSeconds > 0.1f)
        {
            // Invalid tracking MUST NOT keep a previously earned strike.
            Reset();
            return;
        }

        if (!mbHasSample)
        {
            mbHasSample = true;
            mWindowStart = pOHandMeters;
            mLastPosition = pOHandMeters;
            return;
        }

        Vector3 lOStep = pOHandMeters - mLastPosition;
        float lfStep = lOStep.magnitude;
        float lfSpeed = lfStep / pfSeconds;
        if (lfStep > MaxStepMeters || lfSpeed > MaxSpeedMetersPerSecond)
        {
            // A tracking discontinuity should cancel the entire swing, not
            // create a high-power hit from controller teleportation.
            Reset();
            return;
        }

        mLastPosition = pOHandMeters;
        mfWindowAge += pfSeconds;
        mfGestureAge += pfSeconds;
        if (meCandidate != Strike.None && mfGestureAge > GestureFreshSeconds)
        {
            meCandidate = Strike.None;
            mfCandidatePower = 0f;
        }

        if (mfWindowAge > WindowSeconds)
        {
            mWindowStart = pOHandMeters - lOStep;
            mfWindowAge = pfSeconds;
        }

        Vector3 lODisplacement = pOHandMeters - mWindowStart;
        if (lODisplacement.magnitude < MinTravelMeters || lfSpeed < MinSpeedMetersPerSecond)
            return;

        Strike leStrike = Classify(lODisplacement, pOForward);
        if (leStrike == Strike.None)
            return;

        meCandidate = leStrike;
        mfGestureAge = 0f;
        mfCandidatePower = Mathf.Clamp01((lfSpeed - MinSpeedMetersPerSecond) / 2.0f);
        // Begin a new displacement window. The same long sweep should not
        // repeatedly re-arm attacks without another distinct movement.
        mWindowStart = pOHandMeters;
        mfWindowAge = 0f;
    }

    public bool TryFinish(bool pbTrackedAtRelease, out Strike peStrike, out float pfChargeLimit)
    {
        peStrike = meCandidate;
        pfChargeLimit = Mathf.Lerp(0.40f, 1f, mfCandidatePower);
        bool lbValid = pbTrackedAtRelease && peStrike != Strike.None
            && mfGestureAge <= GestureFreshSeconds;
        Reset();
        if (!lbValid)
        {
            peStrike = Strike.None;
            pfChargeLimit = 0f;
        }
        return lbValid;
    }

    public static Strike Classify(Vector3 pODisplacement, Vector3 pOForward)
    {
        if (!IsFinite(pODisplacement) || !IsFinite(pOForward)
            || pODisplacement.magnitude < MinTravelMeters || pOForward.sqrMagnitude < 0.1f)
            return Strike.None;

        float lfForward = Vector3.Dot(pODisplacement, pOForward.normalized);
        if (lfForward >= MinTravelMeters && lfForward >= pODisplacement.magnitude * 0.68f)
            return Strike.Stab;

        if (-pODisplacement.y >= MinTravelMeters
            && -pODisplacement.y >= Mathf.Abs(pODisplacement.x) * 1.1f)
            return Strike.Hack;

        if (Mathf.Abs(pODisplacement.x) >= MinTravelMeters
            && Mathf.Abs(pODisplacement.x) >= Mathf.Abs(pODisplacement.y) * 0.7f)
            return Strike.Slash;

        return Strike.None;
    }

    private static bool IsFinite(Vector3 pOVector)
    {
        return !float.IsNaN(pOVector.x) && !float.IsInfinity(pOVector.x)
            && !float.IsNaN(pOVector.y) && !float.IsInfinity(pOVector.y)
            && !float.IsNaN(pOVector.z) && !float.IsInfinity(pOVector.z);
    }
}
