Shader "Custom/FX/eff_com_WarningOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Mask Texture", 2D) = "white" {}

        [Header(Fill Pattern)]
        _PatternTex ("Pattern Texture", 2D) = "white" {}

        [Enum(Scroll Texture,0, Radial Wave Texture,1)]
        _PatternMode ("Pattern Mode", Float) = 0

        _PatternScaleX ("Pattern Scale X", Float) = 3
        _PatternScaleY ("Pattern Scale Y", Float) = 3
        _PatternSpeedX ("Pattern Speed X", Float) = 0
        _PatternSpeedY ("Pattern Speed Y", Float) = -0.2
        _PatternRotation ("Pattern Rotation Degree", Float) = 0
        _PatternStrength ("Pattern Strength", Range(0,1)) = 1

        [Header(Radial Wave Texture)]
        _WaveCenterX ("Wave Center X", Float) = 0
        _WaveCenterY ("Wave Center Y", Float) = 0
        _WaveRadialScale ("Wave Radial Scale", Float) = 6
        _WaveAngleScale ("Wave Angle Scale", Float) = 1
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveRotation ("Wave Rotation Degree", Float) = 0

        [Header(Fill Pulse)]
        _FillAlpha ("Fill Alpha", Range(0,1)) = 0.35
        _FillPulseAmount ("Fill Pulse Amount", Range(0,1)) = 0.35
        _FillPulseSpeed ("Fill Pulse Speed", Float) = 2

        [Header(Outline)]
        _OutlineAlpha ("Outline Alpha", Range(0,1)) = 1
        _OuterOutlineWidth ("Outer Outline Width Pixel", Range(0,64)) = 4
        _InnerOutlineWidth ("Inner Outline Width Pixel", Range(0,64)) = 2
        _EdgeSoftness ("Edge Softness", Range(0.25,4)) = 1
        _MaskThreshold ("Mask Threshold", Range(0,1)) = 0.35

        [Header(Outline Edge Motion)]
        _OutlineEdgeNoiseAmount ("Outline Edge Noise Amount Pixel", Range(0,16)) = 1
        _OutlineEdgeNoiseScale ("Outline Edge Noise Scale", Float) = 8
        _OutlineEdgeNoiseSpeedX ("Outline Edge Noise Speed X", Float) = 0.25
        _OutlineEdgeNoiseSpeedY ("Outline Edge Noise Speed Y", Float) = 0.15
        _OutlineNoiseRotation ("Outline Noise Rotation Degree", Float) = 0
        _OutlineNoiseRotateSpeed ("Outline Noise Rotate Speed", Float) = 20
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            // 패턴 크기 보정 고정값.
            // 필요하면 이 값만 코드에서 조정.
            #define PATTERN_SCREEN_SCALE 0.01

            sampler2D _MainTex;

            sampler2D _PatternTex;
            float4 _PatternTex_ST;

            float _PatternMode;

            float _PatternScaleX;
            float _PatternScaleY;
            float _PatternSpeedX;
            float _PatternSpeedY;
            float _PatternRotation;
            float _PatternStrength;

            float _WaveCenterX;
            float _WaveCenterY;
            float _WaveRadialScale;
            float _WaveAngleScale;
            float _WaveSpeed;
            float _WaveRotation;

            float _FillAlpha;
            float _FillPulseAmount;
            float _FillPulseSpeed;

            float _OutlineAlpha;
            float _OuterOutlineWidth;
            float _InnerOutlineWidth;
            float _EdgeSoftness;
            float _MaskThreshold;

            float _OutlineEdgeNoiseAmount;
            float _OutlineEdgeNoiseScale;
            float _OutlineEdgeNoiseSpeedX;
            float _OutlineEdgeNoiseSpeedY;
            float _OutlineNoiseRotation;
            float _OutlineNoiseRotateSpeed;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            float2 Rotate2D(float2 p, float degree)
            {
                float r = radians(degree);
                float s = sin(r);
                float c = cos(r);

                return float2(
                    p.x * c - p.y * s,
                    p.x * s + p.y * c
                );
            }

            float Noise(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float SmoothNoise(float2 p)
            {
                float2 ip = floor(p);
                float2 fp = frac(p);
                float2 u = fp * fp * (3.0 - 2.0 * fp);

                float a = Noise(ip);
                float b = Noise(ip + float2(1, 0));
                float c = Noise(ip + float2(0, 1));
                float d = Noise(ip + float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            v2f vert(appdata v)
            {
                v2f o;

                o.vertex = UnityObjectToClipPos(v.vertex);

                // SpriteRenderer Color.
                // rgb = 패턴/아웃라인 공통 색상
                // a   = 전체 알파
                o.color = v.color;

                o.uv = v.uv;

                return o;
            }

            float SafeSampleAlpha(float2 uv)
            {
                if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1)
                    return 0;

                return tex2D(_MainTex, uv).a;
            }

            float ScreenStableMask(float alphaValue)
            {
                float aa = max(fwidth(alphaValue) * _EdgeSoftness, 0.0001);
                return smoothstep(_MaskThreshold - aa, _MaskThreshold + aa, alphaValue);
            }

            float MaxAlphaAround(float2 uv, float2 px, float2 py)
            {
                float a = 0;

                a = max(a, SafeSampleAlpha(uv + px));
                a = max(a, SafeSampleAlpha(uv - px));
                a = max(a, SafeSampleAlpha(uv + py));
                a = max(a, SafeSampleAlpha(uv - py));

                return a;
            }

            float MinAlphaAround(float2 uv, float2 px, float2 py)
            {
                float a = 1;

                a = min(a, SafeSampleAlpha(uv + px));
                a = min(a, SafeSampleAlpha(uv - px));
                a = min(a, SafeSampleAlpha(uv + py));
                a = min(a, SafeSampleAlpha(uv - py));

                return a;
            }

            float2 GetUVScalePerPixel(float2 uv)
            {
                float2 dx = ddx(uv);
                float2 dy = ddy(uv);

                float uScale = length(float2(dx.x, dy.x));
                float vScale = length(float2(dx.y, dy.y));

                return max(float2(uScale, vScale), float2(0.00001, 0.00001));
            }

            float2 GetPatternCoord(float2 uv)
            {
                float2 uvLocal = uv - 0.5;

                // 화면상 UV 밀도 기반 보정.
                // 스프라이트 크기/스케일 변화와 상관없이 패턴 밀도 유지.
                float2 uvScale = GetUVScalePerPixel(uv);
                float2 screenComp = PATTERN_SCREEN_SCALE / uvScale;

                return uvLocal * screenComp;
            }

            float2 ApplyPatternTexST(float2 uv)
            {
                return uv * _PatternTex_ST.xy + _PatternTex_ST.zw;
            }

            float2 GetScrollPatternUV(float2 coord)
            {
                coord = Rotate2D(coord, _PatternRotation);

                float2 uv = coord * float2(_PatternScaleX, _PatternScaleY);
                uv += _Time.y * float2(_PatternSpeedX, _PatternSpeedY);

                return ApplyPatternTexST(uv);
            }

            float2 GetRadialWavePatternUV(float2 coord)
            {
                float2 center = float2(_WaveCenterX, _WaveCenterY);

                float2 p = coord - center;
                p = Rotate2D(p, _WaveRotation);

                float dist = length(p);
                float angle01 = atan2(p.y, p.x) / 6.2831853 + 0.5;

                float2 uv;
                uv.x = dist * _WaveRadialScale - _Time.y * _WaveSpeed;
                uv.y = angle01 * _WaveAngleScale;

                return ApplyPatternTexST(uv);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float spriteAlpha = i.color.a;
                float3 spriteColor = i.color.rgb;

                float baseAlpha = SafeSampleAlpha(i.uv);
                float fillMask = ScreenStableMask(baseAlpha);

                float2 uvDx = ddx(i.uv);
                float2 uvDy = ddy(i.uv);

                float2 outerPxX = uvDx * _OuterOutlineWidth;
                float2 outerPxY = uvDy * _OuterOutlineWidth;

                float2 innerPxX = uvDx * _InnerOutlineWidth;
                float2 innerPxY = uvDy * _InnerOutlineWidth;

                float2 noisePxX = uvDx * _OutlineEdgeNoiseAmount;
                float2 noisePxY = uvDy * _OutlineEdgeNoiseAmount;

                float2 patternCoord = GetPatternCoord(i.uv);

                float2 outlineNoiseCoord = patternCoord;
                outlineNoiseCoord = Rotate2D(
                    outlineNoiseCoord,
                    _OutlineNoiseRotation + _Time.y * _OutlineNoiseRotateSpeed
                );

                outlineNoiseCoord *= _OutlineEdgeNoiseScale;
                outlineNoiseCoord += _Time.y * float2(_OutlineEdgeNoiseSpeedX, _OutlineEdgeNoiseSpeedY);

                float n1 = SmoothNoise(outlineNoiseCoord);
                float n2 = SmoothNoise(outlineNoiseCoord + 37.23);

                float2 edgeJitter =
                    (n1 - 0.5) * 2.0 * noisePxX +
                    (n2 - 0.5) * 2.0 * noisePxY;

                float2 outlineUV = i.uv + edgeJitter;

                float expandedAlpha = MaxAlphaAround(outlineUV, outerPxX, outerPxY);
                float expandedMask = ScreenStableMask(expandedAlpha);
                float outerOutlineMask = saturate(expandedMask - fillMask);

                float erodedAlpha = MinAlphaAround(outlineUV, innerPxX, innerPxY);
                float erodedMask = ScreenStableMask(erodedAlpha);
                float innerOutlineMask = saturate(fillMask - erodedMask);

                float outlineMask = saturate(outerOutlineMask + innerOutlineMask);

                float2 scrollUV = GetScrollPatternUV(patternCoord);
                float2 radialUV = GetRadialWavePatternUV(patternCoord);

                float2 patternUV = lerp(scrollUV, radialUV, step(0.5, _PatternMode));

                float pattern = tex2D(_PatternTex, patternUV).a;
                pattern = lerp(1, pattern, _PatternStrength);

                float pulse01 = 0.5 + 0.5 * sin(_Time.y * _FillPulseSpeed);

                float fillPulse =
                    1.0 - _FillPulseAmount +
                    _FillPulseAmount * pulse01;

                float outlinePulse =
                    1.0 - _FillPulseAmount +
                    _FillPulseAmount * (1.0 - pulse01);

                // SpriteRenderer Color.a가 전체 알파 제어.
                float fillAlpha = fillMask * _FillAlpha * pattern * fillPulse * spriteAlpha;
                float outlineAlpha = outlineMask * _OutlineAlpha * outlinePulse * spriteAlpha;

                fixed4 finalCol;

                // 패턴과 아웃라인 모두 SpriteRenderer Color.rgb 사용.
                finalCol.rgb = spriteColor;
                finalCol.a = max(fillAlpha, outlineAlpha);

                return finalCol;
            }
            ENDCG
        }
    }
}