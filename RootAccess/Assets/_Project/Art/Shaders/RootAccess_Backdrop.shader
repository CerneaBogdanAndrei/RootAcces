// Opaque full-screen backdrop for the 2D renderer: shows the sky camera's render texture (real skybox material)
// behind every parallax layer. The 2D renderer cannot clear to a skybox itself.
Shader "RootAccess/Backdrop"
{
    Properties
    {
        _MainTex("Texture", 2D) = "black" {}
        _Tint("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS);
                o.uv = input.uv;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _Tint;
                return half4(color.rgb, 1.0);
            }
            ENDHLSL
        }
    }
}
