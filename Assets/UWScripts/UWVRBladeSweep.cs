using System;
using UnityEngine;

/// <summary>
/// Optional, continuous right-hand blade CONTACT recording while the right
/// trigger charges an original melee attack. Samples five points from hilt
/// towards tip and casts each point from its prior tracked world position to
/// the current one. The first physical surface blocks each sweep; no through-
/// wall damage. This code records a contact, it does NOT apply any damage.
/// Interaction consumes one contact on trigger release and delegates the
/// actual attack to UWCharacter's existing hit frame and UWPlayerAttack.
/// </summary>
public sealed class UWVRBladeSweep
{
    public readonly struct Contact
    {
        public readonly Collider Collider;
        public readonly Vector3 Point;
        public readonly float Distance;

        public Contact(Collider pOCollider, Vector3 pOPoint, float pfDistance)
        {
            Collider = pOCollider;
            Point = pOPoint;
            Distance = pfDistance;
        }
    }

    public const int SweepPoints = 5;
    public const float ContactFreshSeconds = 0.35f;
    private const float MaxHandMoveMetres = 0.40f;
    private const float MinHandSpeedMetresPerSecond = UWVRMeleeMotion.MinSpeedMetersPerSecond;
    private const float MaxHandSpeedMetresPerSecond = UWVRMeleeMotion.MaxSpeedMetersPerSecond;
    private const int IgnoreRaycastMask = ~(1 << 2);

    private readonly Vector3[] mPrior = new Vector3[SweepPoints];
    private readonly Vector3[] mPoints = new Vector3[SweepPoints];
    private bool mbHasPrior;
    private Vector3 mPreviousHand;
    private UWVRWeaponShape.Kind mePreviousKind;
    private float mfPreviousUnits;
    private Contact mContact;
    private float mfContactAge;

    public bool HasRecentContact => mContact.Collider != null
        && mfContactAge <= ContactFreshSeconds;

    public static bool IsEnabled => UWVRMeleeMotion.IsEnabled
        && UWVRBootstrap.BladeContactRequested;

    public void Reset()
    {
        mbHasPrior = false;
        mContact = default;
        mfContactAge = 0f;
    }

    /// <summary>Measured in world coordinates, but compared with thresholds
    /// in physical metres. Rejects tracking teleports, too-large frame delta,
    /// invalid poses and switching from sword to axe during an attack.</summary>
    public void Sample(bool pbTracked, Vector3 pOHandPosition,
        Quaternion pOHandRotation, UWVRWeaponShape.Profile pOProfile,
        float pfUnitsPerMetre, Vector3 pOEyePosition, float pfOriginalReach,
        Transform pOAvatar, float pfSeconds)
    {
        if (!pbTracked || !IsFinite(pOHandPosition) || !IsFinite(pOHandRotation)
            || Mathf.Abs(1f - Quaternion.Dot(pOHandRotation, pOHandRotation)) > 0.08f
            || !IsFinite(pOEyePosition) || pfUnitsPerMetre <= 0f
            || pfSeconds <= 0f || pfSeconds > 0.1f || pfOriginalReach <= 0f)
        {
            Reset();
            return;
        }

        Ray lOHandRay = new Ray(pOHandPosition, pOHandRotation * Vector3.forward);
        for (int li = 0; li < SweepPoints; li++)
            mPoints[li] = UWVRWeaponShape.ContactPoint(lOHandRay, pOProfile,
                li / (float)(SweepPoints - 1), pfUnitsPerMetre);

        if (!mbHasPrior || mePreviousKind != pOProfile.Form
            || Mathf.Abs(mfPreviousUnits - pfUnitsPerMetre) > 0.001f)
        {
            Reset();
            mbHasPrior = true;
            mePreviousKind = pOProfile.Form;
            mfPreviousUnits = pfUnitsPerMetre;
            mPreviousHand = pOHandPosition;
            Array.Copy(mPoints, mPrior, SweepPoints);
            return;
        }

        float lfTravelMeters = (pOHandPosition - mPreviousHand).magnitude / pfUnitsPerMetre;
        float lfSpeed = lfTravelMeters / pfSeconds;
        if (lfTravelMeters > MaxHandMoveMetres || lfSpeed > MaxHandSpeedMetresPerSecond)
        {
            Reset();
            return;
        }

        mPreviousHand = pOHandPosition;
        mfContactAge += pfSeconds;
        if (mfContactAge > ContactFreshSeconds)
            mContact = default;

        if (lfSpeed >= MinHandSpeedMetresPerSecond && !HasRecentContact)
        {
            for (int li = 0; li < SweepPoints; li++)
            {
                Vector3 lOFrom = mPrior[li];
                Vector3 lOTo = mPoints[li];
                if (TryFirstSurface(lOFrom, lOTo,
                    pOProfile.RadiusMetres * pfUnitsPerMetre, pOAvatar, out RaycastHit lOHit))
                {
                    if (lOHit.collider == null)
                        continue;

                    // Sphere casts sometimes report point=(0,0,0) for an
                    // initial overlap. That value is NOT the enemy position:
                    // checking reach against it gave free hits on enemies
                    // beyond the player's original maximum weapon range.
                    Vector3 lOPhysicalContact = lOHit.collider.ClosestPoint(lOTo);
                    if (!IsFinite(lOPhysicalContact)
                        || Vector3.Distance(pOEyePosition, lOPhysicalContact) > pfOriginalReach)
                        continue;

                    UWEntityInfo lOEntity = lOHit.collider.GetComponentInParent<UWEntityInfo>();
                    if (lOEntity != null && lOEntity.PartOf != null)
                        lOEntity = lOEntity.PartOf;
                    UWDamageable lODamageable = lOEntity != null
                        ? lOEntity.GetComponentInParent<UWDamageable>() : null;
                    if (lODamageable == null || lODamageable.IsDestroyed
                        || !HasClearPath(pOEyePosition, lOPhysicalContact, lOHit.collider, pOAvatar))
                        continue;

                    // One candidate per trigger-held attack. First touch wins
                    // unless it expires; no repeated damage for sword-overlap.
                    if (!HasRecentContact)
                    {
                        mContact = new Contact(lOHit.collider, lOPhysicalContact,
                            Vector3.Distance(pOEyePosition, lOPhysicalContact));
                        mfContactAge = 0f;
                    }
                    break;
                }
            }
        }

        Array.Copy(mPoints, mPrior, SweepPoints);
    }

    public bool TryFinish(out Contact pOContact)
    {
        pOContact = HasRecentContact ? mContact : default;
        Reset();
        return pOContact.Collider != null;
    }

    /// <summary>Find the nearest physical obstruction rather than assuming a
    /// sphere cast result array is sorted. Exclude avatar and Ignore Raycast,
    /// and reject triggers such as invisible proximity volumes.</summary>
    public static bool TryFirstSurface(Vector3 pOFrom, Vector3 pOTo,
        float pfRadius, Transform pOAvatar, out RaycastHit pOHit)
    {
        pOHit = default;
        Vector3 lODelta = pOTo - pOFrom;
        float lfLength = lODelta.magnitude;
        if (lfLength <= 0.00001f || pfRadius <= 0f)
            return false;

        RaycastHit[] lOHits = Physics.SphereCastAll(pOFrom, pfRadius,
            lODelta / lfLength, lfLength, IgnoreRaycastMask, QueryTriggerInteraction.Ignore);
        float lfClosest = float.PositiveInfinity;
        foreach (RaycastHit lOHit in lOHits)
        {
            if (lOHit.collider == null || IsOwnedByAvatar(lOHit.collider, pOAvatar)
                || lOHit.distance >= lfClosest)
                continue;
            lfClosest = lOHit.distance;
            pOHit = lOHit;
        }

        return pOHit.collider != null;
    }

    /// <summary>A collider behind a solid wall cannot be scored, even if a
    /// different blade sample happened to reach it around the wall. A target
    /// collider or sibling under the same entity counts as the first hit.</summary>
    public static bool HasClearPath(Vector3 pOFrom, Vector3 pOTo,
        Collider pOTarget, Transform pOAvatar)
    {
        if (pOTarget == null)
            return false;
        Vector3 lODelta = pOTo - pOFrom;
        float lfLength = lODelta.magnitude;
        if (lfLength < 0.00001f)
            return true;

        RaycastHit[] lOHits = Physics.RaycastAll(pOFrom, lODelta / lfLength,
            lfLength + 0.015f, IgnoreRaycastMask, QueryTriggerInteraction.Ignore);
        float lfNearestObstruction = float.PositiveInfinity;
        Collider lONearest = null;
        foreach (RaycastHit lOHit in lOHits)
        {
            if (lOHit.collider == null || IsOwnedByAvatar(lOHit.collider, pOAvatar)
                || lOHit.distance >= lfNearestObstruction)
                continue;
            lfNearestObstruction = lOHit.distance;
            lONearest = lOHit.collider;
        }

        // The blade can already be inside a large enemy volume when the
        // user's camera is also inside it. In that case Linecast returns no
        // target; absence of any OTHER obstruction is what matters.
        if (lONearest == null || lONearest == pOTarget)
            return true;

        // Most of the level's entities share a common scene parent. Comparing
        // their transform ROOT would mistakenly consider a wall and a goblin
        // the same target and permit attacks through walls.
        UWEntityInfo lOExpected = pOTarget.GetComponentInParent<UWEntityInfo>();
        UWEntityInfo lOHitEntity = lONearest.GetComponentInParent<UWEntityInfo>();
        if (lOExpected == null || lOHitEntity == null)
            return false;
        return lOHitEntity == lOExpected
            || lOHitEntity.PartOf == lOExpected
            || lOExpected.PartOf == lOHitEntity;
    }

    private static bool IsOwnedByAvatar(Collider pOCollider, Transform pOAvatar)
    {
        return pOAvatar != null && pOCollider.transform.IsChildOf(pOAvatar);
    }

    private static bool IsFinite(Vector3 pOValue)
    {
        return !float.IsNaN(pOValue.x) && !float.IsInfinity(pOValue.x)
            && !float.IsNaN(pOValue.y) && !float.IsInfinity(pOValue.y)
            && !float.IsNaN(pOValue.z) && !float.IsInfinity(pOValue.z);
    }

    private static bool IsFinite(Quaternion pOValue)
    {
        return !float.IsNaN(pOValue.x) && !float.IsInfinity(pOValue.x)
            && !float.IsNaN(pOValue.y) && !float.IsInfinity(pOValue.y)
            && !float.IsNaN(pOValue.z) && !float.IsInfinity(pOValue.z)
            && !float.IsNaN(pOValue.w) && !float.IsInfinity(pOValue.w);
    }
}
