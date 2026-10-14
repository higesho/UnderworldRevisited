using UnityEngine;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Physical Touch muzzle positions for opt-in -vr-hand-origin. Arrows and
/// projectile spells use this without replacing the game's original missile
/// trajectory, collision, gravity, ammo, mana or impacts. The world-scale
/// setting is deliberately explicit until a real headset can be calibrated.
/// </summary>
public static class UWVRHandLaunch
{
    public enum Result { Untracked, Ready, Blocked }

    public const float RightMuzzleOffsetMetres = 0.18f;
    public const float LeftMuzzleOffsetMetres = 0.12f;
    private const int IgnoreRaycastMask = ~(1 << 2);

    public static bool IsEnabled => UWVRAim.IsHandAimActive
        && UWVRBootstrap.HandOriginRequested;

    public static Result Resolve(Camera pOCamera, XRNode peHand, out Ray pOMuzzle)
    {
        pOMuzzle = default;
        if (!IsEnabled || pOCamera == null)
            return Result.Untracked;

        UWVRHeadTracking lOTracking = pOCamera.GetComponent<UWVRHeadTracking>();
        Ray lOHand = default;
        if (lOTracking == null || !lOTracking.TryGetControllerRay(peHand, out lOHand))
            return Result.Untracked;

        float lfOffsetMetres = peHand == XRNode.RightHand
            ? RightMuzzleOffsetMetres : LeftMuzzleOffsetMetres;
        Transform lOAvatar = UWScene.Player != null ? UWScene.Player.transform : null;
        return Validate(pOCamera.transform.position, lOHand, true,
            UWVRBootstrap.WorldUnitsPerMeter, lfOffsetMetres, lOAvatar, out pOMuzzle);
    }

    /// <summary>XR-independent geometry. The controller's forward axis is a
    /// weapon's barrel/hand direction, not the HMD's gaze. On tracked-but-
    /// blocked poses DO NOT fall back to the head: doing so could shoot from
    /// behind a wall. Only genuinely missing tracking returns Untracked.</summary>
    public static Result Validate(Vector3 pOEyePosition, Ray pOHand,
        bool pbTracked, float pfUnitsPerMetre, float pfOffsetMetres,
        Transform pOAvatar, out Ray pOMuzzle)
    {
        pOMuzzle = default;
        if (!pbTracked)
            return Result.Untracked;
        if (!IsFinite(pOEyePosition) || !IsFinite(pOHand.origin)
            || !UWVRAim.HasValidDirection(pOHand.direction)
            || float.IsNaN(pfUnitsPerMetre) || float.IsInfinity(pfUnitsPerMetre)
            || pfUnitsPerMetre < UWVRWorldSpace.MinUnitsPerMeter
            || pfUnitsPerMetre > UWVRWorldSpace.MaxUnitsPerMeter)
            return Result.Blocked;

        float lfOffset = Mathf.Max(0f, pfOffsetMetres) * pfUnitsPerMetre;
        Vector3 lOMuzzle = pOHand.origin + pOHand.direction.normalized * lfOffset;
        if (!IsFinite(lOMuzzle) || !IsInDungeon(lOMuzzle)
            || !HasUnobstructedPath(pOEyePosition, lOMuzzle, pOAvatar))
            return Result.Blocked;

        pOMuzzle = new Ray(lOMuzzle, pOHand.direction.normalized);
        return Result.Ready;
    }

    /// <summary>Original motion uses quarter-world-unit fine positions, not
    /// the 8-world-unit sub-tile centres. Do NOT round the VR hand to the
    /// original eighth: at small XR scales all hand positions would look
    /// identical and the missile would leave the avatar's feet again.</summary>
    public static bool TryBuildOriginalLauncher(Ray pOMuzzle, int piHeading,
        out UWMobileObjectMotion.Launcher pOLauncher)
    {
        pOLauncher = default;
        if (!IsFinite(pOMuzzle.origin) || !UWVRAim.HasValidDirection(pOMuzzle.direction))
            return false;

        int liFineX = UWViewpoint.WorldToOriginalX(pOMuzzle.origin.x);
        int liFineY = UWViewpoint.WorldToOriginalY(pOMuzzle.origin.z);
        int liFineZ = UWViewpoint.WorldToOriginalZ(pOMuzzle.origin.y);
        if (liFineX < 0 || liFineX >= 64 * 256
            || liFineY < 0 || liFineY >= 64 * 256
            || liFineZ < 0 || liFineZ >= 128 * 8)
            return false;

        int liX8 = liFineX >> 5;
        int liY8 = liFineY >> 5;
        pOLauncher = new UWMobileObjectMotion.Launcher
        {
            Index = 1,
            IsCreature = true,
            IsPlayer = true,
            HasExplicitStart = true,
            ExplicitFineX = liFineX,
            ExplicitFineY = liFineY,
            ExplicitFineZ = liFineZ,
            TileX = liX8 >> 3,
            TileY = liY8 >> 3,
            XPos = liX8 & 7,
            YPos = liY8 & 7,
            ZPos = liFineZ >> 3,
            Octant = (piHeading >> 5) & 7,
            FineHeadingBits = piHeading & 0x1F
        };
        return true;
    }

    private static bool HasUnobstructedPath(Vector3 pOFrom, Vector3 pOTo,
        Transform pOAvatar)
    {
        Vector3 lODelta = pOTo - pOFrom;
        float lfDistance = lODelta.magnitude;
        if (lfDistance < 0.00001f)
            return true;

        RaycastHit[] lOHits = Physics.RaycastAll(pOFrom, lODelta / lfDistance,
            lfDistance, IgnoreRaycastMask, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit lOHit in lOHits)
        {
            if (lOHit.collider == null || (pOAvatar != null
                && lOHit.collider.transform.IsChildOf(pOAvatar)))
                continue;
            // A solid wall or closed door between the eye and the muzzle
            // cannot be bypassed simply by reaching the Touch through it.
            if (lOHit.distance < lfDistance - 0.01f)
                return false;
        }
        return true;
    }

    private static bool IsInDungeon(Vector3 pOPosition)
    {
        return TryRawPosition(pOPosition, out _, out _, out _);
    }

    private static bool TryRawPosition(Vector3 pOPosition,
        out int piX, out int piY, out int piZ)
    {
        piX = UWViewpoint.WorldToOriginalX(pOPosition.x);
        piY = UWViewpoint.WorldToOriginalY(pOPosition.z);
        piZ = UWViewpoint.WorldToOriginalZ(pOPosition.y);
        return piX >= 0 && piX < 64 * 256
            && piY >= 0 && piY < 64 * 256
            && piZ >= 0 && piZ < 128 * 8;
    }

    private static bool IsFinite(Vector3 pOPosition)
    {
        return !float.IsNaN(pOPosition.x) && !float.IsInfinity(pOPosition.x)
            && !float.IsNaN(pOPosition.y) && !float.IsInfinity(pOPosition.y)
            && !float.IsNaN(pOPosition.z) && !float.IsInfinity(pOPosition.z);
    }
}
