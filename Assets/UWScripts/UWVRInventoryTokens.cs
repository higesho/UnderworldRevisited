using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Reversible, visual-only raised item tokens for the optional VR spatial
/// inventory. Each token is an extruded icon at its original inventory hit
/// area, not a second inventory model or a game-world item. The cursor item
/// follows the right Touch pose after the EXISTING click has taken it; all
/// take/drop/stack/swap rules remain in UWModernBags and UWInventory.
/// No colliders or physical rigidbodies are added, so these visuals cannot
/// intercept the game's interaction rays or change gameplay physics.
/// </summary>
[DefaultExecutionOrder(950)]
public sealed class UWVRInventoryTokens : MonoBehaviour
{
    public readonly struct ItemVisual
    {
        public readonly UWObject Item;
        public readonly Texture Icon;
        public readonly Rect ScreenRect;

        public ItemVisual(UWObject pOItem, Texture pOIcon, Rect pORect)
        {
            Item = pOItem;
            Icon = pOIcon;
            ScreenRect = pORect;
        }
    }

    private sealed class TokenView
    {
        public Transform Root;
        public MeshFilter BodyMesh;
        public MeshRenderer Body;
        public MeshRenderer Face;
        public Material FaceMaterial;
        public UWObject Item;
    }

    private readonly List<ItemVisual> mItems = new List<ItemVisual>(64);
    private readonly List<TokenView> mViews = new List<TokenView>(64);
    private readonly Dictionary<UWVRItemGeometry.Form, Mesh> mShapeMeshes
        = new Dictionary<UWVRItemGeometry.Form, Mesh>();
    private TokenView mHeldView;
    private Material mBodyMaterial;
    private MaterialPropertyBlock mHoverMaterialProperties;
    private Mesh mQuad;
    private UWObject mHeldItem;
    private UWTexture mHeldSource;
    private Texture2D mHeldIcon;

    // The panel is 1.65 m wide by default. These are real metres before
    // multiplying by the XR tracking rig's game-units-per-metre calibration.
    private const float BaseThicknessMetres = 0.015f;
    private const float RaisedFromPanelMetres = 0.023f;
    private const float TouchHoldDistanceMetres = 0.15f;
    private const int MaxTokens = 112;

    /// <summary>Screen-space UV, clipping to the actual display rectangle.
    /// Invalid/off-screen slots never produce a 3D object.</summary>
    public static bool TryGetSlotUV(Rect pORect, float pfWidth, float pfHeight, out Rect pOUV)
    {
        pOUV = Rect.zero;
        if (pfWidth <= 0f || pfHeight <= 0f
            || pORect.width <= 0f || pORect.height <= 0f)
            return false;

        float lfX0 = Mathf.Clamp(pORect.xMin, 0f, pfWidth);
        float lfX1 = Mathf.Clamp(pORect.xMax, 0f, pfWidth);
        float lfY0 = Mathf.Clamp(pORect.yMin, 0f, pfHeight);
        float lfY1 = Mathf.Clamp(pORect.yMax, 0f, pfHeight);
        if (lfX1 <= lfX0 || lfY1 <= lfY0)
            return false;

        pOUV = new Rect(lfX0 / pfWidth, lfY0 / pfHeight,
            (lfX1 - lfX0) / pfWidth, (lfY1 - lfY0) / pfHeight);
        return true;
    }

    /// <summary>Visual thickness/aspect depends on item category. The icon is
    /// still the genuine in-game graphic; this first pass is NOT a new mesh
    /// for every sword, potion or suit of armour.</summary>
    public static Vector3 CategoryShape(UWObject.ObjectCategoryEnum peCategory)
    {
        switch (peCategory)
        {
            case UWObject.ObjectCategoryEnum.WeaponsAndMissiles:
            case UWObject.ObjectCategoryEnum.Wands:
                return new Vector3(0.75f, 0.95f, 1.65f);
            case UWObject.ObjectCategoryEnum.ArmorAndClothing:
                return new Vector3(0.92f, 0.92f, 1.20f);
            case UWObject.ObjectCategoryEnum.Containers:
                return new Vector3(0.9f, 0.88f, 2.0f);
            case UWObject.ObjectCategoryEnum.RunesAndKeyOfInfinityParts:
            case UWObject.ObjectCategoryEnum.KeysLockpickLock:
                return new Vector3(0.78f, 0.88f, 0.85f);
            case UWObject.ObjectCategoryEnum.BooksAndScrolls:
                return new Vector3(0.95f, 0.83f, 0.70f);
            default:
                return new Vector3(0.9f, 0.9f, 1f);
        }
    }

    public static Vector3 HeldPosition(Ray pORay, float pfUnitsPerMetre)
    {
        return pORay.origin + pORay.direction.normalized
            * (TouchHoldDistanceMetres * Mathf.Max(0.001f, pfUnitsPerMetre));
    }

    private void LateUpdate()
    {
        Transform lOPanel = UWVRSpatialInventory.VisiblePanel;
        Camera lOEye = UWVRBootstrap.CurrentVRCamera;
        if (lOPanel == null || lOEye == null)
        {
            HideAll();
            return;
        }

        if (!EnsureMaterials())
            return;

        mItems.Clear();
        if (UWModernBags.Instance != null)
            UWModernBags.Instance.AppendVRItemVisuals(mItems);
        if (UWModernPanel.Instance != null)
            UWModernPanel.Instance.AppendVRItemVisuals(mItems);

        float lfUnits = Mathf.Max(0.001f, lOEye.transform.lossyScale.x);
        int liShown = 0;
        for (int liAt = 0; liAt < mItems.Count && liShown < MaxTokens; liAt++)
        {
            ItemVisual lOVisual = mItems[liAt];
            if (lOVisual.Item == null || lOVisual.Icon == null
                || !TryGetSlotUV(lOVisual.ScreenRect, Screen.width, Screen.height, out Rect lOUV))
                continue;

            TokenView lOToken = GetToken(liShown++, lOPanel);
            lOToken.Item = lOVisual.Item;
            SetIcon(lOToken, lOVisual.Icon);
            SetShape(lOToken, UWVRItemGeometry.Classify(lOVisual.Item));
            SetHovered(lOToken, lOVisual.Item == UWVRNearGrab.HoveredItem);

            Vector3 lOShape = CategoryShape(lOVisual.Item.GetCategory());
            lOToken.Root.localPosition = new Vector3(lOUV.center.x - 0.5f,
                lOUV.center.y - 0.5f, -RaisedFromPanelMetres * lfUnits);
            lOToken.Root.localRotation = Quaternion.identity;
            lOToken.Root.localScale = new Vector3(lOUV.width * lOShape.x * 0.94f,
                lOUV.height * lOShape.y * 0.94f, BaseThicknessMetres * lfUnits * lOShape.z);
            lOToken.Root.gameObject.SetActive(true);
        }

        for (int liAt = liShown; liAt < mViews.Count; liAt++)
            mViews[liAt].Root.gameObject.SetActive(false);

        UpdateHeldToken(lOEye, lfUnits);
    }

    private void UpdateHeldToken(Camera pOEye, float pfUnits)
    {
        // CursorItem is the one authoritative item being carried by the
        // existing inventory model. In click-and-click mode it remains in the
        // cursor between trigger presses, just like the original interface.
        UWInventory lOInventory = UWScene.Inventory;
        UWObject lOItem = lOInventory != null ? lOInventory.CursorItem : null;
        UWVRHeadTracking lOTracking = pOEye.GetComponent<UWVRHeadTracking>();
        if (lOItem == null || lOTracking == null
            || !lOTracking.TryGetControllerRay(XRNode.RightHand, out Ray lOHandRay))
        {
            if (mHeldView != null)
                mHeldView.Root.gameObject.SetActive(false);
            return;
        }

        UWTexture lOSource = lOItem.Icon ?? lOItem.Texture;
        if (lOSource == null)
        {
            if (mHeldView != null)
                mHeldView.Root.gameObject.SetActive(false);
            return;
        }

        if (mHeldView == null)
            mHeldView = CreateToken("UW VR Held Item", transform);

        if (mHeldItem != lOItem || mHeldSource != lOSource || mHeldIcon == null)
        {
            if (mHeldIcon != null)
                Destroy(mHeldIcon);

            mHeldItem = lOItem;
            mHeldSource = lOSource;
            UWGameUI lOUi = UWScene.GameUi;
            mHeldIcon = UWIconTextureBuilder.Build(lOSource,
                lOUi != null ? lOUi.TextureFilterMode : FilterMode.Point);
        }

        SetIcon(mHeldView, mHeldIcon);
        SetShape(mHeldView, UWVRItemGeometry.Classify(lOItem));
        Vector3 lOShape = CategoryShape(lOItem.GetCategory());
        mHeldView.Root.position = HeldPosition(lOHandRay, pfUnits);
        // Aim the token icon back towards the wearer, independent of the
        // hand's yaw or roll; its face always looks out along local -Z.
        mHeldView.Root.rotation = Quaternion.LookRotation(
            mHeldView.Root.position - pOEye.transform.position, Vector3.up);
        mHeldView.Root.localScale = new Vector3(0.09f * pfUnits * lOShape.x,
            0.09f * pfUnits * lOShape.y, BaseThicknessMetres * pfUnits * lOShape.z);
        mHeldView.Root.gameObject.SetActive(true);
    }

    private TokenView GetToken(int piIndex, Transform pOPanel)
    {
        while (mViews.Count <= piIndex)
            mViews.Add(CreateToken("UW VR Item " + mViews.Count, pOPanel));

        TokenView lOToken = mViews[piIndex];
        if (lOToken.Root.parent != pOPanel)
            lOToken.Root.SetParent(pOPanel, false);
        return lOToken;
    }

    private TokenView CreateToken(string psName, Transform pOParent)
    {
        // A render-only mesh from the beginning; never instantiate a physics
        // collider that could grab focus away from the original item system.
        GameObject lOBody = new GameObject(psName, typeof(MeshFilter), typeof(MeshRenderer));
        lOBody.transform.SetParent(pOParent, false);
        MeshRenderer lOBodyRenderer = lOBody.GetComponent<MeshRenderer>();
        lOBodyRenderer.sharedMaterial = mBodyMaterial;
        lOBodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lOBodyRenderer.receiveShadows = false;

        GameObject lOFace = new GameObject("Original game icon", typeof(MeshFilter), typeof(MeshRenderer));
        lOFace.transform.SetParent(lOBody.transform, false);
        lOFace.transform.localPosition = new Vector3(0f, 0f, -0.515f);
        lOFace.transform.localScale = new Vector3(0.92f, 0.92f, 1f);
        lOFace.GetComponent<MeshFilter>().sharedMesh = mQuad;
        MeshRenderer lOFaceRenderer = lOFace.GetComponent<MeshRenderer>();
        lOFaceRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lOFaceRenderer.receiveShadows = false;

        return new TokenView
        {
            Root = lOBody.transform,
            BodyMesh = lOBody.GetComponent<MeshFilter>(),
            Body = lOBodyRenderer,
            Face = lOFaceRenderer,
            FaceMaterial = new Material(mFaceTemplate)
        };
    }

    private void SetShape(TokenView pOToken, UWVRItemGeometry.Form peForm)
    {
        if (!mShapeMeshes.TryGetValue(peForm, out Mesh lOMesh))
        {
            lOMesh = UWVRItemGeometry.CreateMesh(peForm);
            mShapeMeshes.Add(peForm, lOMesh);
        }

        if (pOToken.BodyMesh.sharedMesh != lOMesh)
            pOToken.BodyMesh.sharedMesh = lOMesh;

        // Keep the authentic 2D icon visible, but expose the genuine shape's
        // silhouette all around it; this is not merely a rectangular slab.
        Vector2 lOIconSize = UWVRItemGeometry.IconFaceScale(peForm);
        pOToken.Face.transform.localScale = new Vector3(lOIconSize.x, lOIconSize.y, 1f);
    }

    /// <summary>The hand's nearest valid item becomes visibly brighter. A
    /// property block keeps the shared item material intact and avoids one
    /// new Material allocation per hover change.</summary>
    private void SetHovered(TokenView pOToken, bool pbHovered)
    {
        if (mHoverMaterialProperties == null)
            mHoverMaterialProperties = new MaterialPropertyBlock();

        mHoverMaterialProperties.Clear();
        if (pbHovered)
        {
            Color lOHighlight = new Color(0.72f, 0.5f, 0.17f, 1f);
            mHoverMaterialProperties.SetColor("_BaseColor", lOHighlight);
            mHoverMaterialProperties.SetColor("_Color", lOHighlight);
        }

        pOToken.Body.SetPropertyBlock(mHoverMaterialProperties);
    }

    private Material mFaceTemplate;

    private bool EnsureMaterials()
    {
        if (mBodyMaterial != null)
            return true;

        Shader lOShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lOShader == null)
            lOShader = Shader.Find("Unlit/Texture");
        if (lOShader == null)
            return false;

        mBodyMaterial = new Material(lOShader) { name = "UW VR item token body" };
        mFaceTemplate = new Material(lOShader) { name = "UW VR item icon material", renderQueue = 3010 };
        if (mBodyMaterial.HasProperty("_Cull"))
            mBodyMaterial.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        if (mFaceTemplate.HasProperty("_Cull"))
            mFaceTemplate.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        if (mFaceTemplate.HasProperty("_Surface"))
        {
            mFaceTemplate.SetFloat("_Surface", 1f);
            mFaceTemplate.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mFaceTemplate.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mFaceTemplate.SetFloat("_ZWrite", 0f);
            mFaceTemplate.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        Color lOBodyColor = new Color(0.21f, 0.15f, 0.10f, 1f);
        if (mBodyMaterial.HasProperty("_BaseColor"))
            mBodyMaterial.SetColor("_BaseColor", lOBodyColor);
        if (mBodyMaterial.HasProperty("_Color"))
            mBodyMaterial.SetColor("_Color", lOBodyColor);

        mQuad = new Mesh { name = "UW VR token face" };
        mQuad.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f), new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f), new Vector3(-0.5f, 0.5f, 0f)
        };
        mQuad.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        mQuad.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        mQuad.RecalculateNormals();
        return true;
    }

    private static void SetIcon(TokenView pOToken, Texture pOTexture)
    {
        if (pOToken.FaceMaterial == null)
            return;

        if (pOToken.FaceMaterial.HasProperty("_BaseMap"))
            pOToken.FaceMaterial.SetTexture("_BaseMap", pOTexture);
        else
            pOToken.FaceMaterial.mainTexture = pOTexture;
        pOToken.Face.sharedMaterial = pOToken.FaceMaterial;
    }

    private void HideAll()
    {
        foreach (TokenView lOView in mViews)
            if (lOView.Root != null)
                lOView.Root.gameObject.SetActive(false);
        if (mHeldView != null && mHeldView.Root != null)
            mHeldView.Root.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        foreach (TokenView lOView in mViews)
            if (lOView.FaceMaterial != null)
                Destroy(lOView.FaceMaterial);
        if (mHeldView != null && mHeldView.FaceMaterial != null)
            Destroy(mHeldView.FaceMaterial);
        if (mHeldIcon != null)
            Destroy(mHeldIcon);
        if (mFaceTemplate != null)
            Destroy(mFaceTemplate);
        if (mBodyMaterial != null)
            Destroy(mBodyMaterial);
        if (mQuad != null)
            Destroy(mQuad);
        foreach (Mesh lOMesh in mShapeMeshes.Values)
            if (lOMesh != null)
                Destroy(lOMesh);
        mShapeMeshes.Clear();
    }
}
