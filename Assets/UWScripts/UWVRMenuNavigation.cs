using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;

/// <summary>
/// Direct initial-menu input while the headset is active. The original menu
/// uses a desktop mouse hit test, but an XR eye does not have a Windows mouse
/// pointer. Offer deterministic item selection using keyboard arrows/Enter,
/// pad d-pad/left stick/A, or either Quest Touch thumbstick and A/X/trigger.
/// This does not require a tracked CONTROLLER POSE; a stationary or poorly
/// tracked Touch can still navigate a flat menu. It never takes world inputs.
/// </summary>
public sealed class UWVRMenuNavigation
{
    public struct Commands
    {
        public int Step;
        public bool Confirm;
        public bool Back;
    }

    private bool mbWasXRConfirm;
    private bool mbWasXRBack;
    private int miHeldStick;
    private float mfNextStickStep;
    private bool mbReportedDeviceState;
    private bool mbLastLeftValid;
    private bool mbLastRightValid;

    public static bool IsActive => UWVRBootstrap.IsHeadsetActive && UWMainMenu.IsOpen;

    public static int WrapSelection(int piCurrent, int piDelta, int piCount)
    {
        if (piCount <= 0)
            return -1;
        int liAt = (piCurrent + piDelta) % piCount;
        return liAt < 0 ? liAt + piCount : liAt;
    }

    public static int StickDirection(float pfUpDown)
    {
        if (pfUpDown >= 0.60f) return -1;
        if (pfUpDown <= -0.60f) return 1;
        return 0;
    }

    private static bool ReadFeature(XRNode peNode, InputFeatureUsage<bool> pOFeature)
    {
        UnityEngine.XR.InputDevice lODevice = InputDevices.GetDeviceAtXRNode(peNode);
        return lODevice.isValid && lODevice.TryGetFeatureValue(pOFeature, out bool lbOn)
            && lbOn;
    }

    private static float ReadStickY(XRNode peNode)
    {
        UnityEngine.XR.InputDevice lODevice = InputDevices.GetDeviceAtXRNode(peNode);
        return lODevice.isValid
            && lODevice.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 lOAxis)
            ? lOAxis.y : 0f;
    }

    public Commands Poll()
    {
        Commands lOCommands = default;
        Keyboard lOKeyboard = Keyboard.current;
        Gamepad lOPad = Gamepad.current;

        bool lbLeftValid = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand).isValid;
        bool lbRightValid = InputDevices.GetDeviceAtXRNode(XRNode.RightHand).isValid;
        if (!mbReportedDeviceState || lbLeftValid != mbLastLeftValid
            || lbRightValid != mbLastRightValid)
        {
            mbReportedDeviceState = true;
            mbLastLeftValid = lbLeftValid;
            mbLastRightValid = lbRightValid;
            Debug.Log("[UWVR-MENU-INPUT] XR left=" + lbLeftValid
                + "; XR right=" + lbRightValid
                + "; keyboard=" + (lOKeyboard != null)
                + "; gamepad=" + (lOPad != null));
        }

        if (lOKeyboard != null)
        {
            if (lOKeyboard.downArrowKey.wasPressedThisFrame || lOKeyboard.sKey.wasPressedThisFrame)
                lOCommands.Step = 1;
            else if (lOKeyboard.upArrowKey.wasPressedThisFrame || lOKeyboard.wKey.wasPressedThisFrame)
                lOCommands.Step = -1;

            lOCommands.Confirm = lOKeyboard.enterKey.wasPressedThisFrame
                || lOKeyboard.numpadEnterKey.wasPressedThisFrame
                || lOKeyboard.spaceKey.wasPressedThisFrame;
            lOCommands.Back = lOKeyboard.escapeKey.wasPressedThisFrame
                || lOKeyboard.backspaceKey.wasPressedThisFrame;
        }

        if (lOPad != null)
        {
            if (lOCommands.Step == 0)
            {
                if (lOPad.dpad.down.wasPressedThisFrame)
                    lOCommands.Step = 1;
                else if (lOPad.dpad.up.wasPressedThisFrame)
                    lOCommands.Step = -1;
            }
            lOCommands.Confirm |= lOPad.buttonSouth.wasPressedThisFrame;
            lOCommands.Back |= lOPad.buttonEast.wasPressedThisFrame;
        }

        // Both Touch hands can navigate. No TryGetControllerRay here: menu
        // input must work even when the OpenXR pose has not been calibrated.
        float lfStick = ReadStickY(XRNode.RightHand);
        if (Mathf.Abs(lfStick) < 0.35f)
            lfStick = ReadStickY(XRNode.LeftHand);
        if (Mathf.Abs(lfStick) < 0.35f && lOPad != null)
            lfStick = (UWUserSettings.GamepadSwapSticks
                ? lOPad.rightStick : lOPad.leftStick).ReadValue().y;

        int liDirection = StickDirection(lfStick);
        if (liDirection == 0)
        {
            miHeldStick = 0;
        }
        else if (miHeldStick != liDirection || Time.unscaledTime >= mfNextStickStep)
        {
            lOCommands.Step = liDirection;
            mfNextStickStep = Time.unscaledTime + (miHeldStick != liDirection ? 0.34f : 0.16f);
            miHeldStick = liDirection;
        }

        bool lbXRConfirm = ReadFeature(XRNode.RightHand, XRCommonUsages.primaryButton)
            || ReadFeature(XRNode.LeftHand, XRCommonUsages.primaryButton)
            || ReadFeature(XRNode.RightHand, XRCommonUsages.triggerButton)
            || ReadFeature(XRNode.LeftHand, XRCommonUsages.triggerButton);
        bool lbXRBack = ReadFeature(XRNode.RightHand, XRCommonUsages.secondaryButton)
            || ReadFeature(XRNode.LeftHand, XRCommonUsages.secondaryButton);

        lOCommands.Confirm |= lbXRConfirm && !mbWasXRConfirm;
        lOCommands.Back |= lbXRBack && !mbWasXRBack;
        mbWasXRConfirm = lbXRConfirm;
        mbWasXRBack = lbXRBack;
        return lOCommands;
    }
}
