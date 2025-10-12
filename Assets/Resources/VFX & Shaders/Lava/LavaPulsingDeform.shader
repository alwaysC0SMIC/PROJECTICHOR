Shader "URP/LavaProcedural"
{
    Properties
    {
        _Scale("UV Scale", Range(0.1,10)) = 2.0
        _FlowDir("Flow Direction (xy) & Speed (z)", Vector) = (1,0,0.15,0)
        _CellJitter("Cell Jitter", Range(0,1)) = 0.65
        _CrackWidth("Crack Width", Range(0.1,3)) = 1.35
        _CrackContrast("Crack Contrast", Range(1,8)) = 3.0

        _RockTint("Rock Tint", Color) = (0.15,0.12,0.12,1)
        _LavaTint("Lava Tint", Color) = (1.0,0.45,0.1,1)
        _EdgeGlow("Edge Glow Boost", Range(0,3)) = 1.2

        _EmissiveStrength("Emissive Strength", Range(0,12)) = 6.0
        _PulseSpeed("Pulse Speed", Range(0,10)) = 2.2
        _PulseDepth("Pulse Amount", Range(0,1)) = 0.5

        _DeformAmplitude("Vertex Deform Amplitude", Range(0,0.2)) = 0.03
        _DeformFrequency("Vertex Deform Frequency", Range(0.1,6)) = 3.0
        _DeformAlongNormal("Deform Along Normal", Range(0,1)) = 1.0
        _DeformDrift("Deform Drift Speed", Range(0,2)) = 0.4
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="Opaque" "Queue"="Geometry" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _Scale;
                float4 _FlowDir;
                float  _CellJitter;
                float  _CrackWidth;
                float  _CrackContrast;

                float4 _RockTint;
                float4 _LavaTint;
                float  _EdgeGlow;

                float  _EmissiveStrength;
                float  _PulseSpeed;
                float  _PulseDepth;

                float  _DeformAmplitude;
                float  _DeformFrequency;
                float  _DeformAlongNormal;
                float  _DeformDrift;
            CBUFFER_END

            // === Noise and Voronoi ===
            inline float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            inline float3 hash33(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            struct VorOut { float f1; float2 grad; float2 cellUV; };
            VorOut voronoi2(float2 uv, float time, float jitter, float2 flow)
            {
                float2 p = uv + flow * time;
                float2 g = floor(p);
                float2 f = frac(p);
                float f1 = 8.0;
                float2 df = 0;

                [unroll]
                for (int j = -1; j <= 1; j++)
                {
                    [unroll]
                    for (int i = -1; i <= 1; i++)
                    {
                        float2 o = float2(i, j);
                        float2 h = hash22(g + o);
                        float2 r = o + (h - 0.5) * jitter;
                        r += 0.35 * float2(sin(h.x * 6.283 + time), cos(h.y * 6.283 + time)) * jitter * 0.25;

                        float2 d = f - r;
                        float dist2 = dot(d, d);

                        if (dist2 < f1)
                        {
                            f1 = dist2;
                            df = d;
                        }
                    }
                }
                VorOut o;
                o.f1 = sqrt(max(f1, 1e-6));
                o.grad = normalize(df + 1e-6);
                o.cellUV = p;
                return o;
            }

            float valueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = dot(hash33(i + float3(0,0,0)), 1);
                float n100 = dot(hash33(i + float3(1,0,0)), 1);
                float n010 = dot(hash33(i + float3(0,1,0)), 1);
                float n110 = dot(hash33(i + float3(1,1,0)), 1);
                float n001 = dot(hash33(i + float3(0,0,1)), 1);
                float n101 = dot(hash33(i + float3(1,0,1)), 1);
                float n011 = dot(hash33(i + float3(0,1,1)), 1);
                float n111 = dot(hash33(i + float3(1,1,1)), 1);

                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);

                float n0 = lerp(n00, n10, u.y);
                float n1 = lerp(n01, n11, u.y);

                return lerp(n0, n1, u.z);
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 tangentWS   : TEXCOORD2;
                float3 bitangentWS : TEXCOORD3;
                float2 uv0         : TEXCOORD4;
                float2 uvScaled    : TEXCOORD5;
                UNITY_VERTEX_OUTPUT_STEREO
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs posInputs = GetVertexPositionInputs(v.positionOS.xyz);
                VertexNormalInputs   nrmInputs = GetVertexNormalInputs(v.normalOS, v.tangentOS);
                float3 nWS = normalize(nrmInputs.normalWS);

                float t = _Time.y;
                float3 wp = posInputs.positionWS * _DeformFrequency + float3(_DeformDrift * t, 0, _DeformDrift * t);
                float n = valueNoise3(wp) * 2.0 - 1.0;
                float deform = n * _DeformAmplitude;

                float3 deformedWS = posInputs.positionWS + nWS * deform * _DeformAlongNormal;

                o.positionWS  = deformedWS;
                o.positionHCS = TransformWorldToHClip(deformedWS);

                o.normalWS    = nWS;
                o.tangentWS   = nrmInputs.tangentWS;
                o.bitangentWS = nrmInputs.bitangentWS;
                o.uv0         = v.uv;
                o.uvScaled    = v.uv * _Scale;
                return o;
            }

            float3 normalFromHeight(float h, float2 grad)
            {
                float dhdx = ddx(h);
                float dhdy = ddy(h);
                float2 g = normalize(grad * 0.75 + float2(dhdx, dhdy));
                return normalize(float3(-g.x, -g.y, 1.0));
            }

            float4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float t = _Time.y;
                float2 flow = _FlowDir.xy * _FlowDir.z;

                VorOut cell = voronoi2(i.uvScaled, t, _CellJitter, flow);

                float crack = exp(-cell.f1 * _CrackWidth);
                crack = pow(saturate(crack), _CrackContrast);
                float edge = saturate(length(cell.grad)) * _EdgeGlow;

                float lavaMask = saturate(crack);
                float rockMask = 1.0 - lavaMask;

                float height = rockMask * 0.4 + lavaMask * 0.1;
                float3 Ndetail = normalFromHeight(height, cell.grad);
                float3x3 TBN = float3x3(normalize(i.tangentWS), normalize(i.bitangentWS), normalize(i.normalWS));
                float3 N = normalize(mul(Ndetail, TBN));

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                half shadowAtten = MainLightRealtimeShadow(shadowCoord);

                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float NdotL = saturate(dot(N, L));
                float3 litRock = _RockTint.rgb * (NdotL * mainLight.color.rgb * shadowAtten);

                float pulseSeed = sin((cell.cellUV.x + cell.cellUV.y) * 12.9898);
                float pulse = 0.5 + 0.5 * sin(t * _PulseSpeed + pulseSeed * 6.2831);
                pulse = lerp(1.0, pulse, _PulseDepth);

                float3 lavaGlow = _LavaTint.rgb * (lavaMask + edge * 0.35) * pulse;
                lavaGlow *= lerp(0.4, 1.0, shadowAtten); // Optional: let lava glow faintly in shadows

                float3 baseMix = litRock * rockMask + _LavaTint.rgb * (lavaMask * 0.2);
                float3 color = baseMix + lavaGlow * _EmissiveStrength;

                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    float NdotL2 = saturate(dot(N, l.direction));
                    color += _RockTint.rgb * (NdotL2 * l.color.rgb) * rockMask;
                }
                #endif

                return float4(color, 1.0);
            }
            ENDHLSL
        }

        // === ShadowCaster Pass (unchanged, still works) ===
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float  _DeformAmplitude;
                float  _DeformFrequency;
                float  _DeformAlongNormal;
                float  _DeformDrift;
            CBUFFER_END

            float3 hash33(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.xxy + p.yzz) * p.zyx);
            }

            float valueNoise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float n000 = dot(hash33(i + float3(0,0,0)),1);
                float n100 = dot(hash33(i + float3(1,0,0)),1);
                float n010 = dot(hash33(i + float3(0,1,0)),1);
                float n110 = dot(hash33(i + float3(1,1,0)),1);
                float n001 = dot(hash33(i + float3(0,0,1)),1);
                float n101 = dot(hash33(i + float3(1,0,1)),1);
                float n011 = dot(hash33(i + float3(0,1,1)),1);
                float n111 = dot(hash33(i + float3(1,1,1)),1);
                float n00 = lerp(n000, n100, u.x);
                float n10 = lerp(n010, n110, u.x);
                float n01 = lerp(n001, n101, u.x);
                float n11 = lerp(n011, n111, u.x);
                float n0 = lerp(n00, n10, u.y);
                float n1 = lerp(n01, n11, u.y);
                return lerp(n0, n1, u.z);
            }

            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings   { float4 positionHCS:SV_POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes v)
            {
                Varyings o; UNITY_SETUP_INSTANCE_ID(v); UNITY_TRANSFER_INSTANCE_ID(v, o);
                float3 posWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 nWS   = TransformObjectToWorldNormal(v.normalOS);

                float t = _Time.y;
                float3 wp = posWS * _DeformFrequency + float3(_DeformDrift * t, 0, _DeformDrift * t);
                float n = valueNoise3(wp) * 2.0 - 1.0;
                posWS += nWS * (n * _DeformAmplitude) * _DeformAlongNormal;

                o.positionHCS = TransformWorldToHClip(posWS);
                return o;
            }

            float4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }

    FallBack Off
}
