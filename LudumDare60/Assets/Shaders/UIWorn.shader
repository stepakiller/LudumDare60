Shader "LudumDare60/UIWorn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Chipped Paint)]
        _WearAmount ("Wear Amount", Range(0, 1)) = 0.35
        _WearScale ("Wear Scale", Float) = 150
        _WearSoftness ("Wear Softness", Range(0.005, 0.3)) = 0.05

        [Header(Scratches)]
        _Scratches ("Scratches", Range(0, 1)) = 0.5
        _ScratchScale ("Scratch Scale", Float) = 400
        _ScratchWidth ("Scratch Width", Range(0.01, 0.3)) = 0.06
        _ScratchAngle ("Scratch Angle", Range(0, 180)) = 25

        [Header(Fade and Dirt)]
        _Fade ("Uneven Fade", Range(0, 1)) = 0.35
        _DirtColor ("Dirt Color", Color) = (0.25, 0.2, 0.12, 1)
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.3
        _Seed ("Seed", Float) = 0
        [HideInInspector] _BurnSupported ("Burn Supported", Float) = 1

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" "RenderPipeline"="UniversalPipeline" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half _WearAmount;
                float _WearScale;
                half _WearSoftness;
                half _Scratches;
                float _ScratchScale;
                half _ScratchWidth;
                float _ScratchAngle;
                half _Fade;
                half4 _DirtColor;
                half _DirtAmount;
                float _Seed;
            CBUFFER_END

            float4x4 _PB_Matrix;
            float _PB_Burn;
            float4 _PB_Origin;
            float _PB_NoiseScale;
            float _PB_NoiseStrength;
            float _PB_CharWidth;
            float _PB_EdgeWidth;
            float4 _PB_CharColor;
            float4 _PB_EdgeColor;

            float Hash2(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise2(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash2(i);
                float b = Hash2(i + float2(1, 0));
                float c = Hash2(i + float2(0, 1));
                float d = Hash2(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm2(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * ValueNoise2(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash3(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);

                float n000 = Hash3(i);
                float n100 = Hash3(i + float3(1, 0, 0));
                float n010 = Hash3(i + float3(0, 1, 0));
                float n110 = Hash3(i + float3(1, 1, 0));
                float n001 = Hash3(i + float3(0, 0, 1));
                float n101 = Hash3(i + float3(1, 0, 1));
                float n011 = Hash3(i + float3(0, 1, 1));
                float n111 = Hash3(i + float3(1, 1, 1));

                float x00 = lerp(n000, n100, u.x);
                float x10 = lerp(n010, n110, u.x);
                float x01 = lerp(n001, n101, u.x);
                float x11 = lerp(n011, n111, u.x);
                return lerp(lerp(x00, x10, u.y), lerp(x01, x11, u.y), u.z);
            }

            float Fbm3(float3 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * Noise3(p);
                    p *= 2.07;
                    amplitude *= 0.5;
                }
                return value;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.color = IN.color * _Color;
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;

                float3 p = IN.positionWS * _WearScale + _Seed * 17.31;

                float chips = Fbm3(p);
                float threshold = _WearAmount * 0.75;
                float chipMask = smoothstep(threshold - _WearSoftness, threshold + _WearSoftness, chips);

                float angle = radians(_ScratchAngle);
                float3 direction = float3(cos(angle), sin(angle), 0.0);
                float3 across = float3(-direction.y, direction.x, 0.0);
                float3 sp = IN.positionWS * _ScratchScale + _Seed * 5.17;
                float3 scratchPoint = float3(dot(sp, direction) * 0.04, dot(sp, across), sp.z * 0.04);
                float streak = Noise3(scratchPoint);
                float scratchLine = smoothstep(1.0 - _ScratchWidth, 1.0, streak);
                float scratchGate = step(0.55, Noise3(sp * 0.02 + 3.7));
                float scratchMask = 1.0 - scratchLine * scratchGate * _Scratches;

                float fade = lerp(1.0, saturate(Fbm3(p * 0.25 + 11.0) * 1.6), _Fade);

                float dirt = Fbm3(p * 0.6 + 23.0);
                color.rgb = lerp(color.rgb, _DirtColor.rgb, saturate(dirt * 1.4 - 0.2) * _DirtAmount);

                color.a *= chipMask * scratchMask * fade;

                if (_PB_Burn > 0.0)
                {
                    float2 burnUV = mul(_PB_Matrix, float4(IN.positionWS, 1.0)).xy;
                    float dist = distance(burnUV, _PB_Origin.xy) / 1.42;
                    float field = dist * (1.0 - _PB_NoiseStrength) + Fbm2(burnUV * _PB_NoiseScale) * _PB_NoiseStrength;
                    float threshold = _PB_Burn * (1.0 + _PB_CharWidth + _PB_EdgeWidth) - _PB_EdgeWidth;
                    float d = field - threshold;
                    clip(d);
                    float charAmount = 1.0 - saturate((d - _PB_EdgeWidth) / max(_PB_CharWidth, 1e-4));
                    color.rgb = lerp(color.rgb, _PB_CharColor.rgb, charAmount);
                    float ember = 1.0 - saturate(d / max(_PB_EdgeWidth, 1e-4));
                    color.rgb += _PB_EdgeColor.rgb * ember * ember;
                }

                clip(color.a - 0.001);
                return color;
            }
            ENDHLSL
        }
    }
}
