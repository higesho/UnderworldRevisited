using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VR compatibility layer for the existing 2D UGUI canvases. The game's UI
/// creates ScreenSpaceOverlay canvases dynamically, which do not render into
/// stereo XR eye textures. ScreenSpaceCamera retains the existing layout,
/// sorting and gamepad / mouse event system while making both eyes see it.
///
/// This is an intermediate head-locked UI. True physical panels and tracked
/// controller interaction are a separate milestone. Nothing changes outside
/// active VR and the original canvas settings can be restored.
/// </summary>
public sealed class UWVRCanvasBridge : MonoBehaviour
{
    private sealed class OriginalCanvas
    {
        public RenderMode Mode;
        public Camera WorldCamera;
        public float PlaneDistance;
    }

    private readonly Dictionary<Canvas, OriginalCanvas> mOriginal =
        new Dictionary<Canvas, OriginalCanvas>();

    private Camera mLastCamera;
    private float mfNextRefreshTime;
    private int miLastCanvasCount;

    private const float RefreshInterval = 0.3f;

    private void LateUpdate()
    {
        if (!UWVRBootstrap.IsHeadsetActive)
        {
            if (mOriginal.Count > 0)
                RestoreAll();
            return;
        }

        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        if (lOCamera == null)
            return;

        bool lbCameraChanged = lOCamera != mLastCamera;
        if (!lbCameraChanged && Time.unscaledTime < mfNextRefreshTime)
            return;

        mfNextRefreshTime = Time.unscaledTime + RefreshInterval;
        mLastCamera = lOCamera;

        // Canvases are destroyed and recreated when a save is loaded or the
        // user changes screens. This bridge survives scene reloads, so it
        // must not retain dead Canvas references indefinitely.
        List<Canvas> lODead = null;
        foreach (Canvas lOTracked in mOriginal.Keys)
        {
            if (lOTracked != null)
                continue;

            if (lODead == null)
                lODead = new List<Canvas>();
            lODead.Add(lOTracked);
        }

        if (lODead != null)
            foreach (Canvas lODeadCanvas in lODead)
                mOriginal.Remove(lODeadCanvas);

        Canvas[] lOCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);

        int liNew = 0;
        foreach (Canvas lOCanvas in lOCanvases)
        {
            if (lOCanvas == null)
                continue;

            // Preserve the game UI's sortingOrder and CanvasScaler. Moving
            // entire hierarchies into world space would break their pixel
            // coordinate layout and inventory hit testing.
            if (!mOriginal.ContainsKey(lOCanvas))
            {
                if (lOCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                    continue;

                mOriginal.Add(lOCanvas, new OriginalCanvas
                {
                    Mode = lOCanvas.renderMode,
                    WorldCamera = lOCanvas.worldCamera,
                    PlaneDistance = lOCanvas.planeDistance
                });
                liNew++;
            }

            ConfigureCanvas(lOCanvas, lOCamera);
        }

        if (liNew > 0)
        {
            miLastCanvasCount += liNew;
            Debug.Log("[UWVR] Converted " + liNew + " overlay canvases for XR; total " + miLastCanvasCount);
        }
    }

    /// <summary>Separated for offline verification without an HMD.</summary>
    public static void ConfigureCanvas(Canvas pOCanvas, Camera pOCamera)
    {
        if (pOCanvas == null || pOCamera == null)
            return;

        pOCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        pOCanvas.worldCamera = pOCamera;
        pOCanvas.planeDistance = CalculatePlaneDistance(pOCamera, UWVRBootstrap.UiDistanceMeters);
    }

    /// <summary>
    /// A game unit is not necessarily one metre. Camera and stereo eye
    /// offsets both inherit XR tracking scale, so head-locked UI must be
    /// placed proportionally farther from a larger camera rig. This also
    /// applies to inventory panels built at runtime.
    /// </summary>
    public static float CalculatePlaneDistance(Camera pOCamera, float pfMeters)
    {
        if (pOCamera == null)
            return 0f;

        float lfScale = Mathf.Max(0.001f, pOCamera.transform.lossyScale.x);
        float lfNear = pOCamera.nearClipPlane + 0.15f;
        float lfFar = Mathf.Max(lfNear, pOCamera.farClipPlane - 0.15f);
        return Mathf.Clamp(pfMeters * lfScale, lfNear, lfFar);
    }

    private void RestoreAll()
    {
        foreach (KeyValuePair<Canvas, OriginalCanvas> lOEntry in mOriginal)
        {
            Canvas lOCanvas = lOEntry.Key;
            if (lOCanvas == null)
                continue;

            lOCanvas.renderMode = lOEntry.Value.Mode;
            lOCanvas.worldCamera = lOEntry.Value.WorldCamera;
            lOCanvas.planeDistance = lOEntry.Value.PlaneDistance;
        }

        mOriginal.Clear();
        mLastCamera = null;
        miLastCanvasCount = 0;
    }

    private void OnDestroy()
    {
        RestoreAll();
    }
}
