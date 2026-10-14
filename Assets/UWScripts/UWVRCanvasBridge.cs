using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

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
        public Transform Parent;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
        public Vector3 LocalScale;
        public Vector2 SizeDelta;
        public UWFitCanvas Fit;
        public bool FitEnabled;
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

        Canvas[] lOCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);

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
                    PlaneDistance = lOCanvas.planeDistance,
                    Parent = lOCanvas.transform.parent,
                    LocalPosition = lOCanvas.transform.localPosition,
                    LocalRotation = lOCanvas.transform.localRotation,
                    LocalScale = lOCanvas.transform.localScale,
                    SizeDelta = ((RectTransform)lOCanvas.transform).sizeDelta,
                    Fit = lOCanvas.GetComponent<UWFitCanvas>(),
                    FitEnabled = lOCanvas.GetComponent<UWFitCanvas>() != null
                        && lOCanvas.GetComponent<UWFitCanvas>().enabled
                });
                liNew++;
            }

            if (!UWVRSpatialInventory.TryConfigureSourceCanvas(lOCanvas))
            {
                if (!UWVRBootstrap.LegacyMenuCanvasRequested && IsStereoMenuCanvas(lOCanvas.name))
                    ConfigureStereoMenuCanvas(lOCanvas, lOCamera,
                        UWVRBootstrap.MenuDistanceMeters);
                else
                    ConfigureCanvas(lOCanvas, lOCamera);
            }
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

        ConfigureCanvas(pOCanvas, pOCamera, DistanceForCanvasName(pOCanvas.name,
            UWVRBootstrap.UiDistanceMeters, UWVRBootstrap.InventoryDistanceMeters));
    }

    /// <summary>Two related inventory canvases must share one depth plane:
    /// drag/drop and mouse screen coordinates still belong to one interface.</summary>
    public static float DistanceForCanvasName(string psName, float pfUiMeters, float pfInventoryMeters)
    {
        return string.Equals(psName, "Modern bags", StringComparison.Ordinal)
            || string.Equals(psName, "Modern character panel", StringComparison.Ordinal)
            ? pfInventoryMeters : pfUiMeters;
    }

    public static void ConfigureCanvas(Canvas pOCanvas, Camera pOCamera, float pfDistanceMeters)
    {
        if (pOCanvas == null || pOCamera == null)
            return;

        pOCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        pOCanvas.worldCamera = pOCamera;
        pOCanvas.planeDistance = CalculatePlaneDistance(pOCamera, pfDistanceMeters);
    }

    /// <summary>The game's title and character-creation images are fully
    /// screen-aligned 2D artwork. ScreenSpaceCamera renders them through a
    /// stereo projection separately for each eye, which can leave the two
    /// images at different apparent locations under VDXR. Anchor ONE physical
    /// world-space sheet in front of the HMD instead: both eyes see the same
    /// actual stereo object at one comfortable distance. Do not change any
    /// 3D gameplay HUD, inventory camera or desktop canvas.</summary>
    public static bool IsStereoMenuCanvas(string psName)
    {
        return string.Equals(psName, "UW Main Menu", StringComparison.Ordinal)
            || string.Equals(psName, "UW Intro Canvas", StringComparison.Ordinal)
            || string.Equals(psName, "UW Character Creation", StringComparison.Ordinal)
            || string.Equals(psName, "UW Loading Cover", StringComparison.Ordinal);
    }

    public const float MenuPhysicalWidthMetres = 2.40f;
    public const float MenuReferenceDistanceMetres = 2.25f;

    /// <summary>Changing eye convergence distance should not also make the
    /// text unreadably small. Preserve the previous menu's angular width.
    /// The menu is still ONE physical plane for both XR eyes.</summary>
    public static float PhysicalMenuWidthAtDistance(float pfDistanceMetres)
    {
        return MenuPhysicalWidthMetres
            * Mathf.Max(0.6f, pfDistanceMetres) / MenuReferenceDistanceMetres;
    }

    /// <summary>A single centered plane, 320 by 200 logical pixels, attached
    /// to the tracked XR camera. The physical size/distance is independent of
    /// dungeon world scale; the XR plugin alone supplies the per-eye offsets.
    /// No separate eye offsets, projection hacks or one-camera-per-eye.</summary>
    public static void ConfigureStereoMenuCanvas(Canvas pOCanvas,
        Camera pOCamera, float pfDistanceMetres)
    {
        if (pOCanvas == null || pOCamera == null)
            return;

        bool lbFirst = pOCanvas.renderMode != RenderMode.WorldSpace;
        RectTransform lORect = (RectTransform)pOCanvas.transform;
        if (lORect.parent != pOCamera.transform)
            lORect.SetParent(pOCamera.transform, false);

        float lfDistance = Mathf.Max(0.6f, pfDistanceMetres);
        float lfWidth = PhysicalMenuWidthAtDistance(lfDistance);
        float lfScale = lfWidth / UWScreenUi.ScreenWidth;
        // The fade/loading cover must remain in front of the menu card.
        // Different canvas sorting orders previously accomplished this in
        // ScreenSpaceOverlay; preserve that ordering with small Z offsets.
        float lfDepthOffset = pOCanvas.name == "UW Loading Cover" ? 0.07f : 0f;
        lORect.localPosition = new Vector3(0f, 0f, lfDistance - lfDepthOffset);
        lORect.localRotation = Quaternion.identity;
        lORect.localScale = Vector3.one * lfScale;
        lORect.sizeDelta = new Vector2(UWScreenUi.ScreenWidth, UWScreenUi.ScreenHeight);

        UWFitCanvas lOFit = pOCanvas.GetComponent<UWFitCanvas>();
        if (lOFit != null)
            lOFit.enabled = false; // screen-height fitting does not apply to a physical card

        pOCanvas.renderMode = RenderMode.WorldSpace;
        pOCanvas.worldCamera = pOCamera;
        if (lbFirst)
            Debug.Log("[UWVR-STEREO] " + pOCanvas.name
                + " -> centered world-space plane; camera=" + pOCamera.name
                + "; width=" + lfWidth.ToString("0.00")
                + "m; distance=" + (lfDistance - lfDepthOffset).ToString("0.00")
                + "m; both XR eyes share the same UI geometry.");
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
            Transform lOParent = lOEntry.Value.Parent;
            if (lOParent != null)
            {
                lOCanvas.transform.SetParent(lOParent, false);
                lOCanvas.transform.localPosition = lOEntry.Value.LocalPosition;
                lOCanvas.transform.localRotation = lOEntry.Value.LocalRotation;
                lOCanvas.transform.localScale = lOEntry.Value.LocalScale;
                ((RectTransform)lOCanvas.transform).sizeDelta = lOEntry.Value.SizeDelta;
            }
            if (lOEntry.Value.Fit != null)
                lOEntry.Value.Fit.enabled = lOEntry.Value.FitEnabled;
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
