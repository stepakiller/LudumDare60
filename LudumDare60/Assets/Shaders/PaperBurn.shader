Shader "LudumDare60/PaperBurn"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [ToggleUI] _UseMetallicMap ("Use Metallic Map", Float) = 1
        _MetallicGlossMap ("Metallic (R) Smoothness (A)", 2D) = "black" {}
        _Metallic ("Metallic", Range(0,1)) = 0
        _Smoothness ("Smoothness", Range(0,1)) = 0.5
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1
        _OcclusionMap ("Occlusion", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0,1)) = 1
        [Header(Burn)]
        _Burn ("Burn", Range(0,1)) = 0
        _BurnOrigin ("Burn Origin (UV)", Vector) = (0.5, 0, 0, 0)
        _NoiseScale ("Noise Scale", Float) = 6
        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.35
        _CharWidth ("Char Width", Range(0,0.5)) = 0.12
        _CharColor ("Char Color", Color) = (0.08, 0.05, 0.03, 1)
        _EdgeWidth ("Ember Width", Range(0,0.2)) = 0.03
        [HDR] _EdgeColor ("Ember Color", Color) = (6, 1.8, 0.3, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" }
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_MetallicGlossMap);
        TEXTURE2D(_BumpMap);
        TEXTURE2D(_OcclusionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _UseMetallicMap;
            half _Metallic;
            half _Smoothness;
            half _BumpScale;
            half _OcclusionStrength;
            half _Burn;
            float4 _BurnOrigin;
            float _NoiseScale;
            half _NoiseStrength;
            half _CharWidth;
            half4 _CharColor;
            half _EdgeWidth;
            half4 _EdgeColor;
        CBUFFER_END

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

        float BurnField(float2 uv)
        {
            float dist = distance(uv, _BurnOrigin.xy) / 1.42;
            return dist * (1.0 - _NoiseStrength) + Fbm(uv * _NoiseScale) * _NoiseStrength;
        }

        float BurnThreshold()
        {
            return _Burn * (1.0 + _CharWidth + _EdgeWidth) - _EdgeWidth;
        }

        void ClipBurn(float2 uv)
        {
            if (_Burn <= 0.0) return;
            clip(BurnField(uv) - BurnThreshold());
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
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                float fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionWS = pos.positionWS;
                VertexNormalInputs nrm = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.normalWS = nrm.normalWS;
                OUT.tangentWS = float4(nrm.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv = IN.uv;
                OUT.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                float2 uv = IN.uv;
                ClipBurn(uv);

                float2 mapUV = TRANSFORM_TEX(uv, _BaseMap);
                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, mapUV) * _BaseColor;
                half4 metallicGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_BaseMap, mapUV);
                half metallic = _UseMetallicMap > 0.5 ? metallicGloss.r : _Metallic;
                half smoothness = _UseMetallicMap > 0.5 ? metallicGloss.a * _Smoothness : _Smoothness;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BaseMap, mapUV), _BumpScale);
                half occlusionSample = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_BaseMap, mapUV).g;
                half occlusion = lerp(1.0h, occlusionSample, _OcclusionStrength);
                half3 albedo = baseColor.rgb;
                half3 emission = 0;

                if (_Burn > 0.0)
                {
                    float d = BurnField(uv) - BurnThreshold();
                    float charAmount = 1.0 - saturate((d - _EdgeWidth) / max(_CharWidth, 1e-4));
                    albedo = lerp(albedo, _CharColor.rgb, charAmount);
                    float ember = 1.0 - saturate(d / max(_EdgeWidth, 1e-4));
                    emission = _EdgeColor.rgb * ember * ember;
                }

                float3 vertexNormal = normalize(IN.normalWS);
                float3 tangent = normalize(IN.tangentWS.xyz);
                if (!isFrontFace)
                {
                    vertexNormal = -vertexNormal;
                    normalTS.y = -normalTS.y;
                }
                float3 bitangent = IN.tangentWS.w * cross(vertexNormal, tangent);
                float3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(tangent, bitangent, vertexNormal)));

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
                surface.metallic = metallic;
                surface.specular = 0;
                surface.smoothness = smoothness;
                surface.occlusion = occlusion;
                surface.emission = emission;
                surface.normalTS = normalTS;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                return color;
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
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                ClipBurn(IN.uv);
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
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half frag(Varyings IN) : SV_Target
            {
                ClipBurn(IN.uv);
                return IN.positionCS.z;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormals" }
            ZWrite On
            Cull Off

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
                float3 normalWS : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN, bool isFrontFace : SV_IsFrontFace) : SV_Target
            {
                ClipBurn(IN.uv);
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
