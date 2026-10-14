using UnityEngine;
using UnityEngine.XR;
using UWDataImport.UWData;

/// <summary>
/// Lightweight, collider-free prototype bow/string/arrow silhouette. The bow
/// is held in the right tracked hand, aimed from that Touch, with the string
/// nock following the left Touch while the original right-trigger charge is
/// active. The render is independent of the authoritative gameplay draw.
/// </summary>
[DefaultExecutionOrder(930)]
public sealed class UWVRBowVisual : MonoBehaviour
{
    private const int BowObjectId = 0x19;
    private const float BowHalfHeightMetres = 0.44f;
    private const float BowForwardMetres = 0.18f;
    private const float BowBendMetres = 0.11f;
    private const int BowSegments = 12;

    private LineRenderer mBow;
    private LineRenderer mString;
    private LineRenderer mArrow;
    private Material mWood;
    private Material mStringMaterial;
    private Material mArrowMaterial;
    private bool mbReported;

    /// <summary>Only hide the original 2D bow sprite while the physical
    /// tracked model is actually visible. Tracking loss restores the sprite.</summary>
    public static bool IsBowVisible { get; private set; }

    /// <summary>Symmetric curved stave around the right grip. Pure geometry,
    /// valid in a no-headset Unity Editor test at either XR world scale.</summary>
    public static Vector3 StavePoint(Ray pORight, Vector3 pOUp,
        float pfAlong, float pfUnits)
    {
        float lfT = Mathf.Clamp(pfAlong, -1f, 1f);
        float lfScale = Mathf.Max(0.001f, pfUnits);
        return pORight.origin + pORight.direction.normalized
            * (BowForwardMetres + BowBendMetres * (1f - lfT * lfT)) * lfScale
            + pOUp.normalized * (BowHalfHeightMetres * lfT * lfScale);
    }

    public static Vector3 StringNock(Ray pORight, Vector3 pOLeft,
        bool pbNocked, float pfUnits)
    {
        return pbNocked ? pOLeft : pORight.origin
            + pORight.direction.normalized * (0.045f * Mathf.Max(0.001f, pfUnits));
    }

    private void LateUpdate()
    {
        IsBowVisible = false;
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        Interaction lOCombat = UWScene.Interaction;
        UWInventory lOInventory = UWScene.Inventory;
        bool lbEligible = UWVRTwoHandBowDraw.IsEnabled && lOCamera != null
            && lOCamera.isActiveAndEnabled && lOCombat != null
            && lOCombat.IsCombatModeActive && lOInventory != null
            && (UWScene.ControlScheme == null
                || (UWScene.ControlScheme.Current != UWControlScheme.SchemeEnum.Original
                    && !UWScene.ControlScheme.IsWorldInputBlocked))
            && UWVRSpatialInventory.VisiblePanel == null
            && !(UWModernBags.Instance != null && UWModernBags.Instance.IsOpen);
        if (!lbEligible)
        {
            SetVisible(false);
            return;
        }

        UWObject lOWeapon = lOInventory.GetEquipped(lOInventory.MainHandSlot);
        if (lOWeapon == null || lOWeapon.ID != BowObjectId)
        {
            SetVisible(false);
            return;
        }

        UWVRHeadTracking lOTracking = lOCamera.GetComponent<UWVRHeadTracking>();
        if (lOTracking == null || !lOTracking.TryGetControllerPose(XRNode.RightHand,
                out Vector3 lORight, out Quaternion lORotation))
        {
            SetVisible(false);
            return;
        }

        if (!EnsureRenderers())
            return;

        Ray lOBow = new Ray(lORight, lORotation * Vector3.forward);
        Vector3 lOUp = lORotation * Vector3.up;
        float lfUnits = UWVRBootstrap.WorldUnitsPerMeter;
        for (int li = 0; li <= BowSegments; li++)
            mBow.SetPosition(li, StavePoint(lOBow, lOUp,
                (li * 2f / BowSegments) - 1f, lfUnits));

        bool lbLeft = lOTracking.TryGetControllerRay(XRNode.LeftHand,
            out Ray lOLeft);
        bool lbNocked = lbLeft && lOCombat.IsAttacking
            && lOCombat.VRTwoHandBowNocked;
        Vector3 lONock = StringNock(lOBow, lOLeft.origin, lbNocked, lfUnits);
        mString.SetPosition(0, StavePoint(lOBow, lOUp, -1f, lfUnits));
        mString.SetPosition(1, lONock);
        mString.SetPosition(2, StavePoint(lOBow, lOUp, 1f, lfUnits));

        mBow.enabled = true;
        mString.enabled = true;
        IsBowVisible = true;
        mArrow.enabled = lbNocked;
        if (lbNocked)
        {
            mArrow.SetPosition(0, lONock);
            mArrow.SetPosition(1, lOBow.origin + lOBow.direction.normalized
                * (UWVRHandLaunch.RightMuzzleOffsetMetres * lfUnits));
        }

        mBow.startWidth = 0.019f * lfUnits;
        mBow.endWidth = 0.019f * lfUnits;
        mString.startWidth = 0.003f * lfUnits;
        mString.endWidth = 0.003f * lfUnits;
        mArrow.startWidth = 0.007f * lfUnits;
        mArrow.endWidth = 0.007f * lfUnits;
        if (!mbReported)
        {
            mbReported = true;
            Debug.Log("[UWVR-BOW] Visual bow and string tracking active; no physical string collision.");
        }
    }

    private bool EnsureRenderers()
    {
        if (mBow != null && mString != null && mArrow != null)
            return true;

        Shader lOShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lOShader == null) lOShader = Shader.Find("Sprites/Default");
        if (lOShader == null) lOShader = Shader.Find("Standard");
        if (lOShader == null)
        {
            Debug.LogWarning("[UWVR-BOW] Missing shader; keeping existing combat display.");
            enabled = false;
            return false;
        }

        mWood = MakeMaterial(lOShader, new Color(0.66f, 0.39f, 0.19f));
        mStringMaterial = MakeMaterial(lOShader, new Color(0.92f, 0.84f, 0.66f));
        mArrowMaterial = MakeMaterial(lOShader, new Color(0.95f, 0.80f, 0.35f));
        mBow = MakeLine("UW VR right-hand curved bow stave", BowSegments + 1, mWood);
        mString = MakeLine("UW VR left-hand drawn string", 3, mStringMaterial);
        mArrow = MakeLine("UW VR nocked arrow preview", 2, mArrowMaterial);
        return true;
    }

    private static Material MakeMaterial(Shader pOShader, Color pOColor)
    {
        Material lOMaterial = new Material(pOShader);
        if (lOMaterial.HasProperty("_BaseColor")) lOMaterial.SetColor("_BaseColor", pOColor);
        if (lOMaterial.HasProperty("_Color")) lOMaterial.SetColor("_Color", pOColor);
        return lOMaterial;
    }

    private LineRenderer MakeLine(string psName, int piPositions, Material pOMaterial)
    {
        GameObject lOObject = new GameObject(psName, typeof(LineRenderer));
        lOObject.transform.SetParent(transform, false);
        LineRenderer lOLine = lOObject.GetComponent<LineRenderer>();
        lOLine.positionCount = piPositions;
        lOLine.sharedMaterial = pOMaterial;
        lOLine.useWorldSpace = true;
        lOLine.numCapVertices = 4;
        lOLine.numCornerVertices = 4;
        lOLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lOLine.receiveShadows = false;
        lOLine.enabled = false;
        return lOLine;
    }

    private void SetVisible(bool pbVisible)
    {
        IsBowVisible = pbVisible;
        if (mBow != null) mBow.enabled = pbVisible;
        if (mString != null) mString.enabled = pbVisible;
        if (mArrow != null) mArrow.enabled = pbVisible;
        if (!pbVisible) mbReported = false;
    }

    private void OnDestroy()
    {
        SetVisible(false);
        if (mWood != null) Destroy(mWood);
        if (mStringMaterial != null) Destroy(mStringMaterial);
        if (mArrowMaterial != null) Destroy(mArrowMaterial);
    }
}
