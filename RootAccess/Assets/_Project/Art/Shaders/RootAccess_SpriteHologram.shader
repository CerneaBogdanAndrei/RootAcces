// Unlit additive hologram: tinted sprite with moving scanlines, flicker and horizontal glitch bands.
Shader "RootAccess/Sprite Hologram"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _Tint("Tint", Color) = (0.3, 1.6, 2.2, 1)
        _ScanlineDensity("Scanline Density", Float) = 28
        _ScanSpeed("Scan Speed", Float) = 1.5
        _FlickerSpeed("Flicker Speed", Float) = 9
        _GlitchStrength("Glitch Strength", Range(0, 0.1)) = 0.02
        _Opacity("Opacity", Range(0, 1)) = 0.85
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha One
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Assets/_Project/Art/Shaders/RootAccessCommon.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _Tint;
                float _ScanlineDensity;
                float _ScanSpeed;
                float _FlickerSpeed;
                float _GlitchStrength;
                float _Opacity;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                SetUpSpriteInstanceProperties();
                float3 position = UnityFlipSprite(input.positionOS, unity_SpriteProps.xy);
                o.positionWS = TransformObjectToWorld(position);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = input.uv;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float t = _Time.y;
                float band = step(0.93, RA_ValueNoise(float2(input.positionWS.y * 6.0, floor(t * 8.0))));
                float2 uv = input.uv + float2(band * _GlitchStrength * (RA_Hash21(float2(t, input.positionWS.y)) - 0.5) * 4.0, 0.0);
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

                float scan = 0.55 + 0.45 * step(0.5, frac(input.positionWS.y * _ScanlineDensity - t * _ScanSpeed));
                float flicker = 0.85 + 0.15 * sin(t * _FlickerSpeed + RA_ValueNoise(float2(t * 3.0, 0.0)) * 6.2831);
                half3 rgb = tex.rgb * _Tint.rgb * input.color.rgb * scan * flicker;
                half alpha = tex.a * input.color.a * _Opacity;
                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
