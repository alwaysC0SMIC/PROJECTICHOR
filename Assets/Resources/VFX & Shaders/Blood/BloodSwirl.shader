Shader "URP/BloodSwirl"
{
    Properties
    {
        _MainColor("Blood Color", Color) = (1, 0.1, 0.1, 1)
        _GlowIntensity("Glow Intensity", Range(0, 10)) = 3.0
        _SwirlStrength("Swirl Strength", Range(0, 10)) = 4.0
        _PulseSpeed("Pulse Speed", Range(0, 10)) = 2.0
        _NoiseScale("Noise Scale", Range(1, 20)) = 8.0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Transparent" }

        Pass
        {
            Name "BloodSwirl"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float _GlowIntensity;
                float _SwirlStrength;
                float _PulseSpeed;
                float _NoiseScale;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float swirlNoise(float2 uv, float t)
            {
                float2 center = uv - 0.5;
                float r = length(center);
                float a = atan2(center.y, center.x);
                a += t * 0.5 + sin(r * 10.0 + t * 0.5) * _SwirlStrength;

                float2 swirlUV = float2(cos(a), sin(a)) * r + 0.5;
                return noise(swirlUV * _NoiseScale);
            }

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y;
                float n = swirlNoise(i.uv, t);

                // Radial distance from center
                float2 center = i.uv - 0.5;
                float r = length(center);

                // Emissive pulse
                float pulse = 0.5 + 0.5 * sin(t * _PulseSpeed + r * 12.0);

                // Outer glow fades, inner intensifies
                float glow = saturate(1.0 - r) * n * pulse;

                float3 color = _MainColor.rgb * glow * _GlowIntensity;
                return float4(color, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
