using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Visual direction guides for the opt-in -vr-hand-aim prototype. Orange is
/// right-hand combat/ranged, cyan is left-hand projectile/target magic.
/// These are NOT physical swords or exact projectile collision previews:
/// the game still launches missiles from its historical player origin.
/// No colliders or rigidbodies are created; the normal HUD is unchanged.
/// </summary>
[DefaultExecutionOrder(850)]
public sealed class UWVRHandAimGuides : MonoBehaviour
{
    private const float GuideLengthMetres = 1.8f;
    private const float GuideWidthMetres = 0.0035f;
    private LineRenderer mRight;
    private LineRenderer mLeft;
    private Material mRightMaterial;
    private Material mLeftMaterial;
    private bool mbReportedTracking;
    private bool mbRightTracked;
    private bool mbLeftTracked;

    /// <summary>Pure metre-to-world conversion for synthetic offline tests.</summary>
    public static Vector3 GuideEnd(Ray pOHand, float pfUnitsPerMetre)
    {
        return pOHand.origin + pOHand.direction.normalized
            * (GuideLengthMetres * Mathf.Max(0.001f, pfUnitsPerMetre));
    }

    private void LateUpdate()
    {
        Camera lOCamera = UWVRBootstrap.CurrentVRCamera;
        bool lbGame = UWVRAim.IsHandAimActive && lOCamera != null
            && lOCamera.isActiveAndEnabled && UWScene.Player != null
            && lOCamera.gameObject.name != "UW VR Menu Camera";
        if (!lbGame || UWVRSpatialInventory.VisiblePanel != null
            || (UWModernBags.Instance != null && UWModernBags.Instance.IsOpen))
        {
            SetVisible(false);
            return;
        }

        if (!EnsureGuides())
            return;

        UWVRHeadTracking lOTracking = lOCamera.GetComponent<UWVRHeadTracking>();
        float lfUnits = lOCamera.transform.parent != null
            ? Mathf.Max(0.001f, Mathf.Abs(lOCamera.transform.parent.lossyScale.x)) : 1f;

        // In the gesture-melee prototype, the 3D weapon takes the place of
        // the right-hand laser. The left magic laser remains available.
        bool lbRightTracked = !UWVRMeleeMotion.IsEnabled
            && UpdateGuide(mRight, lOTracking, XRNode.RightHand, lfUnits);
        if (UWVRMeleeMotion.IsEnabled && mRight != null)
            mRight.enabled = false;
        bool lbLeftTracked = UpdateGuide(mLeft, lOTracking, XRNode.LeftHand, lfUnits);
        if (!mbReportedTracking || lbRightTracked != mbRightTracked || lbLeftTracked != mbLeftTracked)
        {
            mbReportedTracking = true;
            mbRightTracked = lbRightTracked;
            mbLeftTracked = lbLeftTracked;
            Debug.Log("[UWVR-AIM] Right Touch tracking=" + lbRightTracked
                + "; left Touch tracking=" + lbLeftTracked
                + "; untracked hands use the headset's gaze ray.");
        }
    }

    private static bool UpdateGuide(LineRenderer pOLine,
        UWVRHeadTracking pOTracking, XRNode peHand, float pfUnits)
    {
        if (pOLine == null)
            return false;

        if (pOTracking == null || !pOTracking.TryGetControllerRay(peHand, out Ray lOHand)
            || !UWVRAim.HasValidDirection(lOHand.direction))
        {
            pOLine.enabled = false;
            return false;
        }

        pOLine.enabled = true;
        pOLine.startWidth = GuideWidthMetres * pfUnits;
        pOLine.endWidth = GuideWidthMetres * pfUnits * 0.6f;
        pOLine.SetPosition(0, lOHand.origin);
        pOLine.SetPosition(1, GuideEnd(lOHand, pfUnits));
        return true;
    }

    private bool EnsureGuides()
    {
        if (mRight != null && mLeft != null)
            return true;

        Shader lOShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (lOShader == null)
            lOShader = Shader.Find("Sprites/Default");
        if (lOShader == null)
        {
            enabled = false;
            Debug.LogWarning("[UWVR] Could not find a shader for hand aim guide lines.");
            return false;
        }

        mRightMaterial = MakeMaterial(lOShader, new Color(1f, 0.64f, 0.12f, 0.75f));
        mLeftMaterial = MakeMaterial(lOShader, new Color(0.18f, 0.85f, 1f, 0.75f));
        mRight = MakeGuide("UW VR Right Combat Aim", mRightMaterial);
        mLeft = MakeGuide("UW VR Left Magic Aim", mLeftMaterial);
        return true;
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

        if (lOMaterial.HasProperty("_BaseColor"))
            lOMaterial.SetColor("_BaseColor", pOColor);
        if (lOMaterial.HasProperty("_Color"))
            lOMaterial.SetColor("_Color", pOColor);
        return lOMaterial;
    }

    private LineRenderer MakeGuide(string psName, Material pOMaterial)
    {
        GameObject lOObject = new GameObject(psName, typeof(LineRenderer));
        lOObject.transform.SetParent(transform, false);
        LineRenderer lOLine = lOObject.GetComponent<LineRenderer>();
        lOLine.sharedMaterial = pOMaterial;
        lOLine.positionCount = 2;
        lOLine.numCapVertices = 4;
        lOLine.useWorldSpace = true;
        lOLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lOLine.receiveShadows = false;
        lOLine.enabled = false;
        return lOLine;
    }

    private void SetVisible(bool pbVisible)
    {
        if (mRight != null)
            mRight.enabled = pbVisible;
        if (mLeft != null)
            mLeft.enabled = pbVisible;
        if (!pbVisible)
            mbReportedTracking = false;
    }

    private void OnDestroy()
    {
        if (mRightMaterial != null)
            Destroy(mRightMaterial);
        if (mLeftMaterial != null)
            Destroy(mLeftMaterial);
    }
}
