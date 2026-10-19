// Плоский "картографический" глобус: океан + материки из маски + сетка параллелей/меридианов.
// Unlit, для URP. Рассчитан на стандартную сферу Unity (UV развёрнуты как карта мира).
Shader "LudumDare60/Globe"
{
    Properties
    {
        [NoScaleOffset] _LandMask ("Land Mask (белое = суша)", 2D) = "black" {}
        _LandThreshold ("Land Threshold", Range(0, 1)) = 0.5
        _LandSoftness ("Land Edge Softness", Range(0.001, 0.5)) = 0.05
        _LongitudeOffset ("Longitude Offset (0..1)", Range(0, 1)) = 0

        _OceanColor ("Ocean Color", Color) = (0.13, 0.18, 0.33, 1)
        _LandColor ("Land Color", Color) = (0.33, 0.86, 1, 1)
        _LineColor ("Grid Color", Color) = (0.33, 0.86, 1, 1)

        _Meridians ("Meridians (количество)", Float) = 36
        _Parallels ("Parallels (количество)", Float) = 18
        _LineWidth ("Line Width (в пикселях)", Range(0.5, 4)) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_LandMask);
            SAMPLER(sampler_LandMask);

            CBUFFER_START(UnityPerMaterial)
                half4 _OceanColor;
                half4 _LandColor;
                half4 _LineColor;
                float _LandThreshold;
                float _LandSoftness;
                float _LongitudeOffset;
                float _Meridians;
                float _Parallels;
                float _LineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // Сглаженная линия сетки постоянной толщины в пикселях.
            // coord - координата в "клетках" сетки, pixelSize - сколько клеток занимает 1 пиксель экрана.
            float GridLine(float coord, float pixelSize)
            {
                float dist = abs(frac(coord - 0.5) - 0.5);   // расстояние до ближайшей линии (0..0.5)
                float halfWidth = _LineWidth * 0.5 * pixelSize;
                return 1.0 - smoothstep(halfWidth - pixelSize * 0.5, halfWidth + pixelSize * 0.5, dist);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Долгота со сдвигом (чтобы повернуть карту без поворота объекта)
                float2 uv = float2(frac(IN.uv.x + _LongitudeOffset), IN.uv.y);

                // На шве сферы U прыгает с 1 на 0, и производные там огромные: из-за этого
                // по шву рисуется лишняя линия, а у текстуры выбирается не тот мип.
                // Берём производные ещё и от копии, сдвинутой на полоборота, и оставляем меньшие.
                float uAlt = frac(uv.x + 0.5);
                float dxU = abs(ddx(uv.x)) < abs(ddx(uAlt)) ? ddx(uv.x) : ddx(uAlt);
                float dyU = abs(ddy(uv.x)) < abs(ddy(uAlt)) ? ddy(uv.x) : ddy(uAlt);
                float2 dx = float2(dxU, ddx(uv.y));
                float2 dy = float2(dyU, ddy(uv.y));

                // --- Сетка ---
                float meridians = max(1.0, round(_Meridians));     // целое, чтобы линии сходились на шве
                float parallels = max(1.0, round(_Parallels));

                float pixelU = (abs(dx.x) + abs(dy.x)) * meridians;
                float pixelV = (abs(dx.y) + abs(dy.y)) * parallels;

                float meridianLine = GridLine(uv.x * meridians, pixelU);
                float parallelLine = GridLine(uv.y * parallels, pixelV);

                // У полюсов меридианы сходятся и сливаются в сплошное пятно - плавно гасим их там
                meridianLine *= saturate((0.5 - pixelU) * 4.0);

                float grid = max(meridianLine, parallelLine);

                // --- Материки ---
                float mask = SAMPLE_TEXTURE2D_GRAD(_LandMask, sampler_LandMask, uv, dx, dy).r;
                float land = smoothstep(_LandThreshold - _LandSoftness, _LandThreshold + _LandSoftness, mask);

                // --- Сборка: океан -> сетка -> материки поверх сетки ---
                half3 color = _OceanColor.rgb;
                color = lerp(color, _LineColor.rgb, grid);
                color = lerp(color, _LandColor.rgb, land);

                return half4(color, 1.0);
            }
            ENDHLSL
        }
    }
}
