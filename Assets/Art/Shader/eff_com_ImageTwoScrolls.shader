Shader "Custom/FX/eff_com_ImageTwoScrolls"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Global)]
        _Rotation ("Fixed Rotation Degree", Range(-180, 180)) = 0
        _OverallAlpha ("Overall Alpha", Range(0, 1)) = 1
        _GlobalSpeed ("Global Speed", Float) = 1

        [Header(Layer 1)]
        _Layer1ScaleX ("Layer 1 Scale X", Float) = 1
        _Layer1ScaleY ("Layer 1 Scale Y", Float) = 1
        _Layer1MoveX ("Layer 1 Move X", Float) = -0.2
        _Layer1MoveY ("Layer 1 Move Y", Float) = -1
        _Layer1Speed ("Layer 1 Speed", Float) = 0.7
        _Layer1Alpha ("Layer 1 Alpha", Range(0, 1)) = 1

        [Header(Layer 2)]
        _Layer2ScaleX ("Layer 2 Scale X", Float) = 1.8
        _Layer2ScaleY ("Layer 2 Scale Y", Float) = 2.3
        _Layer2MoveX ("Layer 2 Move X", Float) = -0.2
        _Layer2MoveY ("Layer 2 Move Y", Float) = -1
        _Layer2Speed ("Layer 2 Speed", Float) = 0.35
        _Layer2Alpha ("Layer 2 Alpha", Range(0, 1)) = 0.5

        [Header(UI Mask)]
        _ClipRect ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)

        [Header(Unity UI Stencil)]
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
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
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
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _ClipRect;

            float _Rotation;
            float _OverallAlpha;
            float _GlobalSpeed;

            float _Layer1ScaleX;
            float _Layer1ScaleY;
            float _Layer1MoveX;
            float _Layer1MoveY;
            float _Layer1Speed;
            float _Layer1Alpha;

            float _Layer2ScaleX;
            float _Layer2ScaleY;
            float _Layer2MoveX;
            float _Layer2MoveY;
            float _Layer2Speed;
            float _Layer2Alpha;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;

                float rad = radians(_Rotation);
                float s = sin(rad);
                float c = cos(rad);

                float2 uv = v.uv - 0.5;

                o.uv.x = uv.x * c - uv.y * s + 0.5;
                o.uv.y = uv.x * s + uv.y * c + 0.5;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float t = _Time.y * _GlobalSpeed;

                float2 uv1 = (i.uv - 0.5) / float2(max(_Layer1ScaleX, 0.0001), max(_Layer1ScaleY, 0.0001)) + 0.5;
                uv1 += float2(_Layer1MoveX, _Layer1MoveY) * _Layer1Speed * t;

                float2 uv2 = (i.uv - 0.5) / float2(max(_Layer2ScaleX, 0.0001), max(_Layer2ScaleY, 0.0001)) + 0.5;
                uv2 += float2(_Layer2MoveX, _Layer2MoveY) * _Layer2Speed * t;

                fixed4 layer1 = tex2D(_MainTex, frac(uv1));
                fixed4 layer2 = tex2D(_MainTex, frac(uv2));

                layer1.a *= _Layer1Alpha;
                layer2.a *= _Layer2Alpha;

                fixed4 col;

                // 일반적인 투명 이미지 겹침.
                // 한쪽을 지우거나 선택하지 않음.
                col.a = layer1.a + layer2.a * (1.0 - layer1.a);
                col.rgb = lerp(layer2.rgb, layer1.rgb, layer1.a);

                // UI Image Color 반영
                col *= i.color;
                col.a *= _OverallAlpha;

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