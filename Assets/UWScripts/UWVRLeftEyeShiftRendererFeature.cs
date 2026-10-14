using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP 17 render-graph-compatible final full-screen pass, guarded so no extra
/// blits happen in desktop play, ordinary VR, or mirror/preview cameras.
/// The shader gets the XR eye index and changes ONLY left-eye UV sampling.
/// Do NOT offset Camera stereo projection matrices or headset tracking here.
/// </summary>
public sealed class UWVRLeftEyeShiftRendererFeature : FullScreenPassRendererFeature
{
    private bool mbReportedXRPass;

    public override void AddRenderPasses(ScriptableRenderer pORenderer,
        ref RenderingData pORenderingData)
    {
        if (!UWVRLeftEyeShift.IsEnabled
            || !pORenderingData.cameraData.xr.enabled
            || pORenderingData.cameraData.camera != UWVRBootstrap.CurrentVRCamera)
            return;

        // In OpenXR MultiPass Unity calls us once for each eye. Submit NO
        // blit to eye 1: this guarantees the right-eye image is not touched,
        // even on hardware whose shader per-eye index behaves differently.
        if (!pORenderingData.cameraData.xr.singlePassEnabled
            && pORenderingData.cameraData.xr.multipassId != 0)
            return;

        int liWidth = pORenderingData.cameraData.cameraTargetDescriptor.width;
        if (liWidth <= 0)
            return; // Never sample several image-widths outside the texture.

        UWVRLeftEyeShift.SetRenderEyeWidth(liWidth);
        Shader.SetGlobalFloat("_UWLeftEyeShiftUv", UWVRLeftEyeShift.NormalizedOffset(
            UWVRLeftEyeShift.ShiftPixels, liWidth));

        if (!mbReportedXRPass)
        {
            mbReportedXRPass = true;
            Debug.Log("[UWVR-EYE-SHIFT] XR final shader pass ACTIVE on camera="
                + pORenderingData.cameraData.camera.name
                + "; stereo mode=" + (pORenderingData.cameraData.xr.singlePassEnabled
                    ? "single pass" : "multi pass, RIGHT pass skipped")
                + "; eye width=" + liWidth
                + "; left index 0 UV shifted, right index 1 UV unchanged.");
        }

        base.AddRenderPasses(pORenderer, ref pORenderingData);
    }
}
