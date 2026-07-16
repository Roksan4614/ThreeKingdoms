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

        // 오브젝트별 랜덤화 강도
        _RandomPhaseStrength ("Random Phase Strength", Float) = 6.28318
        _RandomSpeedOffset ("Random Speed Offset", Float) = 0.35
        _RandomStrengthOffset ("Random Strength Offset", Float) = 0.25
        _RandomFrequencyOffset ("Random Frequency Offset", Float) = 0.2

        // 같은 위치에 완전히 겹친 오브젝트도 다르게 하고 싶을 때 수동 오프셋
        _SeedOffset ("Seed Offset", Float) = 0

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
                float4 worldPosition : TEXCOORD1;   // UI clipping용
                float2 objectSeedPos : TEXCOORD2;   // 랜덤 시드용
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

            float _RandomPhaseStrength;
            float _RandomSpeedOffset;
            float _RandomStrengthOffset;
            float _RandomFrequencyOffset;
            float _SeedOffset;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;

                // 오브젝트 위치 기반 시드
                float3 objectWorldOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                o.objectSeedPos = objectWorldOrigin.xy;

                return o;
            }

            float Hash21(float2 p)
            {
                p += _SeedOffset;
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float GetAnchorMask(float2 uv)
            {
                float maskValue = 0.0;

                if (_FixedSide < 0.5)          // Left fixed
                    maskValue = uv.x;
                else if (_FixedSide < 1.5)     // Right fixed
                    maskValue = 1.0 - uv.x;
                else if (_FixedSide < 2.5)     // Bottom fixed
                    maskValue = uv.y;
                else                           // Top fixed
                    maskValue = 1.0 - uv.y;

                return pow(saturate(maskValue), max(0.0001, _WaveFalloff));
            }

            float GetWaveAxis(float2 uv)
            {
                if (_FixedSide < 1.5)
                    return uv.x;   // Left / Right fixed
                else
                    return uv.y;   // Bottom / Top fixed
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float anchorMask = GetAnchorMask(uv);
                float axis = GetWaveAxis(uv);
                float t = _Time.y;

                // 오브젝트별 랜덤값
                float rndA = Hash21(i.objectSeedPos + float2(1.17, 2.31));
                float rndB = Hash21(i.objectSeedPos + float2(3.91, 7.42));
                float rndC = Hash21(i.objectSeedPos + float2(5.73, 1.58));
                float rndD = Hash21(i.objectSeedPos + float2(8.11, 4.26));

                // 각 오브젝트마다 조금씩 다른 움직임
                float phaseOffset = (rndA * 2.0 - 1.0) * _RandomPhaseStrength;
                float speedMul = 1.0 + (rndB * 2.0 - 1.0) * _RandomSpeedOffset;
                float strengthMul = 1.0 + (rndC * 2.0 - 1.0) * _RandomStrengthOffset;
                float frequencyMul = 1.0 + (rndD * 2.0 - 1.0) * _RandomFrequencyOffset;

                float freq1 = _WaveFrequency * frequencyMul;
                float freq2 = _SecondaryFrequency * lerp(0.9, 1.1, rndA);

                float speed1 = _WaveSpeed * speedMul;
                float speed2 = _SecondarySpeed * lerp(0.9, 1.1, rndB);

                float strength1 = _WaveStrength * strengthMul;
                float strength2 = _SecondaryStrength * lerp(0.85, 1.15, rndC);

                float wave1 = sin(axis * freq1 + t * speed1 + phaseOffset);
                float wave2 = sin(axis * freq2 - t * speed2 + phaseOffset * 0.7);

                float wave = wave1 * strength1 + wave2 * strength2;
                wave *= anchorMask;

                // 고정축에 수직 방향 왜곡
                if (_FixedSide < 1.5)
                    uv.y += wave;
                else
                    uv.x += wave;

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