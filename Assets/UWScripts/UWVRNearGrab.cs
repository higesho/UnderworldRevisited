using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Optional near-hand input adapter for the world-anchored inventory.
/// When -vr-near-grab is used together with -vr-spatial-inventory, the bag
/// panel is placed within reach. A right Touch grip started next to a real
/// item slot holds the SAME synthetic left mouse button used by the game.
/// The player's existing CursorItem, rather than a second VR inventory, owns
/// taking, stacking, combining and dropping. Moving the controller to a real
/// destination slot before releasing performs the game's normal drag.
///
/// This uses the tracked Touch controller position as a coarse hand proxy,
/// NOT finger tracking or physics picking. There are no grab colliders.
/// </summary>
[DefaultExecutionOrder(-150)]
public sealed class UWVRNearGrab : MonoBehaviour
{
    public const float ReachMetres = 0.135f;
    public const float BehindPanelMetres = 0.025f;

    private static UWVRNearGrab msInstance;
    private bool mbRawGripDown;
    private bool mbGrabbing;
    private bool mbGripStarted;
    private bool mbNearPointValid;
    private int miReleasePointUntilFrame = -1;
    private Vector2 mNearPoint;
    private Vector2 mOriginPoint;
    private Vector2 mReleasePoint;
    private UWObject mHoverItem;
    private UWObject mPickedItem;
    private int miWaitForDropUntil = -1;
    private ushort miPendingItemId;
    private float mfLastHapticAt = -10f;
    private bool mbReportedController;

    public static bool IsEnabled => UWVRSpatialInventory.IsEnabled && UWVRBootstrap.NearGrabRequested;
    public static bool IsActive => IsEnabled && UWVRSpatialInventory.VisiblePanel != null;
    public static bool GripHeld => msInstance != null && msInstance.mbGrabbing;
    public static bool GripStarted => msInstance != null && msInstance.mbGripStarted;
    public static UWObject HoveredItem => IsActive && msInstance != null
        && msInstance.mbNearPointValid && !msInstance.mbGrabbing
        ? msInstance.mHoverItem : null;

    /// <summary>Grip is already the usual ready-weapon command. Only reserve
    /// it while this opt-in physical reach interface is actually displayed;
    /// outside the bag, right grip continues to ready/sheathe as before.</summary>
    public static bool ReservesRightGrip => IsActive && ReadRightGrip();

    private void Awake() { msInstance = this; }

    public static bool TryGetPointer(out Vector2 pOPoint)
    {
        pOPoint = Vector2.zero;
        if (msInstance == null)
            return false;

        // OS mouse injection is received by the Input System asynchronously.
        // Give the release point priority over new hand movement until the
        // game has read mouse-up; the older laser may not steal the drop.
        return TryChoosePointer(Time.frameCount <= msInstance.miReleasePointUntilFrame,
            msInstance.mbGrabbing, msInstance.mbNearPointValid, IsActive,
            msInstance.mReleasePoint, msInstance.mNearPoint, msInstance.mOriginPoint,
            out pOPoint);
    }

    /// <summary>Pure cursor resolver for offline regression tests, including
    /// the release-frame hand-off to the original Unity mouse handler.</summary>
    public static bool TryChoosePointer(bool pbReleasePending, bool pbGrabbing,
        bool pbNearPointValid, bool pbPanelActive,
        Vector2 pORelease, Vector2 pONear, Vector2 pOOrigin,
        out Vector2 pOResult)
    {
        pOResult = Vector2.zero;
        if (pbReleasePending)
        {
            pOResult = pORelease;
            return true;
        }

        if (pbGrabbing)
        {
            pOResult = pbNearPointValid ? pONear : pOOrigin;
            return true;
        }

        if (!pbPanelActive || !pbNearPointValid)
            return false;

        pOResult = pONear;
        return true;
    }

    /// <summary>True only when a tracked controller lies within a short
    /// physical reach of the panel and inside the UI rectangle. The panel's
    /// X/Y scale may be nonuniform; depth is in physical metres via units.</summary>
    public static bool TryProjectHand(Vector3 pOHandWorld,
        Matrix4x4 pOWorldToLocal, float pfWorldUnitsPerMetre,
        out Vector2 pOUV)
    {
        pOUV = Vector2.zero;
        if (pfWorldUnitsPerMetre <= 0f)
            return false;

        Vector3 lOHand = pOWorldToLocal.MultiplyPoint3x4(pOHandWorld);
        float lfNearUnits = ReachMetres * pfWorldUnitsPerMetre;
        float lfBehindUnits = BehindPanelMetres * pfWorldUnitsPerMetre;
        if (lOHand.z < -lfNearUnits || lOHand.z > lfBehindUnits
            || lOHand.x < -0.5f || lOHand.x > 0.5f
            || lOHand.y < -0.5f || lOHand.y > 0.5f)
            return false;

        pOUV = new Vector2(lOHand.x + 0.5f, lOHand.y + 0.5f);
        return true;
    }

    public static bool CanBeginGrip(bool pbGripJustPressed,
        bool pbControllerNear, bool pbItemPresent, bool pbCursorEmpty)
    {
        return pbGripJustPressed && pbControllerNear && pbItemPresent && pbCursorEmpty;
    }

    /// <summary>Monitor the physical Touch grip even while the panel is shut.
    /// Holding grip before opening a bag must NEVER count as a new grab.</summary>
    public static bool IsNewPhysicalGrip(bool pbPhysicalGripDown,
        bool pbPhysicalGripWasDown, bool pbInterfaceActive)
    {
        return pbInterfaceActive && pbPhysicalGripDown && !pbPhysicalGripWasDown;
    }

    /// <summary>Releasing a grabbed item off an actual equipment/bag slot
    /// returns the pointer to its source. The original click-and-click logic
    /// then preserves the item rather than throwing it into the world.</summary>
    public static Vector2 ReleasePoint(Vector2 pOOrigin, Vector2 pOCandidate,
        bool pbCandidateIsDropSlot)
    {
        return pbCandidateIsDropSlot ? pOCandidate : pOOrigin;
    }

    private static bool ReadRightGrip()
    {
        UnityEngine.XR.InputDevice lODevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        return lODevice.isValid
            && lODevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool lbDown)
            && lbDown;
    }

    private void Update()
    {
        mbGripStarted = false;
        mbNearPointValid = false;

        UWModernBags lOBags = UWModernBags.Instance;
        UWInventory lOInventory = UWScene.Inventory;
        bool lbPhysicalGrip = IsEnabled && ReadRightGrip();
        bool lbActive = IsActive && UWModernPointer.IsFree
            && lOBags != null && lOInventory != null
            && Mouse.current != null && Application.isFocused
            && !UWModernBags.IsMenuOpen && !UWModernBags.IsSplitting
            && !UWModernQuestion.IsBlocking && !UWControls.IsTextEntryActive
            && UWGamepadPointer.ClicksActive;
        bool lbGrip = lbActive && lbPhysicalGrip;

        if (lbActive && !mbReportedController)
        {
            UnityEngine.XR.InputDevice lODevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            bool lbGripAvailable = lODevice.isValid
                && lODevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton, out bool lbGripValue);
            bool lbHapticAvailable = lODevice.isValid
                && lODevice.TryGetHapticCapabilities(out HapticCapabilities lOHaptics)
                && lOHaptics.supportsImpulse && lOHaptics.numChannels > 0;
            Debug.Log("[UWVR-GRAB] interface ready frame=" + Time.frameCount
                + " controller=" + lODevice.isValid
                + " gripFeature=" + lbGripAvailable
                + " vibration=" + lbHapticAvailable);
            mbReportedController = true;
        }

        // If the bag closes or the application loses focus during a grip,
        // do not allow the later synthetic mouse-up to drop the held item in
        // the 3D world. The existing inventory return method puts it back in
        // its original equipment/bag slot or another valid carried slot.
        if (mbGrabbing && !lbActive)
        {
            Debug.LogWarning("[UWVR-GRAB] gesture canceled (UI hidden, modal or focus lost). item="
                + (mPickedItem != null ? mPickedItem.ID.ToString() : "none"));
            if (lOBags != null && lOInventory != null && lOInventory.CursorItem != null)
                lOBags.ReturnCursorItem();
            mbGrabbing = false;
            mPickedItem = null;
            miWaitForDropUntil = -1;
            miReleasePointUntilFrame = -1;
        }

        if (lbActive)
        {
            Camera lOEye = UWVRBootstrap.CurrentVRCamera;
            UWVRHeadTracking lOTracking = lOEye != null
                ? lOEye.GetComponent<UWVRHeadTracking>() : null;
            Transform lOPanel = UWVRSpatialInventory.VisiblePanel;

            if (lOTracking != null && lOPanel != null
                && lOTracking.TryGetControllerRay(XRNode.RightHand, out Ray lOHandRay)
                && TryProjectHand(lOHandRay.origin, lOPanel.worldToLocalMatrix,
                    Mathf.Max(0.001f, lOEye.transform.lossyScale.x), out Vector2 lOUV))
            {
                mNearPoint = new Vector2(lOUV.x * Screen.width, lOUV.y * Screen.height);
                mbNearPointValid = true;
            }
        }

        if (!mbGrabbing && mbNearPointValid && lOBags != null
            && lOBags.TryGetVRItemAt(mNearPoint, out UWObject lOHovered))
        {
            if (mHoverItem != lOHovered)
            {
                mHoverItem = lOHovered;
                Pulse(0.12f, 0.028f);
            }
        }
        else
            mHoverItem = null;

        if (!mbGrabbing && Time.frameCount > miReleasePointUntilFrame
            && CanBeginGrip(IsNewPhysicalGrip(lbPhysicalGrip, mbRawGripDown, lbActive),
                mbNearPointValid,
                mbNearPointValid && lOBags != null
                    && lOBags.TryGetVRItemAt(mNearPoint, out UWObject _),
                lOInventory != null && lOInventory.CursorItem == null))
        {
            mOriginPoint = mNearPoint;
            mbGrabbing = true;
            mbGripStarted = true;
            mPickedItem = null;
            Debug.Log("[UWVR-GRAB] grip start frame=" + Time.frameCount
                + " source=" + mOriginPoint);
        }

        if (mbGrabbing && lOInventory != null && lOInventory.CursorItem != null)
        {
            if (mPickedItem == null)
            {
                mPickedItem = lOInventory.CursorItem;
                Pulse(0.38f, 0.055f);
                Debug.Log("[UWVR-GRAB] item taken id=" + mPickedItem.ID
                    + " frame=" + Time.frameCount);
            }
        }

        if (mbGrabbing && !lbGrip)
        {
            // A valid drop is strictly inside an actual bag/equipment slot;
            // releasing anywhere else keeps the item on the original cursor.
            bool lbValidDrop = mbNearPointValid && lOBags != null
                && lOBags.IsVRDropSlot(mNearPoint);
            mReleasePoint = ReleasePoint(mOriginPoint, mNearPoint, lbValidDrop);
            // OS mouse injection is observed by the Input System on its next
            // update. Keep the release pixel stable for the next two frames;
            // otherwise the old laser cursor could overwrite a valid drop.
            miReleasePointUntilFrame = Time.frameCount + 2;
            miWaitForDropUntil = mPickedItem != null ? Time.frameCount + 8 : -1;
            miPendingItemId = mPickedItem != null ? mPickedItem.ID : (ushort)0;
            Debug.Log("[UWVR-GRAB] release frame=" + Time.frameCount
                + " dest=" + mReleasePoint + " validSlot=" + lbValidDrop
                + " item=" + (mPickedItem != null ? mPickedItem.ID.ToString() : "not_taken"));
            mbGrabbing = false;
            mPickedItem = null;
        }

        // Confirmation comes from the game's actual inventory, not from the
        // grip signal. Never rumble for a drop that the game has rejected.
        if (miWaitForDropUntil >= Time.frameCount && lOInventory != null
            && lOInventory.CursorItem == null && Time.frameCount > miReleasePointUntilFrame)
        {
            Pulse(0.26f, 0.05f);
            Debug.Log("[UWVR-GRAB] game confirmed placement item="
                + miPendingItemId + " frame=" + Time.frameCount);
            miWaitForDropUntil = -1;
        }
        else if (miWaitForDropUntil < Time.frameCount)
        {
            if (miWaitForDropUntil >= 0)
                Debug.Log("[UWVR-GRAB] item remained on cursor or placement unconfirmed id="
                    + miPendingItemId + " frame=" + Time.frameCount);
            miWaitForDropUntil = -1;
        }

        mbRawGripDown = lbPhysicalGrip;
    }

    private void Pulse(float pfAmplitude, float pfSeconds)
    {
        if (Time.unscaledTime - mfLastHapticAt < 0.12f)
            return;

        UnityEngine.XR.InputDevice lODevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!lODevice.isValid || !lODevice.TryGetHapticCapabilities(out HapticCapabilities lOHaptics)
            || !lOHaptics.supportsImpulse || lOHaptics.numChannels == 0)
            return;

        if (lODevice.SendHapticImpulse(0, Mathf.Clamp01(pfAmplitude),
                Mathf.Clamp(pfSeconds, 0.01f, 0.12f)))
            mfLastHapticAt = Time.unscaledTime;
    }

    private void OnDisable()
    {
        mbGrabbing = false;
        mbRawGripDown = false;
        mbGripStarted = false;
        mbNearPointValid = false;
        miReleasePointUntilFrame = -1;
    }

    private void OnDestroy()
    {
        if (msInstance == this)
            msInstance = null;
    }
}
