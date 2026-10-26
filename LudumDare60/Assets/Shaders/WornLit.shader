Shader "LudumDare60/WornLit"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
        _Metallic ("Metallic", Range(0, 1)) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2

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
        _FadeColor ("Faded Color", Color) = (0.75, 0.72, 0.65, 1)
        _DirtColor ("Dirt Color", Color) = (0.25, 0.2, 0.12, 1)
        _DirtAmount ("Dirt Amount", Range(0, 1)) = 0.3
        _Seed ("Seed", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _Cutoff;
            half _Smoothness;
            half _Metallic;
            half _WearAmount;
            float _WearScale;
            half _WearSoftness;
            half _Scratches;
            float _ScratchScale;
            half _ScratchWidth;
            float _ScratchAngle;
            half _Fade;
            half4 _FadeColor;
            half4 _DirtColor;
            half _DirtAmount;
            float _Seed;
        CBUFFER_END

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

        float WearMask(float3 positionWS)
        {
            float3 p = positionWS * _WearScale + _Seed * 17.31;
            float threshold = _WearAmount * 0.75;
            float chipMask = smoothstep(threshold - _WearSoftness, threshold + _WearSoftness, Fbm3(p));

            float angle = radians(_ScratchAngle);
            float3 direction = float3(cos(angle), sin(angle), 0.0);
            float3 across = float3(-direction.y, direction.x, 0.0);
            float3 sp = positionWS * _ScratchScale + _Seed * 5.17;
            float3 scratchPoint = float3(dot(sp, direction) * 0.04, dot(sp, across), sp.z * 0.04);
            float scratchLine = smoothstep(1.0 - _ScratchWidth, 1.0, Noise3(scratchPoint));
            float scratchGate = step(0.55, Noise3(sp * 0.02 + 3.7));
            float scratchMask = 1.0 - scratchLine * scratchGate * _Scratches;

            return chipMask * scratchMask;
        }

        half4 SampleWorn(float2 uv, float3 positionWS)
        {
            half4 color = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, TRANSFORM_TEX(uv, _BaseMap)) * _BaseColor;
            color.a *= WearMask(positionWS);
            return color;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs position = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = position.positionCS;
                OUT.positionWS = position.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(position.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                half4 color = SampleWorn(IN.uv, IN.positionWS);
                clip(color.a - _Cutoff);

                float3 p = IN.positionWS * _WearScale + _Seed * 17.31;
                float fade = saturate(Fbm3(p * 0.25 + 11.0) * 1.6);
                half3 albedo = lerp(color.rgb, color.rgb * _FadeColor.rgb, (1.0 - fade) * _Fade);
                float dirt = saturate(Fbm3(p * 0.6 + 23.0) * 1.4 - 0.2);
                albedo = lerp(albedo, _DirtColor.rgb, dirt * _DirtAmount);

                float3 normalWS = normalize(IN.normalWS);
                if (!isFrontFace) normalWS = -normalWS;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo;
                surface.alpha = 1;
                surface.metallic = _Metallic;
                surface.smoothness = _Smoothness;
                surface.occlusion = 1;
                surface.normalTS = half3(0, 0, 1);

                half4 result = UniversalFragmentPBR(inputData, surface);
                result.rgb = MixFog(result.rgb, inputData.fogCoord);
                return result;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirection = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirection = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirection));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                OUT.positionWS = positionWS;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(SampleWorn(IN.uv, IN.positionWS).a - _Cutoff);
                return 0;
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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half frag(Varyings IN) : SV_Target
            {
                clip(SampleWorn(IN.uv, IN.positionWS).a - _Cutoff);
                return IN.positionCS.z;
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
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                clip(SampleWorn(IN.uv, IN.positionWS).a - _Cutoff);
                float3 normalWS = normalize(IN.normalWS);
                if (!isFrontFace) normalWS = -normalWS;
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
