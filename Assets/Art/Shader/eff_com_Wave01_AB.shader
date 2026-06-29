Shader "Custom/FX/eff_com_Wave01_AB"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)

        _WaveStrength ("Wave Strength", Range(0, 0.1)) = 0.03
        _WaveSpeed ("Wave Speed", Float) = 2.0
        _WaveFrequency ("Wave Frequency", Float) = 8.0
        _WaveFalloff ("Fixed Axis Falloff", Float) = 1.6

        _SecondaryStrength ("Secondary Strength", Range(0, 0.1)) = 0.01
        _SecondaryFrequency ("Secondary Frequency", Float) = 16.0
        _SecondarySpeed ("Secondary Speed", Float) = 3.0

        _FixedSide ("Fixed Side (0=Left, 1=Right, 2=Bottom, 3=Top)", Float) = 0

        // UI Mask / Stencil
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15

        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Name "Default"

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
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 uv            : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _ClipRect;

            float _WaveStrength;
            float _WaveSpeed;
            float _WaveFrequency;
            float _WaveFalloff;

            float _SecondaryStrength;
            float _SecondaryFrequency;
            float _SecondarySpeed;

            float _FixedSide;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;

                return o;
            }

            float GetAnchorMask(float2 uv)
            {
                float fixedSide = _FixedSide;
                float maskValue = 0.0;

                if (fixedSide < 0.5)          // Left fixed
                    maskValue = uv.x;
                else if (fixedSide < 1.5)     // Right fixed
                    maskValue = 1.0 - uv.x;
                else if (fixedSide < 2.5)     // Bottom fixed
                    maskValue = uv.y;
                else                          // Top fixed
                    maskValue = 1.0 - uv.y;

                return pow(saturate(maskValue), max(0.0001, _WaveFalloff));
            }

            float GetWaveAxis(float2 uv)
            {
                float fixedSide = _FixedSide;

                if (fixedSide < 1.5)
                    return uv.x;   // Left / Right fixed -> x축 따라 흐름
                else
                    return uv.y;   // Bottom / Top fixed -> y축 따라 흐름
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float anchorMask = GetAnchorMask(uv);
                float axis = GetWaveAxis(uv);
                float t = _Time.y;

                float wave1 = sin(axis * _WaveFrequency + t * _WaveSpeed);
                float wave2 = sin(axis * _SecondaryFrequency - t * _SecondarySpeed);

                float wave = wave1 * _WaveStrength + wave2 * _SecondaryStrength;
                wave *= anchorMask;

                // 고정축에 수직한 방향으로 UV 왜곡
                if (_FixedSide < 1.5)
                {
                    uv.y += wave;   // Left/Right 고정이면 위아래로 흔들림
                }
                else
                {
                    uv.x += wave;   // Top/Bottom 고정이면 좌우로 흔들림
                }

                fixed4 col = tex2D(_MainTex, uv) * i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}