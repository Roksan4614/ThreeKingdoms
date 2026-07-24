Shader "Custom/FX/eff_com_Dissolve_UI"
{
    Properties
    {
        [PerRendererData]
        _MainTex ("Main Texture", 2D) = "white" {}

        _Color ("Material Tint", Color) = (1, 1, 1, 1)

        [Header(Reveal)]
        _RevealMap ("Reveal Map", 2D) = "black" {}

        _DissolveAmount
        (
            "Dissolve Amount",
            Range(0, 1)
        ) = 1

        _Feather
        (
            "Reveal Feather",
            Range(0.0001, 0.25)
        ) = 0.02

        [Toggle]
        _ReverseDirection
        (
            "Reverse Direction",
            Float
        ) = 0


        [Header(Noise)]
        _NoiseTex ("Noise Texture", 2D) = "gray" {}

        _NoiseScale
        (
            "Noise Scale XY",
            Vector
        ) = (1, 1, 0, 0)

        _NoiseScroll
        (
            "Noise Scroll XY",
            Vector
        ) = (0, 0, 0, 0)

        _NoiseStrength
        (
            "Noise Strength",
            Range(0, 0.25)
        ) = 0


        [Header(Edge)]
        [HDR]
        _EdgeColor
        (
            "Edge Color",
            Color
        ) = (1, 1, 1, 1)

        _EdgeWidth
        (
            "Edge Width",
            Range(0, 0.25)
        ) = 0.03

        _EdgePower
        (
            "Edge Power",
            Range(0, 5)
        ) = 0


        [Header(Output)]
        _Opacity
        (
            "Opacity",
            Range(0, 1)
        ) = 1


        [Header(UI Stencil)]
        _StencilComp
        (
            "Stencil Comparison",
            Float
        ) = 8

        _Stencil
        (
            "Stencil ID",
            Float
        ) = 0

        _StencilOp
        (
            "Stencil Operation",
            Float
        ) = 0

        _StencilWriteMask
        (
            "Stencil Write Mask",
            Float
        ) = 255

        _StencilReadMask
        (
            "Stencil Read Mask",
            Float
        ) = 255

        _ColorMask
        (
            "Color Mask",
            Float
        ) = 15

        [Toggle(UNITY_UI_ALPHACLIP)]
        _UseUIAlphaClip
        (
            "Use Alpha Clip",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
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
            Name "Path Reveal Dissolve"

            CGPROGRAM

            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"


            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct v2f
            {
                float4 vertex : SV_POSITION;

                fixed4 color : COLOR;

                float2 mainUV : TEXCOORD0;
                float2 revealUV : TEXCOORD1;
                float2 noiseUV : TEXCOORD2;

                float4 localPosition : TEXCOORD3;

                UNITY_VERTEX_OUTPUT_STEREO
            };


            sampler2D _MainTex;
            float4 _MainTex_ST;

            sampler2D _RevealMap;
            float4 _RevealMap_ST;

            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            fixed4 _Color;
            fixed4 _TextureSampleAdd;

            float _DissolveAmount;
            float _Feather;
            float _ReverseDirection;

            float4 _NoiseScale;
            float4 _NoiseScroll;
            float _NoiseStrength;

            fixed4 _EdgeColor;
            float _EdgeWidth;
            float _EdgePower;

            float _Opacity;

            float4 _ClipRect;


            v2f vert(appdata_t v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.localPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);

                /*
                 * v.color에는 다음 값이 전달됩니다.
                 *
                 * - Image 컴포넌트의 Color
                 * - Image 컴포넌트의 Alpha
                 * - CanvasGroup의 Alpha
                 */
                o.color = v.color * _Color;

                o.mainUV =
                    TRANSFORM_TEX(v.texcoord, _MainTex);

                o.revealUV =
                    TRANSFORM_TEX(v.texcoord, _RevealMap);

                float2 noiseBaseUV =
                    TRANSFORM_TEX(v.texcoord, _NoiseTex);

                /*
                 * 시간이 계속 증가하면서 UV 좌표가 커져 발생하는
                 * 부동소수점 정밀도 저하를 막기 위해 frac을 사용합니다.
                 */
                float2 noiseOffset =
                    frac(_NoiseScroll.xy * _Time.y);

                o.noiseUV =
                    noiseBaseUV * _NoiseScale.xy +
                    noiseOffset;

                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                /*
                 * 메인 이미지
                 *
                 * i.color를 곱하므로 Canvas Image의 Color와 Alpha가
                 * 최종 결과에 정상적으로 적용됩니다.
                 */
                fixed4 mainColor =
                    tex2D(_MainTex, i.mainUV) +
                    _TextureSampleAdd;

                mainColor *= i.color;


                /*
                 * Reveal Map
                 *
                 * 검정 0 = 가장 먼저 등장
                 * 흰색 1 = 가장 마지막에 등장
                 */
                float revealOrder =
                    tex2D(_RevealMap, i.revealUV).r;

                revealOrder = lerp
                (
                    revealOrder,
                    1.0 - revealOrder,
                    saturate(_ReverseDirection)
                );


                /*
                 * 노이즈
                 *
                 * 기본 Noise Texture가 Gray이므로
                 * Noise Strength가 있어도 기본적으로 중앙값이 됩니다.
                 */
                float noiseSample =
                    tex2D(_NoiseTex, i.noiseUV).r;

                float noiseOffset =
                    (noiseSample - 0.5) *
                    _NoiseStrength;

                float threshold =
                    saturate(revealOrder + noiseOffset);


                float progress =
                    saturate(_DissolveAmount);

                float feather =
                    max(_Feather, 0.0001);


                /*
                 * 현재 진행도가 해당 픽셀의 생성 순서에 도달했는지 판단
                 */
                float visible =
                    smoothstep
                    (
                        threshold - feather,
                        threshold + feather,
                        progress
                    );


                /*
                 * Amount가 정확히 0일 때는 완전히 숨기고,
                 * 정확히 1일 때는 완전히 표시합니다.
                 *
                 * Reveal Map의 최솟값과 최댓값에서 발생할 수 있는
                 * 반투명 잔여 픽셀을 제거하기 위한 처리입니다.
                 */
                if (progress <= 0.0001)
                {
                    visible = 0.0;
                }
                else if (progress >= 0.9999)
                {
                    visible = 1.0;
                }


                /*
                 * 생성 경계선
                 */
                float edgeSoftness =
                    max(feather, 0.0001);

                float distanceFromFront =
                    abs(progress - threshold);

                float edge =
                    1.0 -
                    smoothstep
                    (
                        _EdgeWidth,
                        _EdgeWidth + edgeSoftness,
                        distanceFromFront
                    );

                /*
                 * 아직 완전히 숨겨진 영역에 경계가 표시되지 않도록
                 * 현재 가시도와 곱합니다.
                 */
                edge *= visible;

                float edgeBlend =
                    saturate(edge * _EdgePower);

                mainColor.rgb =
                    lerp
                    (
                        mainColor.rgb,
                        _EdgeColor.rgb,
                        edgeBlend
                    );


                /*
                 * 메인 이미지 알파
                 * × Canvas Image 알파
                 * × 생성 진행도
                 * × 셰이더 Opacity
                 */
                mainColor.a *=
                    visible *
                    saturate(_Opacity);


                /*
                 * RectMask2D 대응
                 */
#ifdef UNITY_UI_CLIP_RECT

                mainColor.a *=
                    UnityGet2DClipping
                    (
                        i.localPosition.xy,
                        _ClipRect
                    );

#endif


                /*
                 * UI Alpha Clip 대응
                 */
#ifdef UNITY_UI_ALPHACLIP

                clip(mainColor.a - 0.001);

#endif

                return mainColor;
            }

            ENDCG
        }
    }

    FallBack "UI/Default"
}