using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>
/// XR DIAGNOSTIC ONLY. Keep the right-eye color buffer unchanged; move only
/// the sampled LEFT-eye image horizontally after URP finishes rendering.
/// This is NOT a valid replacement for separate stereo eye projections.
/// Do not persist calibration as a gameplay setting or alter HMD tracking.
/// </summary>
public sealed class UWVRLeftEyeShift : MonoBehaviour
{
    public const int StepPixels = 4;
    // The first headset trial reached +192px with the left image still far
    // from fusion. Keep the 4px precision but allow four times the travel.
    public const int MaxPixels = 768;
    public const int FastStepPixels = 32;
    private const float InitialRepeatSeconds = 0.28f;
    private const float HeldRepeatSeconds = 0.055f;
    private static readonly int msShiftProperty = Shader.PropertyToID("_UWLeftEyeShiftUv");

    private static int msShiftPixels;
    private static int msLastRenderEyeWidth;
    private Camera mOverlayCamera;
    private Canvas mOverlayCanvas;
    private Text mOverlayText;
    private int miHeldDirection;
    private float mfNextRepeatAt;
    private bool mbReported;
    private bool mbWasResetHeld;
    private bool mbReportedStereoGeometry;
    private float mfNextStereoCheckAt;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    // Virtual Desktop can keep the Windows game window unfocused even while
    // the headset OpenXR session is FOCUSED. In this explicitly opt-in test,
    // read the physical keyboard state without changing Windows focus.
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int piVirtualKey);
#endif

    private static bool WindowsKeyDown(int piVirtualKey)
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        return (GetAsyncKeyState(piVirtualKey) & 0x8000) != 0;
#else
        return false;
#endif
    }

    public static bool IsEnabled => UWVRBootstrap.IsHeadsetActive
        && UWVRBootstrap.LeftEyeShiftRequested;

    public static int ShiftPixels => msShiftPixels;
    public static int LastRenderEyeWidth => msLastRenderEyeWidth;

    public static void SetRenderEyeWidth(int piWidth)
    {
        if (piWidth > 0 && msLastRenderEyeWidth != piWidth)
        {
            msLastRenderEyeWidth = piWidth;
            Debug.Log("[UWVR-EYE-SHIFT] XR render target eye width=" + piWidth
                + "px; offset denominator uses this render target, not XRSettings.eyeTextureWidth.");
        }
    }

    public static int ClampPixels(int piPixels)
    {
        return Mathf.Clamp(piPixels, -MaxPixels, MaxPixels);
    }

    /// <summary>Normalized offset of ONE XR eye texture. The fragment shader
    /// samples at uv.x - offset on eye index 0, so a positive value visibly
    /// moves left-eye CONTENT to the right; eye index 1 never changes.</summary>
    public static float NormalizedOffset(int piPixels, int piEyeTextureWidth)
    {
        // Unity XRSettings.eyeTextureWidth can return ZERO when OpenXR uses
        // render graph / MultiPass. A 4px shift MUST NOT become a 4.0 UV shift.
        if (piEyeTextureWidth <= 0)
            return 0f;
        return ClampPixels(piPixels) / (float)piEyeTextureWidth;
    }

    /// <summary>Independent of the image-shift shader: compute which physical
    /// XR eye is to the camera's right from the left/right VIEW matrices.
    /// A negative signed baseline flags swapped physical eye geometry.
    /// It is diagnostic evidence, never an automatic projection override.</summary>
    public static float SignedEyeBaseline(Matrix4x4 pOLeftView,
        Matrix4x4 pORightView, Vector3 pOCameraRight)
    {
        Vector3 lOLeftEye = pOLeftView.inverse.MultiplyPoint3x4(Vector3.zero);
        Vector3 lORightEye = pORightView.inverse.MultiplyPoint3x4(Vector3.zero);
        return Vector3.Dot(lORightEye - lOLeftEye, pOCameraRight.normalized);
    }

    private static void ApplyGlobalOffset()
    {
        // The URP renderer feature publishes the correct camera render-target
        // width at render time. The behaviour disables itself if still unknown.
        Shader.SetGlobalFloat(msShiftProperty, IsEnabled
            ? NormalizedOffset(msShiftPixels, msLastRenderEyeWidth) : 0f);
    }

    private void Update()
    {
        if (!IsEnabled)
        {
            if (mOverlayCanvas != null)
                mOverlayCanvas.enabled = false;
            ApplyGlobalOffset();
            return;
        }

        if (!mbReported)
        {
            mbReported = true;
            Debug.Log("[UWVR-EYE-SHIFT] Diagnostic active: right eye unchanged; "
                + "left eye only, Left/Right arrows move 4 pixels, Shift+arrows move 32 pixels, F6 resets; range +/-"
                + MaxPixels + "px; "
                + "stereo mode=" + XRSettings.stereoRenderingMode
                + "; XRSettings eye width=" + XRSettings.eyeTextureWidth
                + "; Windows keyboard fallback active; game window focused="
                + Application.isFocused + ".");
        }

        Keyboard lOKeyboard = Keyboard.current;
        bool lbRight = (lOKeyboard != null && lOKeyboard.rightArrowKey.isPressed)
            || WindowsKeyDown(0x27); // VK_RIGHT
        bool lbLeft = (lOKeyboard != null && lOKeyboard.leftArrowKey.isPressed)
            || WindowsKeyDown(0x25); // VK_LEFT
        bool lbReset = (lOKeyboard != null && lOKeyboard.f6Key.isPressed)
            || WindowsKeyDown(0x75); // VK_F6
        bool lbFast = (lOKeyboard != null
            && (lOKeyboard.leftShiftKey.isPressed || lOKeyboard.rightShiftKey.isPressed))
            || WindowsKeyDown(0x10); // VK_SHIFT

        if (lbReset && !mbWasResetHeld)
            UpdatePixels(0);
        mbWasResetHeld = lbReset;

        int liDirection = lbRight == lbLeft ? 0 : (lbRight ? 1 : -1);
        if (liDirection == 0)
        {
            miHeldDirection = 0;
        }
        else if (liDirection != miHeldDirection || Time.unscaledTime >= mfNextRepeatAt)
        {
            UpdatePixels(msShiftPixels + liDirection
                * (lbFast ? FastStepPixels : StepPixels));
            mfNextRepeatAt = Time.unscaledTime
                + (liDirection != miHeldDirection ? InitialRepeatSeconds : HeldRepeatSeconds);
            miHeldDirection = liDirection;
        }

        ApplyGlobalOffset();
        ReportXRProjectionGeometry();
        UpdateOverlay();
    }

    private void ReportXRProjectionGeometry()
    {
        if (mbReportedStereoGeometry || Time.unscaledTime < mfNextStereoCheckAt)
            return;
        mfNextStereoCheckAt = Time.unscaledTime + 1f;
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        if (lOCamera == null || !lOCamera.isActiveAndEnabled || !XRSettings.isDeviceActive)
            return;

        try
        {
            Matrix4x4 lOLeftView = lOCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Left);
            Matrix4x4 lORightView = lOCamera.GetStereoViewMatrix(Camera.StereoscopicEye.Right);
            Matrix4x4 lOLeftProjection = lOCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
            Matrix4x4 lORightProjection = lOCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
            float lfBaseline = SignedEyeBaseline(lOLeftView, lORightView,
                lOCamera.transform.right);
            Debug.Log("[UWVR-EYE-GEOMETRY] XR camera=" + lOCamera.name
                + "; right-minus-left view origin projected on camera right="
                + lfBaseline.ToString("0.000000") + " world-units"
                + "; projection m02 L=" + lOLeftProjection.m02.ToString("0.000000")
                + "; R=" + lORightProjection.m02.ToString("0.000000")
                + "; XR mode=" + XRSettings.stereoRenderingMode
                + "; baseline sign=" + (lfBaseline > 0.0001f ? "NORMAL"
                    : lfBaseline < -0.0001f ? "REVERSED?" : "ZERO/UNKNOWN")
                + ". These are runtime matrix values, not proof of actual lens fusion.");
            mbReportedStereoGeometry = true;
        }
        catch (System.Exception lOException)
        {
            Debug.LogWarning("[UWVR-EYE-GEOMETRY] XR stereo view/projection query unavailable: "
                + lOException.GetType().Name + ".");
            mbReportedStereoGeometry = true;
        }
    }

    private void UpdatePixels(int piPixels)
    {
        int liNewPixels = ClampPixels(piPixels);
        if (msShiftPixels == liNewPixels)
            return;
        msShiftPixels = liNewPixels;
        Debug.Log("[UWVR-EYE-SHIFT] left=" + msShiftPixels
            + "px (normalized=" + NormalizedOffset(msShiftPixels, msLastRenderEyeWidth)
            + "; renderWidth=" + msLastRenderEyeWidth
            + "); right=UNCHANGED; appFocused=" + Application.isFocused
            + "; arrow right=left image moves right; Shift+arrow=32px; F6=reset.");
    }

    private void UpdateOverlay()
    {
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        if (lOCamera == null || !lOCamera.isActiveAndEnabled)
        {
            if (mOverlayCanvas != null)
                mOverlayCanvas.enabled = false;
            return;
        }

        if (mOverlayCanvas == null)
            BuildOverlay();
        if (mOverlayCanvas == null)
            return;

        if (mOverlayCamera != lOCamera)
        {
            mOverlayCamera = lOCamera;
            RectTransform lORect = (RectTransform)mOverlayCanvas.transform;
            lORect.SetParent(lOCamera.transform, false);
            lORect.localPosition = new Vector3(0f, -0.56f, 1.9f);
            lORect.localRotation = Quaternion.identity;
            lORect.localScale = Vector3.one * 0.004f;
            mOverlayCanvas.worldCamera = lOCamera;
        }

        mOverlayCanvas.enabled = true;
        mOverlayText.text = "LEFT EYE " + (msShiftPixels >= 0 ? "+" : "")
            + msShiftPixels + " / " + MaxPixels + " px     RIGHT EYE FIXED"
            + "\n[<] [>] 4px    SHIFT+[<] [>] 32px    F6 = RESET";
    }

    private void BuildOverlay()
    {
        GameObject lORoot = new GameObject("UW VR LEFT eye shift status (diagnostic)",
            typeof(RectTransform), typeof(Canvas));
        lORoot.transform.SetParent(transform, false);
        mOverlayCanvas = lORoot.GetComponent<Canvas>();
        mOverlayCanvas.renderMode = RenderMode.WorldSpace;
        mOverlayCanvas.sortingOrder = 32002;
        RectTransform lORect = (RectTransform)lORoot.transform;
        lORect.sizeDelta = new Vector2(640f, 66f);

        GameObject lOBackground = new GameObject("Dark label background",
            typeof(RectTransform), typeof(Image));
        lOBackground.transform.SetParent(lORect, false);
        RectTransform lOBgRect = (RectTransform)lOBackground.transform;
        lOBgRect.anchorMin = Vector2.zero;
        lOBgRect.anchorMax = Vector2.one;
        lOBgRect.offsetMin = Vector2.zero;
        lOBgRect.offsetMax = Vector2.zero;
        Image lOBg = lOBackground.GetComponent<Image>();
        lOBg.color = new Color(0.02f, 0.025f, 0.04f, 0.78f);
        lOBg.raycastTarget = false;

        GameObject lOTextObject = new GameObject("Left eye value",
            typeof(RectTransform), typeof(Text));
        lOTextObject.transform.SetParent(lORect, false);
        RectTransform lOTextRect = (RectTransform)lOTextObject.transform;
        lOTextRect.anchorMin = Vector2.zero;
        lOTextRect.anchorMax = Vector2.one;
        lOTextRect.offsetMin = Vector2.zero;
        lOTextRect.offsetMax = Vector2.zero;
        mOverlayText = lOTextObject.GetComponent<Text>();
        mOverlayText.font = UWInterfaceFont.Font;
        mOverlayText.fontSize = 16;
        mOverlayText.alignment = TextAnchor.MiddleCenter;
        mOverlayText.color = Color.white;
        mOverlayText.raycastTarget = false;
    }

    private void OnDisable()
    {
        Shader.SetGlobalFloat(msShiftProperty, 0f);
        if (mOverlayCanvas != null)
            mOverlayCanvas.enabled = false;
    }

    private void OnDestroy()
    {
        Shader.SetGlobalFloat(msShiftProperty, 0f);
        msShiftPixels = 0;
    }
}
