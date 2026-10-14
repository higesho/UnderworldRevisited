using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>Idempotent, opt-in URP renderer-feature wiring for the eye shift
/// experiment. Shader/Material/feature are included in the Windows build even
/// when -vr-left-eye-shift is NOT passed; the feature enqueues no pass then.</summary>
public static class UWVRLeftEyeShiftSetup
{
    public const string RendererAssetPath = "Assets/Settings/UWUniversalRenderer.asset";
    public const string MaterialAssetPath = "Assets/Settings/UWVRLeftEyeShift.mat";
    public const string ShaderName = "Hidden/UW/VRLeftEyeShift";

    [MenuItem("Underworld/VR/Install Left Eye Shift Diagnostic")]
    public static void ConfigureRenderer()
    {
        UniversalRendererData lORenderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(
            RendererAssetPath);
        if (lORenderer == null)
            throw new InvalidOperationException("[UWVR-EYE-SHIFT] The project's URP Renderer Data was not found.");

        Shader lOShader = Shader.Find(ShaderName);
        if (lOShader == null || !lOShader.isSupported)
            throw new InvalidOperationException("[UWVR-EYE-SHIFT] Stereo blit shader missing or unsupported.");

        Material lOMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialAssetPath);
        if (lOMaterial == null)
        {
            lOMaterial = new Material(lOShader)
            {
                name = "UW VR Left-Eye Shift (Diagnostic Only)"
            };
            AssetDatabase.CreateAsset(lOMaterial, MaterialAssetPath);
        }

        UWVRLeftEyeShiftRendererFeature lOFeature = lORenderer.rendererFeatures
            .OfType<UWVRLeftEyeShiftRendererFeature>().FirstOrDefault();
        bool lbAdded = false;
        if (lOFeature == null)
        {
            lOFeature = ScriptableObject.CreateInstance<UWVRLeftEyeShiftRendererFeature>();
            lOFeature.name = "UW VR Left-Eye Shift (opt-in only)";
            lOFeature.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(lOFeature, lORenderer);

            // Match Unity's serialized feature-map bookkeeping so the built
            // player does not lose the feature after domain or scene reloads.
            lORenderer.rendererFeatures.Add(lOFeature);
            SerializedObject lOSerialized = new SerializedObject(lORenderer);
            SerializedProperty lOMap = lOSerialized.FindProperty("m_RendererFeatureMap");
            if (lOMap != null)
            {
                lOMap.InsertArrayElementAtIndex(lOMap.arraySize);
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(lOFeature,
                        out string _, out long liFileId))
                    throw new InvalidOperationException("[UWVR-EYE-SHIFT] Cannot store renderer feature ID.");
                lOMap.GetArrayElementAtIndex(lOMap.arraySize - 1).longValue = liFileId;
                lOSerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            lbAdded = true;
        }

        lOFeature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        lOFeature.fetchColorBuffer = true;
        lOFeature.requirements = ScriptableRenderPassInput.None;
        lOFeature.bindDepthStencilAttachment = false;
        lOFeature.passMaterial = lOMaterial;
        lOFeature.passIndex = 0;
        lOFeature.SetActive(true);
        lOFeature.Create();

        EditorUtility.SetDirty(lOFeature);
        EditorUtility.SetDirty(lORenderer);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(RendererAssetPath);
        Debug.Log("[UWVR-EYE-SHIFT] URP renderer installed stereo blit feature="
            + (lbAdded ? "new" : "existing")
            + "; executes ONLY with -vr-left-eye-shift; right eye untouched.");
    }
}
