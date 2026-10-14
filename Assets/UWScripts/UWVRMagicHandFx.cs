using UnityEngine;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Opt-in hand-scale spell visuals only. An orb follows the tracked left Touch
/// while a projectile spell is pending; successful original projectile launch
/// creates a short-lived emissive ring/flash at the muzzle. This is NOT a
/// separate projectile and cannot hit or alter any original damage/mana.
/// All meshes are collider-free. Nothing is drawn in the desktop game.
/// </summary>
[DefaultExecutionOrder(940)]
public sealed class UWVRMagicHandFx : MonoBehaviour
{
    public const float FlashLifetimeSeconds = 0.42f;
    private static UWVRMagicHandFx msInstance;
    private Transform mPreview;
    private Transform mFlash;
    private Transform mRing;
    private Mesh mOrbMesh;
    private Mesh mRingMesh;
    private Material mPreviewMaterial;
    private Material mFlashMaterial;
    private Material mRingMaterial;
    private float mfFlashAge = FlashLifetimeSeconds;
    private float mfFlashUnits = 1f;
    private Color mFlashColor;
    private bool mbReported;

    public static bool IsEnabled => UWVRHandLaunch.IsEnabled
        && UWVRBootstrap.HandMagicFxRequested;

    private void Awake()
    {
        msInstance = this;
    }

    /// <summary>Only Interaction calls this, AFTER the game has actually
    /// created a projectile. If the original collision/placement failed, no
    /// flash plays and no false successful cast is suggested to the player.</summary>
    public static void NotifySuccessfulLaunch(Ray pOLeftMuzzle, int piDamageType)
    {
        if (IsEnabled && msInstance != null)
            msInstance.BeginFlash(pOLeftMuzzle, piDamageType);
    }

    public static Color ColorForDamage(int piDamageType)
    {
        if ((piDamageType & UWDamageTypes.PlainFire) != 0)
            return new Color(1f, 0.39f, 0.09f, 0.92f);
        if (piDamageType == UWDamageTypes.Cold)
            return new Color(0.35f, 0.77f, 1f, 0.9f);
        if (piDamageType == UWDamageTypes.Poison)
            return new Color(0.33f, 1f, 0.34f, 0.9f);
        return new Color(0.40f, 0.52f, 1f, 0.90f);
    }

    public static float FlashRadiusMetres(float pfAgeSeconds)
    {
        float lfT = Mathf.Clamp01(pfAgeSeconds / FlashLifetimeSeconds);
        return Mathf.Lerp(0.07f, 0.30f, lfT);
    }

    public static float FlashOpacity(float pfAgeSeconds)
    {
        float lfT = Mathf.Clamp01(pfAgeSeconds / FlashLifetimeSeconds);
        return (1f - lfT) * (1f - lfT);
    }

    /// <summary>Octahedral glow volume: deliberate real 3D silhouette, no
    /// GameObject.CreatePrimitive collision component.</summary>
    public static Mesh CreateOrbMesh()
    {
        Mesh lOMesh = new Mesh { name = "UW VR spell muzzle orb" };
        Vector3[] lOVertices = {
            Vector3.up, Vector3.down, Vector3.right, Vector3.left,
            Vector3.forward, Vector3.back
        };
        int[] lOTriangles = {
            0, 4, 2, 0, 3, 4, 0, 5, 3, 0, 2, 5,
            1, 2, 4, 1, 4, 3, 1, 3, 5, 1, 5, 2
        };
        lOMesh.vertices = lOVertices;
        lOMesh.triangles = lOTriangles;
        lOMesh.RecalculateNormals();
        lOMesh.RecalculateBounds();
        return lOMesh;
    }

    /// <summary>Flat two-sided ring facing the launch direction. Inner and
    /// outer radii 0.72 and 1 relative to the mesh's local radius.</summary>
    public static Mesh CreateRingMesh(int piSegments = 24)
    {
        int liSegments = Mathf.Clamp(piSegments, 8, 64);
        Vector3[] lOVertices = new Vector3[(liSegments + 1) * 2];
        int[] lOTriangles = new int[liSegments * 12];
        for (int li = 0; li <= liSegments; li++)
        {
            float lfAngle = Mathf.PI * 2f * li / liSegments;
            Vector3 lODirection = new Vector3(Mathf.Cos(lfAngle), Mathf.Sin(lfAngle), 0f);
            lOVertices[2 * li] = lODirection * 0.72f;
            lOVertices[2 * li + 1] = lODirection;
            if (li == liSegments)
                continue;

            int liBase = li * 12;
            int liV = li * 2;
            lOTriangles[liBase] = liV;
            lOTriangles[liBase + 1] = liV + 1;
            lOTriangles[liBase + 2] = liV + 3;
            lOTriangles[liBase + 3] = liV;
            lOTriangles[liBase + 4] = liV + 3;
            lOTriangles[liBase + 5] = liV + 2;
            lOTriangles[liBase + 6] = liV + 3;
            lOTriangles[liBase + 7] = liV + 1;
            lOTriangles[liBase + 8] = liV;
            lOTriangles[liBase + 9] = liV + 2;
            lOTriangles[liBase + 10] = liV + 3;
            lOTriangles[liBase + 11] = liV;
        }
        Mesh lOMesh = new Mesh { name = "UW VR spell cast radial ring" };
        lOMesh.vertices = lOVertices;
        lOMesh.triangles = lOTriangles;
        lOMesh.RecalculateNormals();
        lOMesh.RecalculateBounds();
        return lOMesh;
    }

    private void BeginFlash(Ray pOLeftMuzzle, int piDamageType)
    {
        if (!UWVRAim.HasValidDirection(pOLeftMuzzle.direction)
            || !EnsureVisuals())
            return;

        mfFlashAge = 0f;
        mfFlashUnits = UWVRBootstrap.WorldUnitsPerMeter;
        mFlashColor = ColorForDamage(piDamageType);
        mFlash.position = pOLeftMuzzle.origin;
        mFlash.rotation = Quaternion.LookRotation(pOLeftMuzzle.direction.normalized);
        mRing.position = mFlash.position;
        mRing.rotation = mFlash.rotation;
        Debug.Log("[UWVR-MAGIC] successful left-hand projectile; visual flash at "
            + pOLeftMuzzle.origin + "; damage type=" + piDamageType);
    }

    private void LateUpdate()
    {
        if (!IsEnabled || !Application.isFocused)
        {
            SetVisible(false);
            return;
        }

        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        bool lbGame = lOCamera != null && lOCamera.isActiveAndEnabled
            && lOCamera.gameObject.name != "UW VR Menu Camera"
            && UWScene.Player != null && UWVRSpatialInventory.VisiblePanel == null
            && !(UWModernBags.Instance != null && UWModernBags.Instance.IsOpen);
        if (!lbGame)
        {
            SetVisible(false);
            return;
        }

        if (!EnsureVisuals())
            return;

        Interaction lOInteraction = UWScene.Interaction;
        bool lbPending = lOInteraction != null && lOInteraction.HasPendingProjectileSpell;
        Ray lOPreviewMuzzle = default;
        UWVRHandLaunch.Result lePose = lbPending
            ? UWVRHandLaunch.Resolve(lOCamera, XRNode.LeftHand, out lOPreviewMuzzle)
            : UWVRHandLaunch.Result.Untracked;

        if (mPreview != null)
        {
            bool lbPreview = lbPending && lePose == UWVRHandLaunch.Result.Ready;
            mPreview.gameObject.SetActive(lbPreview);
            if (lbPreview)
            {
                mPreview.position = lOPreviewMuzzle.origin;
                mPreview.localScale = Vector3.one * (0.055f * UWVRBootstrap.WorldUnitsPerMeter);
                mPreview.rotation = Quaternion.LookRotation(lOPreviewMuzzle.direction);
            }
        }

        if (mfFlashAge < FlashLifetimeSeconds)
        {
            mfFlashAge += Mathf.Max(0f, Time.deltaTime);
            float lfOpacity = FlashOpacity(mfFlashAge);
            float lfRadius = FlashRadiusMetres(mfFlashAge) * mfFlashUnits;
            bool lbVisible = lfOpacity > 0f;
            mFlash.gameObject.SetActive(lbVisible);
            mRing.gameObject.SetActive(lbVisible);
            mFlash.localScale = Vector3.one * (lfRadius * 0.64f);
            mRing.localScale = Vector3.one * lfRadius;
            SetColor(mFlashMaterial, new Color(mFlashColor.r, mFlashColor.g,
                mFlashColor.b, mFlashColor.a * lfOpacity));
            SetColor(mRingMaterial, new Color(mFlashColor.r, mFlashColor.g,
                mFlashColor.b, mFlashColor.a * lfOpacity));
        }
        else
        {
            mFlash.gameObject.SetActive(false);
            mRing.gameObject.SetActive(false);
        }

        if (!mbReported)
        {
            mbReported = true;
            Debug.Log("[UWVR-MAGIC] Optional 3D orb and launch ring enabled; no independent spell damage.");
        }
    }

    private bool EnsureVisuals()
    {
        if (mPreview != null && mFlash != null && mRing != null)
            return true;

        Shader lOShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lOShader == null) lOShader = Shader.Find("Sprites/Default");
        if (lOShader == null) lOShader = Shader.Find("Standard");
        if (lOShader == null)
        {
            Debug.LogWarning("[UWVR-MAGIC] Missing shader; magic remains functional without a flash.");
            enabled = false;
            return false;
        }

        mOrbMesh = CreateOrbMesh();
        mRingMesh = CreateRingMesh();
        mPreviewMaterial = MakeMaterial(lOShader, new Color(0.38f, 0.65f, 1f, 0.86f));
        mFlashMaterial = MakeMaterial(lOShader, Color.white);
        mRingMaterial = MakeMaterial(lOShader, Color.white);
        mPreview = CreatePart("UW VR Left Spell Preview", mOrbMesh, mPreviewMaterial);
        mFlash = CreatePart("UW VR Left Spell Flash", mOrbMesh, mFlashMaterial);
        mRing = CreatePart("UW VR Left Spell Expanding Ring", mRingMesh, mRingMaterial);
        mPreview.gameObject.SetActive(false);
        mFlash.gameObject.SetActive(false);
        mRing.gameObject.SetActive(false);
        return true;
    }

    private Transform CreatePart(string psName, Mesh pOMesh, Material pOMaterial)
    {
        GameObject lOObject = new GameObject(psName, typeof(MeshFilter), typeof(MeshRenderer));
        lOObject.transform.SetParent(transform, false);
        lOObject.GetComponent<MeshFilter>().sharedMesh = pOMesh;
        MeshRenderer lORenderer = lOObject.GetComponent<MeshRenderer>();
        lORenderer.sharedMaterial = pOMaterial;
        lORenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lORenderer.receiveShadows = false;
        return lOObject.transform;
    }

    private static Material MakeMaterial(Shader pOShader, Color pOColor)
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
        SetColor(lOMaterial, pOColor);
        return lOMaterial;
    }

    private static void SetColor(Material pOMaterial, Color pOColor)
    {
        if (pOMaterial == null) return;
        if (pOMaterial.HasProperty("_BaseColor")) pOMaterial.SetColor("_BaseColor", pOColor);
        if (pOMaterial.HasProperty("_Color")) pOMaterial.SetColor("_Color", pOColor);
        if (pOMaterial.HasProperty("_EmissionColor")) pOMaterial.SetColor("_EmissionColor", pOColor * 1.5f);
    }

    private void SetVisible(bool pbVisible)
    {
        if (!pbVisible)
        {
            if (mPreview != null) mPreview.gameObject.SetActive(false);
            if (mFlash != null) mFlash.gameObject.SetActive(false);
            if (mRing != null) mRing.gameObject.SetActive(false);
            mfFlashAge = FlashLifetimeSeconds;
            mbReported = false;
        }
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        SetVisible(false);
        if (msInstance == this) msInstance = null;
        if (mOrbMesh != null) Destroy(mOrbMesh);
        if (mRingMesh != null) Destroy(mRingMesh);
        if (mPreviewMaterial != null) Destroy(mPreviewMaterial);
        if (mFlashMaterial != null) Destroy(mFlashMaterial);
        if (mRingMaterial != null) Destroy(mRingMaterial);
    }
}
