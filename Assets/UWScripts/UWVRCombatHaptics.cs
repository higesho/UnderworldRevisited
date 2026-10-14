using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Optional Quest/OpenXR haptic feedback for the existing two-hand bow and
/// left-hand spell projectile modes. No game mechanics or projectile/mana/
/// ammunition are modified here. XR pulse failure is normal on devices that
/// do not expose haptic impulses. Never send feedback when the inventory,
/// conversation or modal UI owns controller input.
/// </summary>
public static class UWVRCombatHaptics
{
    private static readonly UWVRBowHapticTimeline msBowTimeline = new UWVRBowHapticTimeline();
    private static float msLastLeftAt = float.NegativeInfinity;
    private static float msLastRightAt = float.NegativeInfinity;
    private static bool mbReportedLeft;
    private static bool mbReportedRight;

    public static bool IsEnabled => UWVRBootstrap.IsHeadsetActive
        && UWVRBootstrap.CombatHapticsRequested && UWVRHandLaunch.IsEnabled;

    public static void ResetBow()
    {
        msBowTimeline.Reset();
    }

    /// <summary>Called only while a real XR trigger is held and the bow's
    /// two-hand gesture is valid. The nock and at most four additional draw
    /// stages each request a very brief pulse on LEFT Touch.</summary>
    public static void BowSample(bool pbNocked, float pfDrawFraction)
    {
        if (!IsEnabled || !CanPlayInWorld() || !UWVRTwoHandBowDraw.IsEnabled)
        {
            ResetBow();
            return;
        }

        int liCue = msBowTimeline.Sample(pbNocked, pfDrawFraction);
        if (liCue > 0)
            TryPulse(XRNode.LeftHand, UWVRBowHapticTimeline.DrawAmplitude(liCue),
                UWVRBowHapticTimeline.DrawDuration(liCue));
    }

    /// <summary>Called ONLY after the original engine has created a bow
    /// projectile successfully. Rejected/no-ammo shots never rumble as hits.
    /// One short right-hand string snap is supplemented by a softer left cue.</summary>
    public static void BowProjectileLaunched()
    {
        ResetBow();
        if (!IsEnabled || !UWVRTwoHandBowDraw.IsEnabled)
            return;

        TryPulse(XRNode.RightHand, 0.46f, 0.062f);
        TryPulse(XRNode.LeftHand, 0.30f, 0.040f);
    }

    /// <summary>The same successful-spawn event as the existing 3D magic
    /// flash. This never changes spell cost, cooldown, damage or collisions.</summary>
    public static void SpellProjectileLaunched()
    {
        if (IsEnabled && UWVRHandLaunch.IsEnabled)
            TryPulse(XRNode.LeftHand, 0.40f, 0.070f);
    }

    private static bool CanPlayInWorld()
    {
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        return Application.isFocused && lOCamera != null
            && lOCamera.isActiveAndEnabled && UWScene.Player != null
            && UWVRSpatialInventory.VisiblePanel == null
            && !(UWModernBags.Instance != null && UWModernBags.Instance.IsOpen)
            && !UWConversationScreen.IsAnyOpen
            && (UWScene.ControlScheme == null
                || !UWScene.ControlScheme.IsWorldInputBlocked);
    }

    private static bool TryPulse(XRNode peHand, float pfAmplitude, float pfSeconds)
    {
        if (!IsEnabled || !CanPlayInWorld())
            return false;

        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        UWVRHeadTracking lOTracking = lOCamera != null
            ? lOCamera.GetComponent<UWVRHeadTracking>() : null;
        if (lOTracking == null || !lOTracking.TryGetControllerRay(peHand, out _))
            return false;

        bool lbLeft = peHand == XRNode.LeftHand;
        float lfNow = Time.unscaledTime;
        float lfLast = lbLeft ? msLastLeftAt : msLastRightAt;
        if (!UWVRBowHapticTimeline.CanPulse(lfNow, lfLast))
            return false;

        InputDevice lODevice = InputDevices.GetDeviceAtXRNode(peHand);
        if (!lODevice.isValid || !lODevice.TryGetHapticCapabilities(out HapticCapabilities lOHaptics)
            || !lOHaptics.supportsImpulse || lOHaptics.numChannels < 1)
            return false;

        bool lbSent = lODevice.SendHapticImpulse(0,
            Mathf.Clamp01(pfAmplitude), Mathf.Clamp(pfSeconds, 0.01f, 0.085f));
        if (!lbSent)
            return false;

        if (lbLeft)
        {
            msLastLeftAt = lfNow;
            if (!mbReportedLeft)
            {
                mbReportedLeft = true;
                Debug.Log("[UWVR-HAPTIC] left Touch accepts XR impulse requests.");
            }
        }
        else
        {
            msLastRightAt = lfNow;
            if (!mbReportedRight)
            {
                mbReportedRight = true;
                Debug.Log("[UWVR-HAPTIC] right Touch accepts XR impulse requests.");
            }
        }

        return true;
    }
}
