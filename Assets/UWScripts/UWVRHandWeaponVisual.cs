using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Visual-only prototype melee weapon attached to the right Touch controller.
/// This maps the ORIGINAL game's weapon animation family to sword/axe/mace/
/// fist silhouettes, not a separate authored mesh for each object ID.
/// It has NO colliders or rigidbodies and cannot damage enemies on contact.
/// The original UWCharacter animation remains the authority for combat hits.
/// Shown only with -vr -vr-hand-aim -vr-gesture-melee in an active world.
/// </summary>
[DefaultExecutionOrder(900)]
public sealed class UWVRHandWeaponVisual : MonoBehaviour
{
    private Transform mRoot;
    private MeshRenderer mBlade;
    private MeshFilter mBladeFilter;
    private MeshRenderer mGuard;
    private MeshRenderer mGrip;
    private MeshRenderer mFist;
    private Mesh mBladeMesh;
    private Mesh mAxeMesh;
    private Mesh mMaceMesh;
    private Mesh mCubeMesh;
    private Material mSteelMaterial;
    private Material mGuardMaterial;
    private Material mGripMaterial;
    private bool mbReported;
    private UWVRWeaponShape.Kind meShownForm = (UWVRWeaponShape.Kind)(-1);

    /// <summary>VR UI can hide the old 2D weapon sprite only when the
    /// replacement mesh is actually following a tracked hand.</summary>
    public static bool IsModelVisible { get; private set; }

    /// <summary>Locates the controller/palm behind the grip by 6cm, scaled
    /// with the XR world metres used for all UI and stereoscopic offsets.</summary>
    public static Vector3 GripOrigin(Ray pOController, float pfUnitsPerMetre)
    {
        return pOController.origin - pOController.direction.normalized
            * (0.06f * Mathf.Max(0.001f, pfUnitsPerMetre));
    }

    /// <summary>Approximate metal blade taper, 0.15m to 0.81m ahead of the
    /// controller's forward axis, generated as genuine closed 3D geometry.</summary>
    public static Mesh CreateBladeMesh()
    {
        var lOVertices = new List<Vector3>(20);
        var lOTriangles = new List<int>(48);
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.034f, -0.009f, 0.145f),
            new Vector3(0.034f, 0.009f, 0.695f));

        int liStart = lOVertices.Count;
        lOVertices.Add(new Vector3(-0.034f, -0.009f, 0.695f));
        lOVertices.Add(new Vector3(0.034f, -0.009f, 0.695f));
        lOVertices.Add(new Vector3(0.034f, 0.009f, 0.695f));
        lOVertices.Add(new Vector3(-0.034f, 0.009f, 0.695f));
        lOVertices.Add(new Vector3(0f, 0f, 0.81f));
        AddTri(lOTriangles, liStart, liStart + 1, liStart + 4);
        AddTri(lOTriangles, liStart + 1, liStart + 2, liStart + 4);
        AddTri(lOTriangles, liStart + 2, liStart + 3, liStart + 4);
        AddTri(lOTriangles, liStart + 3, liStart, liStart + 4);

        return MakeMesh("UW VR generic melee blade", lOVertices, lOTriangles);
    }

    /// <summary>Staff and distinct heavy crescent-shaped single blade.</summary>
    public static Mesh CreateAxeMesh()
    {
        var lOVertices = new List<Vector3>(36);
        var lOTriangles = new List<int>(120);
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.023f, -0.023f, -0.09f),
            new Vector3(0.023f, 0.023f, 0.76f));
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.18f, -0.043f, 0.57f),
            new Vector3(0.075f, 0.043f, 0.75f));
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.22f, -0.018f, 0.59f),
            new Vector3(-0.17f, 0.018f, 0.72f));
        return MakeMesh("UW VR generic axe", lOVertices, lOTriangles);
    }

    /// <summary>Blunt club head, with a short haft and raised studs.</summary>
    public static Mesh CreateMaceMesh()
    {
        var lOVertices = new List<Vector3>(56);
        var lOTriangles = new List<int>(220);
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.022f, -0.022f, -0.09f),
            new Vector3(0.022f, 0.022f, 0.68f));
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.081f, -0.081f, 0.56f),
            new Vector3(0.081f, 0.081f, 0.73f));
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.105f, -0.024f, 0.61f),
            new Vector3(0.105f, 0.024f, 0.67f));
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.024f, -0.105f, 0.61f),
            new Vector3(0.024f, 0.105f, 0.67f));
        return MakeMesh("UW VR generic mace", lOVertices, lOTriangles);
    }

    public static Mesh CreateCubeMesh()
    {
        var lOVertices = new List<Vector3>(8);
        var lOTriangles = new List<int>(36);
        AppendBox(lOVertices, lOTriangles,
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f));
        return MakeMesh("UW VR weapon handle cube", lOVertices, lOTriangles);
    }

    private static Mesh MakeMesh(string psName, List<Vector3> pOVertices, List<int> pOTriangles)
    {
        Mesh lOMesh = new Mesh { name = psName };
        lOMesh.SetVertices(pOVertices);
        lOMesh.SetTriangles(pOTriangles, 0);
        lOMesh.RecalculateNormals();
        lOMesh.RecalculateBounds();
        return lOMesh;
    }

    private static void AddTri(List<int> pOIndices, int piA, int piB, int piC)
    {
        pOIndices.Add(piA);
        pOIndices.Add(piB);
        pOIndices.Add(piC);
    }

    private static void AppendBox(List<Vector3> pOVertices, List<int> pOTriangles,
        Vector3 pOMin, Vector3 pOMax)
    {
        int li = pOVertices.Count;
        pOVertices.Add(new Vector3(pOMin.x, pOMin.y, pOMin.z));
        pOVertices.Add(new Vector3(pOMax.x, pOMin.y, pOMin.z));
        pOVertices.Add(new Vector3(pOMax.x, pOMax.y, pOMin.z));
        pOVertices.Add(new Vector3(pOMin.x, pOMax.y, pOMin.z));
        pOVertices.Add(new Vector3(pOMin.x, pOMin.y, pOMax.z));
        pOVertices.Add(new Vector3(pOMax.x, pOMin.y, pOMax.z));
        pOVertices.Add(new Vector3(pOMax.x, pOMax.y, pOMax.z));
        pOVertices.Add(new Vector3(pOMin.x, pOMax.y, pOMax.z));

        int[] lOFaces = {
            0, 2, 1, 0, 3, 2, 4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4, 3, 7, 6, 3, 6, 2,
            0, 4, 7, 0, 7, 3, 1, 2, 6, 1, 6, 5
        };
        foreach (int liIndex in lOFaces)
            pOTriangles.Add(li + liIndex);
    }

    private void LateUpdate()
    {
        IsModelVisible = false;
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        Interaction lOInteraction = UWScene.Interaction;
        UWInventory lOInventory = UWScene.Inventory;
        bool lbEligible = UWVRMeleeMotion.IsEnabled && lOCamera != null
            && lOCamera.isActiveAndEnabled && lOInteraction != null
            && lOInteraction.IsCombatModeActive && lOInventory != null
            && !(UWScene.ControlScheme != null
                && UWScene.ControlScheme.Current == UWControlScheme.SchemeEnum.Original)
            && !(UWScene.ControlScheme != null && UWScene.ControlScheme.IsWorldInputBlocked)
            && UWVRSpatialInventory.VisiblePanel == null
            && !(UWModernBags.Instance != null && UWModernBags.Instance.IsOpen);
        if (!lbEligible)
        {
            SetVisible(false);
            return;
        }

        UWCharacter lOCharacter = UWScene.Player != null
            ? UWScene.Player.GetComponent<UWCharacter>() : null;
        if (lOCharacter != null && lOCharacter.HasRangedWeapon)
        {
            SetVisible(false);
            return;
        }

        UWVRHeadTracking lOTracking = lOCamera.GetComponent<UWVRHeadTracking>();
        if (lOTracking == null || !lOTracking.TryGetControllerPose(XRNode.RightHand,
                out Vector3 lOHandPosition, out Quaternion lOHandRotation))
        {
            SetVisible(false);
            return;
        }
        Ray lORay = new Ray(lOHandPosition, lOHandRotation * Vector3.forward);
        if (!UWVRAim.HasValidDirection(lORay.direction))
        {
            SetVisible(false);
            return;
        }

        if (!EnsureVisual())
            return;

        if (!mRoot.gameObject.activeSelf)
            mRoot.gameObject.SetActive(true);

        float lfUnits = UWVRBootstrap.WorldUnitsPerMeter;
        mRoot.position = GripOrigin(lORay, lfUnits);
        mRoot.rotation = lOHandRotation;
        mRoot.localScale = Vector3.one * lfUnits;

        UWObject lOWeapon = lOInventory.GetEquipped(lOInventory.MainHandSlot);
        UWVRWeaponShape.Kind leForm = lOWeapon == null || lOCharacter == null
            ? UWVRWeaponShape.Kind.Fist
            : UWVRWeaponShape.Classify(lOCharacter.CurrentWeaponType);
        bool lbFist = leForm == UWVRWeaponShape.Kind.Fist;
        if (meShownForm != leForm)
        {
            meShownForm = leForm;
            mBladeFilter.sharedMesh = leForm == UWVRWeaponShape.Kind.Axe
                ? mAxeMesh : leForm == UWVRWeaponShape.Kind.Mace ? mMaceMesh : mBladeMesh;
            Debug.Log("[UWVR-BLADE] displayed family=" + leForm);
        }
        mBlade.enabled = !lbFist;
        mGuard.enabled = leForm == UWVRWeaponShape.Kind.Sword;
        mGrip.enabled = leForm == UWVRWeaponShape.Kind.Sword;
        mFist.enabled = lbFist;
        IsModelVisible = true;
        if (!mbReported)
        {
            Debug.Log("[UWVR-MELEE] Right Touch weapon model active (generic mesh, no collision). ");
            mbReported = true;
        }
    }

    private bool EnsureVisual()
    {
        if (mRoot != null)
            return true;

        Shader lOShader = Shader.Find("Universal Render Pipeline/Lit");
        if (lOShader == null) lOShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lOShader == null) lOShader = Shader.Find("Standard");
        if (lOShader == null)
        {
            Debug.LogWarning("[UWVR-MELEE] No weapon mesh shader; keeping original 2D weapon.");
            enabled = false;
            return false;
        }

        mSteelMaterial = MakeMaterial(lOShader, new Color(0.65f, 0.75f, 0.80f));
        mGuardMaterial = MakeMaterial(lOShader, new Color(0.70f, 0.54f, 0.25f));
        mGripMaterial = MakeMaterial(lOShader, new Color(0.32f, 0.17f, 0.09f));
        mBladeMesh = CreateBladeMesh();
        mAxeMesh = CreateAxeMesh();
        mMaceMesh = CreateMaceMesh();
        mCubeMesh = CreateCubeMesh();
        mRoot = new GameObject("UW VR Tracked Main Hand Weapon").transform;
        mRoot.SetParent(transform, false);

        mBlade = MakePart("Metal blade", mBladeMesh, mSteelMaterial,
            Vector3.zero, Vector3.one);
        mBladeFilter = mBlade.GetComponent<MeshFilter>();
        mGuard = MakePart("Crossguard", mCubeMesh, mGuardMaterial,
            new Vector3(0f, 0f, 0.13f), new Vector3(0.24f, 0.032f, 0.044f));
        mGrip = MakePart("Hilt", mCubeMesh, mGripMaterial,
            new Vector3(0f, 0f, 0.02f), new Vector3(0.031f, 0.035f, 0.20f));
        mFist = MakePart("Unarmed hand placeholder", mCubeMesh, mGripMaterial,
            new Vector3(0f, 0f, 0.07f), new Vector3(0.12f, 0.07f, 0.15f));
        return true;
    }

    private static Material MakeMaterial(Shader pOShader, Color pOColor)
    {
        Material lOMaterial = new Material(pOShader);
        if (lOMaterial.HasProperty("_BaseColor")) lOMaterial.SetColor("_BaseColor", pOColor);
        if (lOMaterial.HasProperty("_Color")) lOMaterial.SetColor("_Color", pOColor);
        return lOMaterial;
    }

    private MeshRenderer MakePart(string psName, Mesh pOMesh, Material pOMaterial,
        Vector3 pOLocation, Vector3 pOScale)
    {
        GameObject lOPart = new GameObject(psName, typeof(MeshFilter), typeof(MeshRenderer));
        lOPart.transform.SetParent(mRoot, false);
        lOPart.transform.localPosition = pOLocation;
        lOPart.transform.localScale = pOScale;
        lOPart.GetComponent<MeshFilter>().sharedMesh = pOMesh;
        MeshRenderer lORenderer = lOPart.GetComponent<MeshRenderer>();
        lORenderer.sharedMaterial = pOMaterial;
        lORenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lORenderer.receiveShadows = false;
        return lORenderer;
    }

    private void SetVisible(bool pbVisible)
    {
        IsModelVisible = false;
        if (mRoot != null && mRoot.gameObject.activeSelf)
            mRoot.gameObject.SetActive(false);
        mbReported = false;
    }

    private void OnDisable()
    {
        SetVisible(false);
    }

    private void OnDestroy()
    {
        SetVisible(false);
        if (mBladeMesh != null) Destroy(mBladeMesh);
        if (mAxeMesh != null) Destroy(mAxeMesh);
        if (mMaceMesh != null) Destroy(mMaceMesh);
        if (mCubeMesh != null) Destroy(mCubeMesh);
        if (mSteelMaterial != null) Destroy(mSteelMaterial);
        if (mGuardMaterial != null) Destroy(mGuardMaterial);
        if (mGripMaterial != null) Destroy(mGripMaterial);
    }
}
