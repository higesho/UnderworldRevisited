using UnityEngine;

/// <summary>
/// Optional VR-only snap-turning of the existing player body. The headset
/// retains free head rotation; right-stick yaw is applied to the body instead
/// of trying to turn the tracked camera. The motion core already observes body
/// yaw changes, so no additional movement system is introduced.
/// </summary>
public sealed class UWVRComfortTurn : MonoBehaviour
{
    private UWControlScheme mScheme;
    private UWControls.PlayerActions mInput;
    private bool mbStickHeld;

    private const float SnapDegrees = 30f;
    private const float TurnThreshold = 0.7f;
    private const float ReleaseThreshold = 0.3f;

    private void Start()
    {
        mScheme = GetComponent<UWControlScheme>();
        if (mScheme != null)
            mInput = mScheme.Controls.Player;
    }

    private void Update()
    {
        if (!UWVRBootstrap.IsHeadsetActive || mScheme == null ||
            mScheme.Current != UWControlScheme.SchemeEnum.Modern ||
            mScheme.IsWorldInputBlocked || UWGamepadPointer.IsDriving)
        {
            mbStickHeld = false;
            return;
        }

        float lfX = mInput.LookStick.ReadValue<Vector2>().x;
        if (Mathf.Abs(lfX) < ReleaseThreshold)
        {
            mbStickHeld = false;
            return;
        }

        if (!mbStickHeld && Mathf.Abs(lfX) >= TurnThreshold)
        {
            mbStickHeld = true;
            transform.Rotate(0f, Mathf.Sign(lfX) * SnapDegrees, 0f, Space.Self);
        }
    }
}
