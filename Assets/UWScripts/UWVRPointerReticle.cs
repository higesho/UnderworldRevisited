using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The Windows hardware cursor is not rendered into the OpenXR eye textures.
/// Draw a small software pointer on the same stereo camera as the old UI.
/// In pointer mode it follows the existing GamepadPointer / mouse position so
/// all old inventory and menu hit tests still agree. In look mode it becomes
/// a subdued center-of-view reticle, matching UWVRAim's gaze ray.
/// </summary>
public sealed class UWVRPointerReticle : MonoBehaviour
{
    private Canvas mCanvas;
    private RectTransform mCanvasRect;
    private RectTransform mCursorRect;
    private Image[] mMarks;
    private Camera mCamera;

    private static readonly Color msPointerColor = new Color(1f, 0.86f, 0.42f, 0.92f);
    private static readonly Color msGazeColor = new Color(0.86f, 0.93f, 1f, 0.35f);

    private void LateUpdate()
    {
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        if (!UWVRBootstrap.IsHeadsetActive || lOCamera == null)
        {
            if (mCanvas != null)
                mCanvas.enabled = false;
            return;
        }

        if (mCanvas == null)
            CreateCanvas();

        mCanvas.enabled = true;

        // Put the software cursor just in front of the currently interactive
        // UI plane. Otherwise a cursor at the default HUD distance appears
        // behind the nearer stereo inventory when a bag is opened.
        bool lbInventory = (UWModernBags.Instance != null && UWModernBags.Instance.IsOpen)
            || (UWModernPanel.Instance != null && UWModernPanel.Instance.IsOpen);
        float lfDistance = (lbInventory ? UWVRBootstrap.InventoryDistanceMeters
            : UWVRBootstrap.UiDistanceMeters) - 0.04f;
        float lfDesiredPlane = UWVRCanvasBridge.CalculatePlaneDistance(lOCamera, lfDistance);

        if (mCamera != lOCamera || Mathf.Abs(mCanvas.planeDistance - lfDesiredPlane) > 0.001f)
        {
            UWVRCanvasBridge.ConfigureCanvas(mCanvas, lOCamera, lfDistance);
            mCanvas.sortingOrder = 32000;
            mCamera = lOCamera;
        }

        bool lbPointer = Cursor.lockState != CursorLockMode.Locked;
        Vector2 lOPoint;
        if (lbPointer && UWGamepadPointer.OwnsPosition)
            lOPoint = UWGamepadPointer.Position;
        else if (lbPointer && Mouse.current != null)
            lOPoint = Mouse.current.position.ReadValue();
        else
            lOPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mCanvasRect, lOPoint, mCamera, out Vector2 lOLocal))
            mCursorRect.anchoredPosition = lOLocal;

        Color lOColor = lbPointer ? msPointerColor : msGazeColor;
        foreach (Image lOMark in mMarks)
            lOMark.color = lOColor;
    }

    private void CreateCanvas()
    {
        GameObject lOCanvas = new GameObject("UW VR Cursor", typeof(RectTransform),
            typeof(Canvas), typeof(CanvasScaler));
        lOCanvas.transform.SetParent(transform, false);

        mCanvas = lOCanvas.GetComponent<Canvas>();
        mCanvas.sortingOrder = 32000;
        mCanvasRect = (RectTransform)lOCanvas.transform;

        // Screen-space-camera UI remains head locked and the pointer cannot
        // intercept a click because every mark has raycastTarget disabled.
        GameObject lOPointer = new GameObject("Pointer", typeof(RectTransform));
        lOPointer.transform.SetParent(mCanvasRect, false);
        mCursorRect = (RectTransform)lOPointer.transform;
        mCursorRect.anchorMin = new Vector2(0.5f, 0.5f);
        mCursorRect.anchorMax = mCursorRect.anchorMin;
        mCursorRect.sizeDelta = Vector2.zero;
        mCursorRect.anchoredPosition = Vector2.zero;

        mMarks = new Image[3];
        mMarks[0] = CreateMark("Horizontal", new Vector2(22f, 3f));
        mMarks[1] = CreateMark("Vertical", new Vector2(3f, 22f));
        mMarks[2] = CreateMark("Center", new Vector2(5f, 5f));
    }

    private Image CreateMark(string psName, Vector2 pOSize)
    {
        GameObject lOObject = new GameObject(psName, typeof(RectTransform), typeof(Image));
        lOObject.transform.SetParent(mCursorRect, false);
        RectTransform lORect = (RectTransform)lOObject.transform;
        lORect.anchorMin = new Vector2(0.5f, 0.5f);
        lORect.anchorMax = lORect.anchorMin;
        lORect.sizeDelta = pOSize;
        lORect.anchoredPosition = Vector2.zero;
        Image lOImage = lOObject.GetComponent<Image>();
        lOImage.raycastTarget = false;
        lOImage.color = msPointerColor;
        return lOImage;
    }
}
