// Unlit water body for WaterBody2D meshes: vertex-colour gradient, animated caustic shimmer and a bright crest line.
Shader "RootAccess/Water Surface"
{
    Properties
    {
        _ShimmerStrength("Shimmer Strength", Range(0, 1)) = 0.18
        _ShimmerScale("Shimmer Scale", Float) = 1.4
        _ShimmerSpeed("Shimmer Speed", Float) = 0.6
        [HDR] _CrestColor("Crest Color", Color) = (0.8, 1.6, 2, 1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_Project/Art/Shaders/RootAccessCommon.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag

            CBUFFER_START(UnityPerMaterial)
                float _ShimmerStrength;
                float _ShimmerScale;
                float _ShimmerSpeed;
                half4 _CrestColor;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(input.positionOS);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.color = input.color;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y * _ShimmerSpeed;
                float2 p = input.positionWS.xy * _ShimmerScale;
                float caustic = RA_ValueNoise(p + float2(t, t * 0.6)) * RA_ValueNoise(p * 1.7 - float2(t * 0.8, t));
                float surface = saturate((input.color.a - 0.78) * 12.0);
                half3 rgb = input.color.rgb + caustic * _ShimmerStrength + _CrestColor.rgb * surface * 0.35;
                return half4(rgb, input.color.a);
            }
            ENDHLSL
        }
    }
}
