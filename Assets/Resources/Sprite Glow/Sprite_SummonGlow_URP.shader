Shader "VFX/Sprite_SummonGlow_URP_Fixed"
{
    Properties
    {
        // SPRITE
        [MainTexture] _MainTex ("Sprite", 2D) = "white" {}
        [MainColor]   _Color   ("Tint", Color) = (1,1,1,1)

        // GROUND GLOW
        [HDR]_GroundGlowColor ("Ground Glow Color", Color) = (2,0.6,0.7,1)
        _GroundGlowStrength   ("Ground Glow Strength", Range(0,10)) = 2
        _GroundSoftness       ("Ground Edge Softness", Range(0,1)) = 0.0
        _AlphaCut             ("Alpha Cut (discard below)", Range(0,1)) = 0.02

        // UPRIGHT GLOW
        [HDR]_UpGlowColor   ("Up Glow Color", Color) = (2,0.6,0.9,1)
        _UpGlowStrength     ("Up Glow Strength", Range(0,20)) = 6
        _UpGlowHeight       ("Up Glow Height (world)", Range(0,20)) = 4
        _UpWidth            ("Up Glow Width (world)", Range(0,20)) = 3
        _UpHeightFalloff    ("Up Height Falloff", Range(0.1,8)) = 2.5
        _UpSideFalloff      ("Up Side Falloff",   Range(0.1,8)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        ZWrite Off
        Cull Off

        // ---------- PASS 1 : Ground sprite with emissive ----------
        Pass
        {
            Name "GroundSprite"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZTest LEqual
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _GroundGlowColor;
                float  _GroundGlowStrength;
                float  _GroundSoftness;
                float  _AlphaCut;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata IN)
            {
                v2f o;
                float3 worldPos = TransformObjectToWorld(IN.vertex.xyz);
                o.pos = TransformWorldToHClip(worldPos);
                o.uv  = TRANSFORM_TEX(IN.uv, _MainTex);
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                float a = tex.a;
                if (_GroundSoftness > 0.001)
                {
                    // softening along its own alpha
                    a = smoothstep(_AlphaCut, saturate(_AlphaCut + _GroundSoftness), a);
                }
                clip(a - _AlphaCut);

                float3 baseRGB = tex.rgb * _Color.rgb;
                float3 emissive = _GroundGlowColor.rgb * a * _GroundGlowStrength;

                return float4(baseRGB + emissive, a * _Color.a);
            }
            ENDHLSL
        }

        // ---------- PASS 2 : Upright additive glow (masked by sprite alpha) ----------
        Pass
        {
            Name "UpGlow"
            Tags { "LightMode"="UniversalForward" }
            Blend One One
            ZTest LEqual
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _UpGlowColor;
                float  _UpGlowStrength;
                float  _UpGlowHeight;
                float  _UpWidth;
                float  _UpHeightFalloff;
                float  _UpSideFalloff;
                float  _AlphaCut;
            CBUFFER_END

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION; // used to get object origin
                float2 uv     : TEXCOORD0; // 0..1
            };

            struct v2f
            {
                float4 pos   : SV_POSITION;
                float2 uv    : TEXCOORD0; // sprite alpha mask
                float  hT    : TEXCOORD1; // height 0..1
                float  sideT : TEXCOORD2; // side 0..1
            };

            v2f vert(appdata IN)
            {
                v2f o;

                // Object/world info
                float3 worldOrigin = mul(unity_ObjectToWorld, float4(0,0,0,1)).xyz;

                // Build a billboard around WORLD UP that faces camera.
                float3 worldUp = float3(0,1,0);
                float3 toCam   = normalize(_WorldSpaceCameraPos.xyz - worldOrigin);

                float3 right = normalize(cross(worldUp, toCam));
                // robust fallback if camera is almost vertical → right ~ (0,0,0)
                if (dot(right, right) < 1e-6)
                    right = float3(1,0,0);

                float u = IN.uv.x - 0.5;   // -0.5..0.5
                float v = IN.uv.y;         // 0..1 up the column

                float3 worldPos = worldOrigin
                                 + right    * (u * _UpWidth)
                                 + worldUp  * (v * _UpGlowHeight);

                o.pos   = TransformWorldToHClip(worldPos);
                o.uv    = TRANSFORM_TEX(IN.uv, _MainTex);
                o.hT    = v;
                o.sideT = saturate(abs(u) * 2.0); // 0 center → 1 edge
                return o;
            }

            half4 frag(v2f IN) : SV_Target
            {
                float4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float a = tex.a;
                clip(a - _AlphaCut);

                float heightFade = pow(saturate(1.0 - IN.hT), _UpHeightFalloff);
                float sideFade   = pow(saturate(1.0 - IN.sideT), _UpSideFalloff);

                float glow = a * heightFade * sideFade * _UpGlowStrength;
                float3 rgb = _UpGlowColor.rgb * glow;

                return float4(rgb, glow); // additive
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
