using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.InputSystem;

/// <summary>
/// Keeps the game's physical player controller and saved camera position in
/// place while inserting a separately scaled OpenXR tracking space. The XR
/// plugin's camera translations AND per-eye offsets inherit this same parent
/// scale, instead of artificially scaling only headset position and leaving
/// stereo disparity inconsistent with it.
/// </summary>
public sealed class UWVRHeadTracking : MonoBehaviour
{
    private readonly List<XRNodeState> mNodeStates = new List<XRNodeState>(16);
    private Vector3 mBaseLocalPosition;
    private Quaternion mBaseLocalRotation;
    private Vector3 mBaseLocalScale;
    private Transform mOriginalParent;
    private Transform mTrackingSpace;
    private Vector3 mInitialTrackedPosition;
    private bool mbOriginalPoseCaptured;
    private bool mbCalibrated;

    /// <summary>
    /// XR controller poses share the tracking origin and room-scale calibration
    /// used by the headset. A controller ray must pass through this space:
    /// using raw XR positions as Unity world coordinates breaks the ray when
    /// world units per metre are not one or the player moves in the dungeon.
    /// </summary>
    public bool TryGetControllerRay(XRNode peNode, out Ray pORay)
    {
        pORay = default;
        if (!UWVRBootstrap.IsHeadsetActive || mTrackingSpace == null || !mbCalibrated)
            return false;

        UnityEngine.XR.InputDevice lOController = InputDevices.GetDeviceAtXRNode(peNode);
        if (!lOController.isValid
            || !lOController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out Vector3 lOPosition)
            || !lOController.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out Quaternion lORotation))
            return false;

        pORay = RayFromTrackedPose(mTrackingSpace, lOPosition,
            lORotation, mInitialTrackedPosition);
        return true;
    }

    /// <summary>Pure conversion reused in headset-free tests.</summary>
    public static Ray RayFromTrackedPose(Transform pOTrackingSpace,
        Vector3 pOControllerPosition, Quaternion pOControllerRotation,
        Vector3 pOHeadOrigin)
    {
        return new Ray(pOTrackingSpace.TransformPoint(pOControllerPosition - pOHeadOrigin),
            (pOTrackingSpace.rotation * pOControllerRotation) * Vector3.forward);
    }

    private void OnEnable()
    {
        CaptureOriginalPose();
        mbCalibrated = false;
        Application.onBeforeRender += UpdateTrackedPose;

        if (UWVRBootstrap.IsHeadsetActive)
            EnsureTrackingSpace(gameObject.name == "UW VR Menu Camera"
                ? 1f : UWVRBootstrap.WorldUnitsPerMeter);
    }

    private void CaptureOriginalPose()
    {
        if (mbOriginalPoseCaptured)
            return;

        mBaseLocalPosition = transform.localPosition;
        mBaseLocalRotation = transform.localRotation;
        mBaseLocalScale = transform.localScale;
        mOriginalParent = transform.parent;
        mbOriginalPoseCaptured = true;
    }

    private void Update()
    {
        UpdateTrackedPose();
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateTrackedPose;
        RestoreTrackingSpace();
    }

    /// <summary>
    /// Rigging is deferred until an XR session exists; the original camera
    /// hierarchy is never changed in desktop mode. The editor's offline test
    /// also invokes this private method using a synthetic camera.
    /// </summary>
    private void EnsureTrackingSpace(float pfUnitsPerMeter)
    {
        if (mTrackingSpace != null)
            return;

        CaptureOriginalPose();

        GameObject lOSpace = new GameObject("UW VR Tracking Space");
        mTrackingSpace = lOSpace.transform;
        mTrackingSpace.SetParent(mOriginalParent, false);
        mTrackingSpace.localPosition = mBaseLocalPosition;
        mTrackingSpace.localRotation = mBaseLocalRotation;
        mTrackingSpace.localScale = Vector3.one * pfUnitsPerMeter;

        transform.SetParent(mTrackingSpace, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = mBaseLocalScale;
    }

    private void RestoreTrackingSpace()
    {
        if (mTrackingSpace == null)
            return;

        Transform lOSpace = mTrackingSpace;
        mTrackingSpace = null;

        if (mOriginalParent != null)
        {
            transform.SetParent(mOriginalParent, false);
            transform.localPosition = mBaseLocalPosition;
            transform.localRotation = mBaseLocalRotation;
            transform.localScale = mBaseLocalScale;
        }

        if (Application.isPlaying)
            Destroy(lOSpace.gameObject);
        else
            DestroyImmediate(lOSpace.gameObject);

        mbOriginalPoseCaptured = false;
        mbCalibrated = false;
    }

    private void UpdateTrackedPose()
    {
        if (!UWVRBootstrap.IsHeadsetActive)
        {
            RestoreTrackingSpace();
            return;
        }

        if (mTrackingSpace == null)
            EnsureTrackingSpace(gameObject.name == "UW VR Menu Camera"
                ? 1f : UWVRBootstrap.WorldUnitsPerMeter);

        InputTracking.GetNodeStates(mNodeStates);

        // CenterEye is the camera's correct stereo reference; Head is a
        // fallback for OpenXR runtimes without a CenterEye node state.
        for (int liPreferred = 0; liPreferred < 2; liPreferred++)
        {
            XRNode lONode = liPreferred == 0 ? XRNode.CenterEye : XRNode.Head;
            foreach (XRNodeState lOState in mNodeStates)
            {
                if (lOState.nodeType != lONode || !lOState.TryGetRotation(out Quaternion lORotation))
                    continue;

                if (lOState.TryGetPosition(out Vector3 lOPosition))
                {
                    bool lbRecenter = Keyboard.current != null
                        && Keyboard.current.f9Key.wasPressedThisFrame;
                    if (!mbCalibrated || lbRecenter)
                    {
                        mInitialTrackedPosition = lOPosition;
                        mbCalibrated = true;
                        if (lbRecenter)
                            Debug.Log("[UWVR] Head position re-centered with F9.");
                    }

                    float lfSwimmingOffset = gameObject.name != "UW VR Menu Camera"
                        && UWScene.PlayerTerrain != null
                        ? UWScene.PlayerTerrain.VRCameraHeightOffset : 0f;

                    // The anchor carries the original camera height and the
                    // game's swimming offset (game world units). The eyes
                    // move in metres inside the appropriately scaled rig.
                    mTrackingSpace.localPosition = mBaseLocalPosition + Vector3.up * lfSwimmingOffset;
                    transform.localPosition = lOPosition - mInitialTrackedPosition;
                }

                transform.localRotation = lORotation;
                return;
            }
        }
    }
}
