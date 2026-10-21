Shader "LudumDare60/ImageBurn"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
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
        Blend One OneMinusSrcAlpha
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

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            float Fbm(float2 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * ValueNoise(p);
                    p *= 2.03;
                    a *= 0.5;
                }
                return v;
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
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                half3 color = tex.rgb;
                half3 glow = 0;

                if (_PB_Burn > 0.0)
                {
                    float2 uv = mul(_PB_Matrix, float4(IN.positionWS, 1.0)).xy;
                    float dist = distance(uv, _PB_Origin.xy) / 1.42;
                    float field = dist * (1.0 - _PB_NoiseStrength) + Fbm(uv * _PB_NoiseScale) * _PB_NoiseStrength;
                    float threshold = _PB_Burn * (1.0 + _PB_CharWidth + _PB_EdgeWidth) - _PB_EdgeWidth;
                    float d = field - threshold;
                    clip(d);
                    float charAmount = 1.0 - saturate((d - _PB_EdgeWidth) / max(_PB_CharWidth, 1e-4));
                    color = lerp(color, _PB_CharColor.rgb, charAmount);
                    float ember = 1.0 - saturate(d / max(_PB_EdgeWidth, 1e-4));
                    glow = _PB_EdgeColor.rgb * ember * ember;
                }

                clip(tex.a - 0.001);
                return half4((color + glow) * tex.a, tex.a);
            }
            ENDHLSL
        }
    }
}
