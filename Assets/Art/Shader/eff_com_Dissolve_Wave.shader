Shader "Custom/FX/eff_com_Dissolve_Wave"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)


        // ============================================================
        // Wave
        // ============================================================

        [Header(Wave)]

        _WaveStrength ("Wave Strength", Range(0, 0.15)) = 0.03

        // 최종 웨이브 전체 배율
        // 0 = 완전 정지
        // 1 = 기본
        _WaveMultiplier ("Wave Multiplier", Range(0, 2)) = 1.0

        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveFrequency ("Wave Frequency", Float) = 8.0

        // 고정점에서 반대쪽으로 갈수록
        // 흔들림이 얼마나 빠르게 증가하는지
        _WaveFalloff ("Fixed Axis Falloff", Float) = 1.6

        // 실제로 완전히 고정할 영역
        _FixedRange ("Fixed Range", Range(0, 0.5)) = 0.05


        // ============================================================
        // Secondary Wave
        // ============================================================

        [Header(Secondary Wave)]

        _SecondaryStrength
        (
            "Secondary Strength",
            Range(0, 0.1)
        ) = 0.01

        _SecondaryFrequency
        (
            "Secondary Frequency",
            Float
        ) = 16.0

        _SecondarySpeed
        (
            "Secondary Speed",
            Float
        ) = 3.0


        // ============================================================
        // Cross Phase
        //
        // 0이면 기존 셰이더처럼
        // 동일한 높이의 픽셀이 같은 방향으로 움직임.
        //
        // 값을 높이면 X/Y 위치에 따라 위상이 달라져서
        // 천이 통째로 흔들리지 않고 표면이 꾸물거림.
        // ============================================================

        [Header(Wave Shape)]

        _CrossPhase
        (
            "Cross Phase",
            Range(0, 10)
        ) = 2.0


        // 0 Left
        // 1 Right
        // 2 Bottom
        // 3 Top
        _FixedSide
        (
            "Fixed Side (0=Left, 1=Right, 2=Bottom, 3=Top)",
            Float
        ) = 3


        // ============================================================
        // Particle Custom Data
        //
        // Custom1.x = Dissolve
        // Custom1.y = Wave Multiplier
        // ============================================================

        [Header(Particle Custom Data)]

        [Toggle]
        _UseCustomDissolve
        (
            "Use Custom1 X - Dissolve",
            Float
        ) = 1

        [Toggle]
        _UseCustomWave
        (
            "Use Custom1 Y - Wave",
            Float
        ) = 1


        // ============================================================
        // Dissolve
        // ============================================================

        [Header(Dissolve)]

        _DissolveTex
        (
            "Dissolve Texture (R)",
            2D
        ) = "white" {}

        // Custom Data를 사용하지 않을 때
        // 직접 테스트용
        _DissolveAmount
        (
            "Dissolve Amount",
            Range(0, 1)
        ) = 0

        _DissolveSoftness
        (
            "Dissolve Softness",
            Range(0.001, 0.25)
        ) = 0.03

        [Toggle]
        _DissolveInvert
        (
            "Invert Dissolve Texture",
            Float
        ) = 0


        // ============================================================
        // Strong Wave Smoothing
        //
        // 강한 UV 변형 시 계단을 완화하기 위해
        // Main Texture를 3회 샘플링.
        //
        // OFF = 1 sample
        // ON  = 3 samples
        // ============================================================

        [Header(Strong Wave Smoothing)]

        [Toggle(WAVE_SMOOTH_ON)]
        _WaveSmooth
        (
            "Smooth Strong Wave",
            Float
        ) = 1

        // 텍셀 단위
        _SmoothSpread
        (
            "Smooth Spread",
            Range(0, 3)
        ) = 0.75


        // ============================================================
        // UV Edge
        //
        // 웨이브가 0~1 범위 밖으로 나갔을 때
        // Clamp 된 가장자리 픽셀이 길게 늘어나는 현상 방지.
        // ============================================================

        [Header(UV Edge)]

        _EdgeFeather
        (
            "UV Edge Feather",
            Range(0.0001, 0.05)
        ) = 0.005


        // ============================================================
        // UI Mask / Stencil
        // ============================================================

        _ClipRect
        (
            "Clip Rect",
            Vector
        ) = (-32767, -32767, 32767, 32767)

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
            Name "Default"


            CGPROGRAM


            #pragma target 2.0

            #pragma vertex vert
            #pragma fragment frag


            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #pragma shader_feature_local _ WAVE_SMOOTH_ON


            #include "UnityCG.cginc"
            #include "UnityUI.cginc"


            // ========================================================
            // Vertex Input
            //
            // Particle Custom Vertex Streams:
            //
            // TEXCOORD0.xy = UV
            // TEXCOORD0.z  = Custom1.x = Dissolve
            // TEXCOORD0.w  = Custom1.y = Wave
            // ========================================================

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;

                float4 texcoord : TEXCOORD0;


                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct v2f
            {
                float4 vertex        : SV_POSITION;

                fixed4 color         : COLOR;

                float2 uv            : TEXCOORD0;

                half2 customData     : TEXCOORD1;

                float4 worldPosition : TEXCOORD2;


                UNITY_VERTEX_OUTPUT_STEREO
            };


            sampler2D _MainTex;
            float4 _MainTex_ST;

            float4 _MainTex_TexelSize;


            sampler2D _DissolveTex;
            float4 _DissolveTex_ST;


            fixed4 _Color;

            float4 _ClipRect;


            float _WaveStrength;
            float _WaveMultiplier;

            float _WaveSpeed;
            float _WaveFrequency;

            float _WaveFalloff;

            float _FixedRange;

            float _SecondaryStrength;
            float _SecondaryFrequency;
            float _SecondarySpeed;

            float _CrossPhase;

            float _FixedSide;


            float _UseCustomDissolve;
            float _UseCustomWave;


            float _DissolveAmount;
            float _DissolveSoftness;
            float _DissolveInvert;


            float _SmoothSpread;

            float _EdgeFeather;


            // ========================================================
            // Vertex
            // ========================================================

            v2f vert(appdata_t v)
            {
                v2f o;


                UNITY_SETUP_INSTANCE_ID(v);

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);


                o.worldPosition =
                    v.vertex;


                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );


                // raw 0~1 UV
                o.uv =
                    v.texcoord.xy;


                // Particle Color / Alpha
                o.color =
                    v.color
                    * _Color;


                // Custom1.x / Custom1.y
                o.customData =
                    v.texcoord.zw;


                return o;
            }


            // ========================================================
            // Fixed Mask
            //
            // 0 = 완전 고정
            // 1 = 완전 흔들림
            // ========================================================

            float GetAnchorMask(float2 uv)
            {
                float distanceFromFixed;


                if (_FixedSide < 0.5)
                {
                    // Left fixed
                    distanceFromFixed =
                        uv.x;
                }
                else if (_FixedSide < 1.5)
                {
                    // Right fixed
                    distanceFromFixed =
                        1.0 - uv.x;
                }
                else if (_FixedSide < 2.5)
                {
                    // Bottom fixed
                    distanceFromFixed =
                        uv.y;
                }
                else
                {
                    // Top fixed
                    distanceFromFixed =
                        1.0 - uv.y;
                }


                // ----------------------------------------------------
                // Fixed Range
                //
                // 지정 영역까지는 완전히 0.
                //
                // 그 이후부터 0~1로 다시 정규화.
                // ----------------------------------------------------

                float range =
                    saturate(
                        _FixedRange
                    );


                float movable =
                    saturate(
                        (
                            distanceFromFixed
                            - range
                        )
                        /
                        max(
                            1.0 - range,
                            0.0001
                        )
                    );


                return pow(
                    movable,
                    max(
                        0.0001,
                        _WaveFalloff
                    )
                );
            }


            // ========================================================
            // Main Wave Axis
            // ========================================================

            float GetWaveAxis(float2 uv)
            {
                if (_FixedSide < 1.5)
                {
                    // Left / Right fixed
                    return uv.x;
                }

                // Bottom / Top fixed
                return uv.y;
            }


            // ========================================================
            // Cross Axis
            //
            // Wave가 같은 행/열 전체에서
            // 똑같이 움직이는 현상을 깨뜨림.
            // ========================================================

            float GetCrossAxis(float2 uv)
            {
                if (_FixedSide < 1.5)
                {
                    return uv.y;
                }

                return uv.x;
            }


            // ========================================================
            // Edge Mask
            //
            // UV가 텍스처 영역 밖으로 밀렸을 때
            // Clamp pixel이 길게 늘어나는 현상을 제거.
            // ========================================================

            float GetUVInsideMask(float2 uv)
            {
                float feather =
                    max(
                        _EdgeFeather,
                        0.0001
                    );


                float left =
                    smoothstep(
                        0.0,
                        feather,
                        uv.x
                    );


                float right =
                    1.0 -
                    smoothstep(
                        1.0 - feather,
                        1.0,
                        uv.x
                    );


                float bottom =
                    smoothstep(
                        0.0,
                        feather,
                        uv.y
                    );


                float top =
                    1.0 -
                    smoothstep(
                        1.0 - feather,
                        1.0,
                        uv.y
                    );


                return
                    left
                    * right
                    * bottom
                    * top;
            }


            // ========================================================
            // Main Texture Sample
            // ========================================================

            fixed4 SampleMain(
                float2 rawUV,
                float2 distortionDirection
            )
            {
                float2 mainUV =
                    TRANSFORM_TEX(
                        rawUV,
                        _MainTex
                    );


                // ----------------------------------------------------
                // Normal mode
                // ----------------------------------------------------

                #ifndef WAVE_SMOOTH_ON


                return tex2D(
                    _MainTex,
                    mainUV
                );


                // ----------------------------------------------------
                // 3 Tap smoothing
                // ----------------------------------------------------

                #else


                float2 texelOffset =
                    distortionDirection
                    * _MainTex_TexelSize.xy
                    * _SmoothSpread;


                fixed4 sampleCenter =
                    tex2D(
                        _MainTex,
                        mainUV
                    );


                fixed4 sampleA =
                    tex2D(
                        _MainTex,
                        mainUV
                        + texelOffset
                    );


                fixed4 sampleB =
                    tex2D(
                        _MainTex,
                        mainUV
                        - texelOffset
                    );


                // Center 비중을 조금 더 크게
                return
                    sampleCenter * 0.5
                    +
                    sampleA * 0.25
                    +
                    sampleB * 0.25;


                #endif
            }


            // ========================================================
            // Fragment
            // ========================================================

            fixed4 frag(v2f i) : SV_Target
            {
                float2 baseUV =
                    i.uv;


                // ----------------------------------------------------
                // Anchor
                // ----------------------------------------------------

                float anchorMask =
                    GetAnchorMask(
                        baseUV
                    );


                // ----------------------------------------------------
                // Wave axes
                // ----------------------------------------------------

                float axis =
                    GetWaveAxis(
                        baseUV
                    );


                float crossAxis =
                    GetCrossAxis(
                        baseUV
                    );


                float time =
                    _Time.y;


                // ----------------------------------------------------
                // Primary
                //
                // CrossPhase 덕분에
                // 같은 높이의 픽셀이 완전히 동일하게
                // 움직이지 않음.
                // ----------------------------------------------------

                float primaryPhase =
                    axis
                    * _WaveFrequency

                    +

                    crossAxis
                    * _CrossPhase

                    +

                    time
                    * _WaveSpeed;


                float primaryWave =
                    sin(
                        primaryPhase
                    )
                    * _WaveStrength;


                // ----------------------------------------------------
                // Secondary
                // ----------------------------------------------------

                float secondaryPhase =
                    axis
                    * _SecondaryFrequency

                    +

                    crossAxis
                    * (
                        _CrossPhase
                        * 1.618
                    )

                    -

                    time
                    * _SecondarySpeed;


                float secondaryWave =
                    sin(
                        secondaryPhase
                    )
                    * _SecondaryStrength;


                // ----------------------------------------------------
                // Particle Wave Multiplier
                //
                // Custom1.y
                //
                // 0 = 정지
                // 1 = 100%
                // ----------------------------------------------------

                float particleWave =
                    lerp(
                        1.0,
                        saturate(
                            i.customData.y
                        ),
                        saturate(
                            _UseCustomWave
                        )
                    );


                // ----------------------------------------------------
                // IMPORTANT
                //
                // Wave Multiplier는 무조건 최종 단계에서 적용.
                //
                // 0이면 어떤 조건에서도 wave = 0.
                // ----------------------------------------------------

                float finalWaveMultiplier =
                    max(
                        0.0,
                        _WaveMultiplier
                    )
                    * particleWave;


                float wave =
                    (
                        primaryWave
                        +
                        secondaryWave
                    )

                    * anchorMask

                    * finalWaveMultiplier;


                // ----------------------------------------------------
                // UV Distortion
                // ----------------------------------------------------

                float2 warpedUV =
                    baseUV;


                float2 distortionDirection;


                if (_FixedSide < 1.5)
                {
                    // Left / Right fixed
                    //
                    // 고정축에 수직으로 움직임
                    warpedUV.y +=
                        wave;


                    distortionDirection =
                        float2(
                            0.0,
                            1.0
                        );
                }
                else
                {
                    // Bottom / Top fixed

                    warpedUV.x +=
                        wave;


                    distortionDirection =
                        float2(
                            1.0,
                            0.0
                        );
                }


                // ----------------------------------------------------
                // UV Outside protection
                // ----------------------------------------------------

                float insideMask =
                    GetUVInsideMask(
                        warpedUV
                    );


                // ----------------------------------------------------
                // Main
                // ----------------------------------------------------

                fixed4 col =
                    SampleMain(
                        warpedUV,
                        distortionDirection
                    );


                col *=
                    i.color;


                col.a *=
                    insideMask;


                // ====================================================
                // Dissolve
                // ====================================================

                float dissolveAmount =
                    lerp(
                        _DissolveAmount,
                        saturate(
                            i.customData.x
                        ),
                        saturate(
                            _UseCustomDissolve
                        )
                    );


                float2 dissolveUV =
                    TRANSFORM_TEX(
                        warpedUV,
                        _DissolveTex
                    );


                half dissolveValue =
                    tex2D(
                        _DissolveTex,
                        dissolveUV
                    ).r;


                dissolveValue =
                    lerp(
                        dissolveValue,
                        1.0h - dissolveValue,
                        saturate(
                            _DissolveInvert
                        )
                    );


                half softness =
                    max(
                        (half)_DissolveSoftness,
                        0.001h
                    );


                half threshold =
                    lerp(
                        -softness,
                        1.0h + softness,
                        saturate(
                            dissolveAmount
                        )
                    );


                half dissolveAlpha =
                    smoothstep(
                        threshold - softness,
                        threshold + softness,
                        dissolveValue
                    );


                col.a *=
                    dissolveAlpha;


                // ====================================================
                // RectMask2D
                // ====================================================

                #ifdef UNITY_UI_CLIP_RECT


                col.a *=
                    UnityGet2DClipping(
                        i.worldPosition.xy,
                        _ClipRect
                    );


                #endif


                // ====================================================
                // Alpha Clip
                // ====================================================

                #ifdef UNITY_UI_ALPHACLIP


                clip(
                    col.a
                    - 0.001
                );


                #endif


                return col;
            }


            ENDCG
        }
    }
}