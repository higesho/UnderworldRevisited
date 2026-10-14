using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Optional first spatial-inventory prototype (-vr-spatial-inventory).
/// The existing bags and character screen render into a transparent texture;
/// that texture lives on a world-anchored panel instead of a head-locked HUD.
/// The right Touch controller's tracked pointing ray maps back to the SAME
/// original mouse coordinates. No item/equipment logic is duplicated.
///
/// The source canvases still use ScreenSpaceCamera, but point to a separate
/// camera whose target is the panel's RenderTexture. Other HUD canvases keep
/// their normal XR stereo bridge. The panel is flat, not a collection of 3D
/// items or a physically grabbed backpack.
/// </summary>
[DefaultExecutionOrder(900)]
public sealed class UWVRSpatialInventory : MonoBehaviour
{
    private const float PanelWidthMeters = 1.65f;
    private const float RayDotMeters = 0.016f;

    private static UWVRSpatialInventory msInstance;

    private Camera mUICamera;
    private Camera mViewCamera;
    private RenderTexture mTexture;
    private Material mPanelMaterial;
    private Material mRayMaterial;
    private Mesh mQuadMesh;
    private Transform mPanel;
    private MeshRenderer mPanelRenderer;
    private LineRenderer mRayLine;
    private Transform mRayDot;
    private MeshRenderer mRayDotRenderer;
    private bool mbWasOpen;
    private bool mbTouchHit;

    public static bool IsEnabled => UWVRBootstrap.SpatialInventoryRequested && UWVRBootstrap.IsHeadsetActive;
    public static bool HasTouchHit => msInstance != null && msInstance.mbTouchHit;

    private void Awake()
    {
        msInstance = this;
    }

    public static bool IsInventoryCanvas(string psName)
    {
        return string.Equals(psName, "Modern bags", StringComparison.Ordinal)
            || string.Equals(psName, "Modern character panel", StringComparison.Ordinal);
    }

    /// <summary>Called by UWVRCanvasBridge as canvases appear or reload.</summary>
    public static bool TryConfigureSourceCanvas(Canvas pOCanvas)
    {
        if (!IsEnabled || msInstance == null || pOCanvas == null
            || !IsInventoryCanvas(pOCanvas.name))
            return false;

        if (!msInstance.EnsureResources())
            return false;

        pOCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        pOCanvas.worldCamera = msInstance.mUICamera;
        pOCanvas.planeDistance = 1.25f;
        return true;
    }

    private bool IsShown()
    {
        return (UWModernBags.Instance != null && UWModernBags.Instance.IsOpen)
            || (UWModernPanel.Instance != null && UWModernPanel.Instance.IsOpen);
    }

    private void Update()
    {
        mbTouchHit = false;

        Camera lOView = UWVRBootstrap.CurrentVRCamera;
        if (!IsEnabled || lOView == null || !IsShown() || !EnsureResources())
        {
            mbWasOpen = false;
            SetVisible(false);
            return;
        }

        bool lbReanchor = !mbWasOpen || lOView != mViewCamera
            || (Keyboard.current != null && Keyboard.current.f8Key.wasPressedThisFrame);
        if (lbReanchor)
            AnchorInWorld(lOView);

        mViewCamera = lOView;
        mbWasOpen = true;
        SetVisible(true);

        // This is purely a pointing aid; the real mouse click/drag is still
        // delivered by UWGamepadPointer on its subsequent Update (order 1000).
        if (TryTouchHit(out _, out Ray lORay, out Vector3 lOHit))
        {
            mbTouchHit = true;
            float lfUnits = TrackingUnitsPerMetre(lOView);
            mRayLine.startWidth = 0.004f * lfUnits;
            mRayLine.endWidth = 0.002f * lfUnits;
            mRayLine.SetPosition(0, lORay.origin);
            mRayLine.SetPosition(1, lOHit);
            mRayLine.enabled = true;
            mRayDot.position = lOHit - (mPanel.forward * (0.01f * lfUnits));
            mRayDot.rotation = mPanel.rotation;
            mRayDot.localScale = Vector3.one * (RayDotMeters * lfUnits);
            mRayDotRenderer.enabled = true;
        }
        else
            SetRayVisible(false);
    }

    public static bool TryGetTouchScreenPoint(out Vector2 pOScreenPoint)
    {
        pOScreenPoint = Vector2.zero;
        return IsEnabled && msInstance != null && msInstance.TryTouchHit(
            out pOScreenPoint, out _, out _);
    }

    private bool TryTouchHit(out Vector2 pOScreenPoint, out Ray pORay, out Vector3 pOHit)
    {
        pOScreenPoint = Vector2.zero;
        pORay = default;
        pOHit = Vector3.zero;

        if (!IsEnabled || mPanel == null || mPanelRenderer == null
            || !mPanelRenderer.enabled || mViewCamera == null)
            return false;

        UWVRHeadTracking lOTracking = mViewCamera.GetComponent<UWVRHeadTracking>();
        if (lOTracking == null || !lOTracking.TryGetControllerRay(XRNode.RightHand, out pORay))
            return false;

        if (!TryIntersectPanel(pORay, mPanel.worldToLocalMatrix, out Vector2 lOUV))
            return false;

        pOScreenPoint = new Vector2(lOUV.x * Screen.width, lOUV.y * Screen.height);
        pOHit = mPanel.TransformPoint(new Vector3(lOUV.x - 0.5f, lOUV.y - 0.5f, 0f));
        return true;
    }

    /// <summary>
    /// Convert a physical ray to the panel's original 0..1 screen coordinates.
    /// The front faces -Z; reject points behind the hand, from behind the
    /// panel, or beyond the visible rectangle. Works with arbitrarily rotated
    /// and nonuniformly scaled panels, independent of OpenXR hardware.
    /// </summary>
    public static bool TryIntersectPanel(Ray pORay, Matrix4x4 pOWorldToLocal, out Vector2 pOUV)
    {
        pOUV = Vector2.zero;
        Vector3 lOOrigin = pOWorldToLocal.MultiplyPoint3x4(pORay.origin);
        Vector3 lODirection = pOWorldToLocal.MultiplyVector(pORay.direction);

        if (lODirection.z <= 0.000001f)
            return false;

        float lfT = -lOOrigin.z / lODirection.z;
        if (lfT <= 0f)
            return false;

        Vector3 lOAt = lOOrigin + lODirection * lfT;
        if (lOAt.x < -0.5f || lOAt.x > 0.5f
            || lOAt.y < -0.5f || lOAt.y > 0.5f)
            return false;

        pOUV = new Vector2(lOAt.x + 0.5f, lOAt.y + 0.5f);
        return true;
    }

    private static float TrackingUnitsPerMetre(Camera pOCamera)
    {
        return pOCamera != null && pOCamera.transform.parent != null
            ? Mathf.Max(0.001f, Mathf.Abs(pOCamera.transform.parent.lossyScale.x)) : 1f;
    }

    private void AnchorInWorld(Camera pOCamera)
    {
        float lfUnits = TrackingUnitsPerMetre(pOCamera);
        Vector3 lOForward = Vector3.ProjectOnPlane(pOCamera.transform.forward, Vector3.up);
        if (lOForward.sqrMagnitude < 0.001f)
            lOForward = Vector3.ProjectOnPlane(pOCamera.transform.up, Vector3.up);
        if (lOForward.sqrMagnitude < 0.001f)
            lOForward = Vector3.forward;
        lOForward.Normalize();

        mPanel.position = pOCamera.transform.position + lOForward * (UWVRBootstrap.InventoryDistanceMeters * lfUnits);
        mPanel.rotation = Quaternion.LookRotation(lOForward, Vector3.up);
        mPanel.localScale = new Vector3(PanelWidthMeters * lfUnits,
            PanelWidthMeters * lfUnits * Screen.height / Mathf.Max(1f, Screen.width), 1f);
        Debug.Log("[UWVR] Spatial inventory anchored in world; F8 repositions in front of the headset.");
    }

    private void SetVisible(bool pbVisible)
    {
        if (mPanelRenderer != null)
            mPanelRenderer.enabled = pbVisible;
        if (mUICamera != null)
            mUICamera.enabled = pbVisible;
        if (!pbVisible)
            SetRayVisible(false);
    }

    private void SetRayVisible(bool pbVisible)
    {
        if (mRayLine != null)
            mRayLine.enabled = pbVisible;
        if (mRayDotRenderer != null)
            mRayDotRenderer.enabled = pbVisible;
    }

    private bool EnsureResources()
    {
        if (mPanel == null)
        {
            mQuadMesh = new Mesh { name = "UW VR spatial inventory quad" };
            mQuadMesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
            };
            mQuadMesh.uv = new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 1f), new Vector2(0f, 1f)
            };
            mQuadMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mQuadMesh.RecalculateNormals();
            mQuadMesh.RecalculateBounds();

            Shader lOShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (lOShader == null)
                lOShader = Shader.Find("Sprites/Default");
            if (lOShader == null)
            {
                Debug.LogError("[UWVR] No transparent shader for spatial inventory panel.");
                return false;
            }

            mPanelMaterial = MakeTransparent(lOShader, Color.white);
            mRayMaterial = MakeTransparent(lOShader, new Color(0.95f, 0.75f, 0.25f, 0.92f));

            GameObject lOPanelObject = new GameObject("UW VR World Inventory", typeof(MeshFilter), typeof(MeshRenderer));
            lOPanelObject.transform.SetParent(transform, false);
            mPanel = lOPanelObject.transform;
            lOPanelObject.GetComponent<MeshFilter>().sharedMesh = mQuadMesh;
            mPanelRenderer = lOPanelObject.GetComponent<MeshRenderer>();
            mPanelRenderer.sharedMaterial = mPanelMaterial;
            mPanelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mPanelRenderer.receiveShadows = false;

            GameObject lODot = new GameObject("UW VR Touch Laser Hit", typeof(MeshFilter), typeof(MeshRenderer));
            lODot.transform.SetParent(transform, false);
            lODot.GetComponent<MeshFilter>().sharedMesh = mQuadMesh;
            mRayDot = lODot.transform;
            mRayDotRenderer = lODot.GetComponent<MeshRenderer>();
            mRayDotRenderer.sharedMaterial = mRayMaterial;
            mRayDotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            GameObject lORayObject = new GameObject("UW VR Touch Laser", typeof(LineRenderer));
            lORayObject.transform.SetParent(transform, false);
            mRayLine = lORayObject.GetComponent<LineRenderer>();
            mRayLine.sharedMaterial = mRayMaterial;
            mRayLine.positionCount = 2;
            mRayLine.useWorldSpace = true;
            mRayLine.numCapVertices = 3;
            SetRayVisible(false);
            mPanelRenderer.enabled = false;
        }

        if (mUICamera == null)
        {
            GameObject lOCameraObject = new GameObject("UW VR Inventory Texture Camera", typeof(Camera));
            lOCameraObject.transform.SetParent(transform, false);
            lOCameraObject.transform.localPosition = new Vector3(0f, -10000f, 0f);
            mUICamera = lOCameraObject.GetComponent<Camera>();
            mUICamera.clearFlags = CameraClearFlags.SolidColor;
            mUICamera.backgroundColor = Color.clear;
            mUICamera.stereoTargetEye = StereoTargetEyeMask.None;
            mUICamera.nearClipPlane = 0.05f;
            mUICamera.farClipPlane = 10f;
            mUICamera.allowHDR = false;
            mUICamera.allowMSAA = false;
            mUICamera.depth = -200f;
            mUICamera.enabled = false;
        }

        int liWidth = Mathf.Max(1, Screen.width);
        int liHeight = Mathf.Max(1, Screen.height);
        if (mTexture == null || mTexture.width != liWidth || mTexture.height != liHeight)
        {
            if (mTexture != null)
            {
                mUICamera.targetTexture = null;
                mTexture.Release();
                Destroy(mTexture);
            }

            mTexture = new RenderTexture(liWidth, liHeight, 16, RenderTextureFormat.ARGB32)
            {
                name = "UW VR spatial inventory source",
                filterMode = FilterMode.Bilinear,
                useMipMap = false
            };
            if (!mTexture.Create())
            {
                Debug.LogError("[UWVR] Spatial inventory RenderTexture allocation failed.");
                return false;
            }

            mUICamera.targetTexture = mTexture;
            if (mPanelMaterial.HasProperty("_BaseMap"))
                mPanelMaterial.SetTexture("_BaseMap", mTexture);
            else
                mPanelMaterial.mainTexture = mTexture;
        }

        return true;
    }

    private static Material MakeTransparent(Shader pOShader, Color pOColor)
    {
        Material lOMaterial = new Material(pOShader) { renderQueue = 3000 };
        if (lOMaterial.HasProperty("_Surface"))
        {
            lOMaterial.SetFloat("_Surface", 1f);
            lOMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            lOMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            lOMaterial.SetFloat("_ZWrite", 0f);
            lOMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        if (lOMaterial.HasProperty("_BaseColor"))
            lOMaterial.SetColor("_BaseColor", pOColor);
        if (lOMaterial.HasProperty("_Color"))
            lOMaterial.SetColor("_Color", pOColor);
        return lOMaterial;
    }

    private void OnDestroy()
    {
        if (msInstance == this)
            msInstance = null;
        if (mUICamera != null)
            mUICamera.targetTexture = null;
        if (mTexture != null)
        {
            mTexture.Release();
            Destroy(mTexture);
        }
        if (mPanelMaterial != null)
            Destroy(mPanelMaterial);
        if (mRayMaterial != null)
            Destroy(mRayMaterial);
        if (mQuadMesh != null)
            Destroy(mQuadMesh);
    }
}
