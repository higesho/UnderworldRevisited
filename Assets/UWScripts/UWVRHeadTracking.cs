using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Experimental head tracking for the existing player camera. Uses the first
/// valid tracked eye position as the seated/standing reference so the game's
/// established camera height and collision rules stay unchanged. Subsequent
/// real-world head movement is additive to this anchor.
/// </summary>
public sealed class UWVRHeadTracking : MonoBehaviour
{
    private readonly List<XRNodeState> mNodeStates = new List<XRNodeState>(16);
    private Vector3 mBaseLocalPosition;
    private Quaternion mBaseLocalRotation;
    private Vector3 mInitialTrackedPosition;
    private bool mbCalibrated;

    private void OnEnable()
    {
        mBaseLocalPosition = transform.localPosition;
        mBaseLocalRotation = transform.localRotation;
        mbCalibrated = false;
        Application.onBeforeRender += UpdateTrackedPose;
    }

    private void Update()
    {
        UpdateTrackedPose();
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= UpdateTrackedPose;
        transform.localPosition = mBaseLocalPosition;
        transform.localRotation = mBaseLocalRotation;
    }

    private void UpdateTrackedPose()
    {
        if (!UWVRBootstrap.IsHeadsetActive)
            return;

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
                    if (!mbCalibrated)
                    {
                        mInitialTrackedPosition = lOPosition;
                        mbCalibrated = true;
                    }
                    transform.localPosition = mBaseLocalPosition + lOPosition - mInitialTrackedPosition;
                }

                transform.localRotation = lORotation;
                return;
            }
        }
    }
}
