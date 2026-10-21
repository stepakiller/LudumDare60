Shader "LudumDare60/CRTScreen"
{
    Properties
    {
        _BaseMap ("Screen Texture", 2D) = "black" {}
        _BaseColor ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness (HDR)", Float) = 1.6
        _OffColor ("Outside Color", Color) = (0.01, 0.012, 0.01, 1)

        [Header(Shape)]
        _Curvature ("Curvature", Range(0, 0.5)) = 0.12
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.08
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.1)) = 0.01
        _Vignette ("Vignette", Range(0, 1)) = 0.35

        [Header(Scanlines)]
        _ScanlineCount ("Scanline Count", Float) = 260
        _ScanlineStrength ("Scanline Strength", Range(0, 1)) = 0.25
        _ScanlineSpeed ("Scanline Scroll Speed", Float) = 2

        [Header(Phosphor Mask)]
        _MaskScale ("Mask Columns", Float) = 500
        _MaskStrength ("Mask Strength", Range(0, 1)) = 0.15

        [Header(Signal)]
        _ChromaticShift ("Chromatic Shift", Range(0, 0.01)) = 0.0015
        _FlickerStrength ("Flicker Strength", Range(0, 0.2)) = 0.03
        _FlickerSpeed ("Flicker Speed", Float) = 24
        _NoiseStrength ("Noise Strength", Range(0, 0.3)) = 0.04
        _RollBarStrength ("Roll Bar Strength", Range(0, 0.5)) = 0.08
        _RollBarSpeed ("Roll Bar Speed", Float) = 0.12
        _RollBarWidth ("Roll Bar Width", Range(0.01, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Brightness;
            half4 _OffColor;
            half _Curvature;
            half _CornerRadius;
            half _EdgeSoftness;
            half _Vignette;
            float _ScanlineCount;
            half _ScanlineStrength;
            float _ScanlineSpeed;
            float _MaskScale;
            half _MaskStrength;
            half _ChromaticShift;
            half _FlickerStrength;
            float _FlickerSpeed;
            half _NoiseStrength;
            half _RollBarStrength;
            float _RollBarSpeed;
            half _RollBarWidth;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 c = IN.uv * 2.0 - 1.0;
                c *= 1.0 + _Curvature * c.yx * c.yx;
                float2 uv = c * 0.5 + 0.5;

                float radius = _CornerRadius * 2.0;
                float2 q = abs(c) - (1.0 - radius);
                float edge = length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - radius;
                float inside = 1.0 - smoothstep(-_EdgeSoftness, _EdgeSoftness, edge);

                float2 mapped = TRANSFORM_TEX(uv, _BaseMap);
                float2 shift = c * _ChromaticShift;
                half3 color;
                color.r = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, mapped + shift).r;
                color.g = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, mapped).g;
                color.b = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, mapped - shift).b;

                float time = _Time.y;

                float scan = sin((uv.y * _ScanlineCount + time * _ScanlineSpeed) * 6.2831853) * 0.5 + 0.5;
                color *= 1.0 - _ScanlineStrength * (1.0 - scan);

                float column = frac(uv.x * _MaskScale) * 3.0;
                half3 triad = half3(column < 1.0, column >= 1.0 && column < 2.0, column >= 2.0);
                color *= lerp(1.0, 0.6 + 0.8 * triad, _MaskStrength);

                float roll = frac(uv.y - time * _RollBarSpeed) - 0.5;
                color *= 1.0 + _RollBarStrength * exp(-(roll * roll) / (_RollBarWidth * _RollBarWidth));

                float flicker = 1.0 + (Hash(float2(floor(time * _FlickerSpeed), 7.13)) - 0.5) * 2.0 * _FlickerStrength;
                color *= flicker;

                color += (Hash(uv * 512.0 + frac(time * 37.0)) - 0.5) * _NoiseStrength;

                color *= 1.0 - _Vignette * dot(c, c) * 0.5;
                color = max(color, 0.0) * _BaseColor.rgb * _Brightness;

                return half4(lerp(_OffColor.rgb, color, inside), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(float4 positionOS : POSITION) : SV_POSITION
            {
                return TransformObjectToHClip(positionOS.xyz);
            }

            half frag(float4 positionCS : SV_POSITION) : SV_Target
            {
                return positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 octahedral = PackNormalOctQuadEncode(normalWS);
                    float2 remapped = saturate(octahedral * 0.5 + 0.5);
                    return half4(PackFloat2To888(remapped), 0.0);
                #else
                    return half4(normalWS, 0.0);
                #endif
            }
            ENDHLSL
        }
    }
}
