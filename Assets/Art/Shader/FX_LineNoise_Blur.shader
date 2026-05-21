Shader "Custom/FX/eff_UI_Character_Blur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Housing Sketch Match)]
        _Match ("Match Strength", Float) = 0.75
        _BlurRadius ("Blur Radius", Float) = 1.4
        _LineRadius ("Line Radius", Float) = 1.2
        _InkStrength ("Ink Strength", Float) = 0.75
        _Opacity ("Opacity", Float) = 1.0

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 _Color;

            float _Match;
            float _BlurRadius;
            float _LineRadius;
            float _InkStrength;
            float _Opacity;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            fixed4 SampleSprite(float2 uv)
            {
                fixed4 c = tex2D(_MainTex, uv);

                // 투명 영역에 저장된 검은 RGB가 번짐 계산에 섞이는 것 방지
                if (c.a <= 0.001)
                {
                    c.rgb = fixed3(1, 1, 1);
                }

                return c;
            }

            float GetLum(fixed3 rgb)
            {
                return dot(rgb, fixed3(0.299, 0.587, 0.114));
            }

            fixed4 AlphaWeightedBlur(float2 uv, float radius)
            {
                float2 t = _MainTex_TexelSize.xy * max(radius, 0.0);

                fixed4 c0 = SampleSprite(uv);

                fixed4 c1 = SampleSprite(uv + float2(-t.x, 0));
                fixed4 c2 = SampleSprite(uv + float2( t.x, 0));
                fixed4 c3 = SampleSprite(uv + float2(0, -t.y));
                fixed4 c4 = SampleSprite(uv + float2(0,  t.y));

                fixed4 c5 = SampleSprite(uv + float2(-t.x, -t.y));
                fixed4 c6 = SampleSprite(uv + float2(-t.x,  t.y));
                fixed4 c7 = SampleSprite(uv + float2( t.x, -t.y));
                fixed4 c8 = SampleSprite(uv + float2( t.x,  t.y));

                float w0 = 4.0;
                float w1 = 2.0;
                float w2 = 1.0;

                float totalA =
                    c0.a * w0 +
                    (c1.a + c2.a + c3.a + c4.a) * w1 +
                    (c5.a + c6.a + c7.a + c8.a) * w2;

                fixed3 rgbSum =
                    c0.rgb * c0.a * w0 +
                    (c1.rgb * c1.a + c2.rgb * c2.a + c3.rgb * c3.a + c4.rgb * c4.a) * w1 +
                    (c5.rgb * c5.a + c6.rgb * c6.a + c7.rgb * c7.a + c8.rgb * c8.a) * w2;

                fixed4 result;
                result.rgb = rgbSum / max(totalA, 0.0001);

                float avgA = totalA / 16.0;

                // 알파가 과하게 죽으면 유령처럼 보이므로 원본을 보존
                result.a = max(avgA, c0.a * 0.92);

                return result;
            }

            float NeighborDarkMask(float2 uv, float radius)
            {
                float2 t = _MainTex_TexelSize.xy * max(radius, 0.0);

                fixed4 c0 = SampleSprite(uv);

                fixed4 c1 = SampleSprite(uv + float2(-t.x, 0));
                fixed4 c2 = SampleSprite(uv + float2( t.x, 0));
                fixed4 c3 = SampleSprite(uv + float2(0, -t.y));
                fixed4 c4 = SampleSprite(uv + float2(0,  t.y));

                fixed4 c5 = SampleSprite(uv + float2(-t.x, -t.y));
                fixed4 c6 = SampleSprite(uv + float2(-t.x,  t.y));
                fixed4 c7 = SampleSprite(uv + float2( t.x, -t.y));
                fixed4 c8 = SampleSprite(uv + float2( t.x,  t.y));

                float d0 = (1.0 - GetLum(c0.rgb)) * c0.a;
                float d1 = (1.0 - GetLum(c1.rgb)) * c1.a;
                float d2 = (1.0 - GetLum(c2.rgb)) * c2.a;
                float d3 = (1.0 - GetLum(c3.rgb)) * c3.a;
                float d4 = (1.0 - GetLum(c4.rgb)) * c4.a;
                float d5 = (1.0 - GetLum(c5.rgb)) * c5.a;
                float d6 = (1.0 - GetLum(c6.rgb)) * c6.a;
                float d7 = (1.0 - GetLum(c7.rgb)) * c7.a;
                float d8 = (1.0 - GetLum(c8.rgb)) * c8.a;

                float dark = d0;
                dark = max(dark, d1);
                dark = max(dark, d2);
                dark = max(dark, d3);
                dark = max(dark, d4);
                dark = max(dark, d5);
                dark = max(dark, d6);
                dark = max(dark, d7);
                dark = max(dark, d8);

                return saturate(dark);
            }

            fixed3 ReduceSharpContrast(fixed3 rgb, float amount)
            {
                // 검은 선을 완전히 죽이지 않고,
                // 너무 또렷한 대비만 줄인다.
                fixed3 mid = fixed3(0.55, 0.55, 0.55);
                fixed3 softened = lerp(mid, rgb, 0.78);

                return lerp(rgb, softened, amount);
            }

            fixed4 MakeHousingSketch(float2 uv)
            {
                float match = saturate(_Match);

                fixed4 original = SampleSprite(uv);

                // 1. 먼저 원본 고해상도 디테일을 흐림
                fixed4 blurred = AlphaWeightedBlur(uv, _BlurRadius);

                // 2. 흐려진 결과를 기본으로 사용해서 선명도 차이를 줄임
                fixed4 baseCol;
                baseCol.rgb = lerp(original.rgb, blurred.rgb, match);
                baseCol.a = lerp(original.a, blurred.a, match);

                // 3. 주변 어두운 선을 넓게 감지해서 손그림처럼 번진 선을 얹음
                float darkMask = NeighborDarkMask(uv, _LineRadius);

                // Match가 높을수록 원본의 얇은 선보다 번진 선 쪽을 더 사용
                float ink = saturate(darkMask * _InkStrength * match);

                fixed3 inked = lerp(baseCol.rgb, fixed3(0, 0, 0), ink);

                // 4. 너무 또렷한 대비를 살짝 줄임
                inked = ReduceSharpContrast(inked, match * 0.45);

                fixed4 result;
                result.rgb = saturate(inked);
                result.a = baseCol.a * _Opacity;

                return result;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = MakeHousingSketch(i.uv);
                col *= i.color;
                return col;
            }

            ENDCG
        }
    }
}