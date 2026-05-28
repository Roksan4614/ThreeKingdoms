Shader "Custom/FX/eff_BG_Sky"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _ScrollSpeed        ("Scroll Speed (XY)", Vector) = (0.2, 0.0, 0, 0)
        _DistortTex         ("Distortion Map (RG)", 2D) = "gray" {}
        _DistortStrength    ("Distort Strength", Range(0,1)) = 0.05
        _DistortTiling      ("Distort Tiling (XY)", Vector) = (1,1,0,0)
        _DistortScrollSpeed ("Distort Scroll Speed (XY)", Vector) = (0.1, 0.0, 0, 0)
        _DomeStrength       ("Dome Strength (±)", Float) = 0.5
        _DomeCenter         ("Dome Center (UV)", Vector) = (0.5,0.5,0,0)
        _DomeScale          ("Dome Scale (radius)", Float) = 1.0
        _EdgeFade           ("Edge Fade (L,R,B,T)", Vector) = (0.05,0.05,0.05,0.05)
        _AlphaCutoff        ("Alpha Cutoff", Range(0,1)) = 0.0
        _BrightMul          ("Brightness Multiply", Range(0,4)) = 1.0

        // UI 마스킹 필수 프로퍼티 (이름이 정확해야 합니다)
        _ClipRect           ("Clip Rect", Vector) = (-32767, -32767, 32767, 32767)
        _ColorMask          ("Color Mask", Float) = 15
        
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
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

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 atlasUV  : TEXCOORD0;
                fixed4 color    : COLOR;
                float4 worldPosition : TEXCOORD1; // RectMask2D 핵심 좌표
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ScrollSpeed;
            sampler2D _DistortTex;
            float4 _DistortTiling, _DistortScrollSpeed;
            float _DistortStrength, _DomeStrength, _DomeScale, _BrightMul, _AlphaCutoff;
            float4 _DomeCenter, _EdgeFade, _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                // [중요] UI Image 컴포넌트에서는 v.vertex를 그대로 worldPosition에 할당하는 것이
                // 유니티 표준 UI 마스킹 방식입니다. CanvasRenderer가 이미 필요한 계산을 마쳤기 때문입니다.
                o.worldPosition = v.vertex;
                
                o.vertex = UnityObjectToClipPos(o.worldPosition);
                o.atlasUV = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) Atlas -> Local UV (0~1)
                // UI Image가 Sprite Atlas를 사용할 때 필수적인 계산입니다.
                float2 rectMin = _MainTex_ST.zw;
                float2 rectSize = _MainTex_ST.xy;
                float2 localUV0 = (i.atlasUV - rectMin) / max(rectSize, 0.0001);

                // 2) Dome Effect
                float2 dc = localUV0 - _DomeCenter.xy;
                float r = length(dc);
                float s = max(_DomeScale, 1e-4);
                float rp = ((r/s > 0) ? pow(saturate(r/s), 1.0 + _DomeStrength) : 0) * s;
                float2 localUV1 = _DomeCenter.xy + ((r > 0) ? (dc / r) : 0) * rp;

                // 3) Distortion & Scroll
                float2 dUV = localUV1 * _DistortTiling.xy + _Time.y * _DistortScrollSpeed.xy;
                float2 noise = tex2D(_DistortTex, dUV).rg * 2 - 1;
                float2 localUVs = frac(localUV1 + (_Time.y * _ScrollSpeed.xy) + (noise * _DistortStrength));
                float2 finalUV = rectMin + localUVs * rectSize;

                // 4) Edge Fade
                float4 fade = smoothstep(0, max(0.001, _EdgeFade), float4(localUV0, 1.0 - localUV0));
                float edgeMask = fade.x * fade.y * fade.z * fade.w;

                // 5) Final Color
                fixed4 col = tex2D(_MainTex, finalUV) * i.color;
                col.rgb *= _BrightMul;
                col.a *= edgeMask;

                // 6) [최종 해결책] UnityUI 내장 클리핑 적용
                // RectMask2D가 활성화되면 유니티가 i.worldPosition과 _ClipRect를 비교하여 알파를 깎습니다.
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                #ifdef UNITY_UI_ALPHACLIP
                clip (col.a - 0.001);
                #endif

                if (_AlphaCutoff > 0 && col.a < _AlphaCutoff) discard;
                return col;
            }
            ENDCG
        }
    }
}