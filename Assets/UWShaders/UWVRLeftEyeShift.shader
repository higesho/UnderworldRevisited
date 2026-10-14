// Diagnostic ONLY: re-sample the finished LEFT XR eye a few pixels left/right.
// The right XR eye samples the source without any UV correction.
// Uses the OpenXR/URP per-eye index, not the monitor mirror's two halves.
Shader "Hidden/UW/VRLeftEyeShift"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "LeftEyeDiagnosticShift"
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            SAMPLER(sampler_BlitTexture);
            float _UWLeftEyeShiftUv;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                bool leftEye = (unity_StereoEyeIndex == 0);
                float2 originalUV = input.texcoord;
                float2 sampleUV = originalUV;
                // Positive offset moves the CONTENT of the left-eye image
                // towards screen-right. Do not alter the right-eye UV at all.
                if (leftEye)
                    sampleUV.x -= _UWLeftEyeShiftUv;

                half4 color = (sampleUV.x >= 0.0 && sampleUV.x <= 1.0)
                    ? SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_BlitTexture, sampleUV)
                    : half4(0.0, 0.0, 0.0, 1.0);

                // Small top-left cyan mark identifies the corrected left-eye
                // view. The right eye must retain its ORIGINAL exact pixels.
                if (originalUV.x > 0.016 && originalUV.x < 0.040
                    && originalUV.y > 0.94 && originalUV.y < 0.949 && leftEye)
                    return half4(0.0, 0.92, 1.0, 1.0);

                return color;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
