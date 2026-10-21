Shader "LudumDare60/TextBurn"
{
    Properties
    {
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Thickness", Range(0,1)) = 0
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0
        _UnderlayColor ("Underlay Color", Color) = (0,0,0,0.5)
        _UnderlayOffsetX ("Underlay Offset X", Range(-1,1)) = 0
        _UnderlayOffsetY ("Underlay Offset Y", Range(-1,1)) = 0
        _UnderlayDilate ("Underlay Dilate", Range(-1,1)) = 0
        _UnderlaySoftness ("Underlay Softness", Range(0,1)) = 0
        _WeightNormal ("Weight Normal", Float) = 0
        _WeightBold ("Weight Bold", Float) = 0.5
        _ShaderFlags ("Flags", Float) = 0
        _ScaleRatioA ("Scale RatioA", Float) = 1
        _ScaleRatioB ("Scale RatioB", Float) = 1
        _ScaleRatioC ("Scale RatioC", Float) = 1
        _MainTex ("Font Atlas", 2D) = "white" {}
        _TextureWidth ("Texture Width", Float) = 512
        _TextureHeight ("Texture Height", Float) = 512
        _GradientScale ("Gradient Scale", Float) = 5
        _ScaleX ("Scale X", Float) = 1
        _ScaleY ("Scale Y", Float) = 1
        _PerspectiveFilter ("Perspective Correction", Range(0,1)) = 0.875
        _Sharpness ("Sharpness", Range(-1,1)) = 0
        _VertexOffsetX ("Vertex OffsetX", Float) = 0
        _VertexOffsetY ("Vertex OffsetY", Float) = 0
        _ClipRect ("Clip Rect", Vector) = (-32767,-32767,32767,32767)
        _MaskSoftnessX ("Mask SoftnessX", Float) = 0
        _MaskSoftnessY ("Mask SoftnessY", Float) = 0
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _CullMode ("Cull Mode", Float) = 0
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull [_CullMode]
        ZWrite Off
        Lighting Off
        Fog { Mode Off }
        ZTest LEqual
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
                half4 _FaceColor;
                float _FaceDilate;
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
                float4 uv : TEXCOORD0;
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
                OUT.color = IN.color * _FaceColor;
                OUT.uv = IN.uv.xy;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float sdf = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                float edge = 0.5 - _FaceDilate * 0.25;
                float w = max(fwidth(sdf), 1e-4);
                float alpha = smoothstep(edge - w, edge + w, sdf);

                half3 color = IN.color.rgb;
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

                alpha *= IN.color.a;
                clip(alpha - 0.001);
                return half4((color + glow) * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
