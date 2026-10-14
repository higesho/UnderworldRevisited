using UnityEngine;

/// <summary>
/// Hardware-independent cue scheduler for the experimental VR bowstring.
/// Stage 1 means nocking, stages 2-5 mean crossing increasing draw milestones.
/// Returning 0 means no new cue. It never fires ammunition or XR commands.
/// Reset on release, tracking loss, modal UI or sheathing the weapon.
/// </summary>
public sealed class UWVRBowHapticTimeline
{
    private bool mbNocked;
    private int miLastMilestone;

    public bool HasNock => mbNocked;

    public void Reset()
    {
        mbNocked = false;
        miLastMilestone = 0;
    }

    public int Sample(bool pbTrackedNock, float pfDrawFraction)
    {
        if (!pbTrackedNock || float.IsNaN(pfDrawFraction)
            || float.IsInfinity(pfDrawFraction))
        {
            Reset();
            return 0;
        }

        if (!mbNocked)
        {
            mbNocked = true;
            miLastMilestone = DrawMilestone(pfDrawFraction);
            return 1;
        }

        int liMilestone = DrawMilestone(pfDrawFraction);
        if (liMilestone <= miLastMilestone)
            return 0;

        // A single rendered frame may cross multiple steps. Emit at most ONE
        // cue for that frame and remember the highest step, not every skipped
        // step. A fast pull therefore cannot queue several simultaneous buzzes.
        miLastMilestone = liMilestone;
        return liMilestone + 1;
    }

    public static int DrawMilestone(float pfFraction)
    {
        if (float.IsNaN(pfFraction) || float.IsInfinity(pfFraction))
            return 0;
        float lfFraction = Mathf.Clamp01(pfFraction);
        if (lfFraction >= 0.99f) return 4;
        if (lfFraction >= 0.75f) return 3;
        if (lfFraction >= 0.50f) return 2;
        if (lfFraction >= 0.25f) return 1;
        return 0;
    }

    public static float DrawAmplitude(int piCue)
    {
        // Both amplitude and duration are intentionally conservative until
        // physical Quest comfort tests can establish appropriate levels.
        return Mathf.Clamp(0.12f + Mathf.Max(0, piCue - 1) * 0.06f, 0.12f, 0.36f);
    }

    public static float DrawDuration(int piCue)
    {
        return Mathf.Clamp(0.018f + Mathf.Max(0, piCue - 1) * 0.007f, 0.018f, 0.046f);
    }

    /// <summary>Pure per-device minimum separation to avoid sending impulses
    /// on every frame or crowding a release signal.</summary>
    public static bool CanPulse(float pfNow, float pfLastAt, float pfCooldown = 0.105f)
    {
        return !float.IsNaN(pfNow) && !float.IsInfinity(pfNow)
            && pfNow >= 0f && pfNow - pfLastAt >= Mathf.Max(0f, pfCooldown);
    }
}
