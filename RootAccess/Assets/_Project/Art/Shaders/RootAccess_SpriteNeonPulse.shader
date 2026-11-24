// Unlit HDR emissive neon: the sprite alpha is the tube mask, brightness pulses over time (picked up by Bloom).
Shader "RootAccess/Sprite Neon Pulse"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        [HDR] _EmissionColor("Emission", Color) = (3, 0.4, 2.4, 1)
        _PulseSpeed("Pulse Speed", Float) = 2
        _PulseMin("Pulse Minimum", Range(0, 1)) = 0.55
        _PhaseByX("Phase by World X", Float) = 0.5
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
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
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EmissionColor;
                float _PulseSpeed;
                float _PulseMin;
                float _PhaseByX;
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
                float phase : TEXCOORD1;
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
                float3 world = TransformObjectToWorld(position);
                o.positionCS = TransformWorldToHClip(world);
                o.uv = input.uv;
                o.phase = world.x * _PhaseByX;
                o.color = input.color * _Color * unity_SpriteColor;
                return o;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float pulse = lerp(_PulseMin, 1.0, 0.5 + 0.5 * sin(_Time.y * _PulseSpeed + input.phase));
                half3 rgb = _EmissionColor.rgb * input.color.rgb * tex.rgb * pulse;
                return half4(rgb, tex.a * input.color.a);
            }
            ENDHLSL
        }
    }
}
