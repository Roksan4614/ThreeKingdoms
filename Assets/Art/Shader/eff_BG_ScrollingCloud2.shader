Shader "Custom/FX/eff_BG_ScrollingCloud2"
{
    Properties
    {
        _MainTex            ("Sprite Texture", 2D) = "white" {}

        // Scroll
        _ScrollSpeed        ("Scroll Speed (XY)", Vector) = (0.2, 0.0, 0, 0)

        // Distortion
        _DistortTex         ("Distortion Map (RG)", 2D) = "gray" {}
        _DistortStrength    ("Distort Strength", Range(0,1)) = 0.05
        _DistortTiling      ("Distort Tiling (XY)", Vector) = (1,1,0,0)
        _DistortScrollSpeed ("Distort Scroll Speed (XY)", Vector) = (0.1, 0.0, 0, 0)

        // Dome Effect
        _DomeStrength       ("Dome Strength (±)", Float) = 0.5
        _DomeCenter         ("Dome Center (UV)", Vector) = (0.5,0.5,0,0)
        _DomeScale          ("Dome Scale (radius)", Float) = 1.0   // ← 추가: 스케일로 영향 범위 조절

        // Edge Fade Mask (fixed, per-sprite)
        _EdgeFade           ("Edge Fade (L,R,B,T)", Vector) = (0.05,0.05,0.05,0.05)

        _AlphaCutoff        ("Alpha Cutoff", Range(0,1)) = 0.0

        // Brightness (Mul)
        _BrightMul          ("Brightness Multiply", Range(0,4)) = 1.0   // ← 추가: RGB 곱 연산으로 밝기 조절
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Sprite" "CanUseSpriteAtlas"="True" }
        LOD 100
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #pragma multi_compile _ PIXELSNAP_ON
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4   _MainTex_ST;
            float4   _ScrollSpeed;

            sampler2D _DistortTex;
            float4    _DistortTiling;
            float4    _DistortScrollSpeed;
            float     _DistortStrength;

            float     _DomeStrength;
            float4    _DomeCenter;
            float     _DomeScale;     // ← 추가

            float4    _EdgeFade;
            float     _AlphaCutoff;

            float     _BrightMul;     // ← 추가

            struct appdata_t { float4 vertex:POSITION; float2 texcoord:TEXCOORD0; fixed4 color:COLOR; };
            struct v2f     { float4 vertex:SV_POSITION; float2 atlasUV:TEXCOORD0; fixed4 color:COLOR; };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.atlasUV = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color   = v.color; // Tint 제거
                #ifdef PIXELSNAP_ON
                    o.vertex = UnityPixelSnap(o.vertex);
                #endif
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 1) Atlas → Local UV (0~1)
                float2 rectMin  = _MainTex_ST.zw;
                float2 rectSize = _MainTex_ST.xy;
                float2 localUV0 = (i.atlasUV - rectMin) / rectSize;

                // 2) Dome Effect: power-based warp + scale (범위를 넓혀 고르게)
                float2 center = _DomeCenter.xy;
                float2 dc     = localUV0 - center;
                float  r      = length(dc);

                // 스케일로 정규화 → 지수적 왜곡 → 다시 스케일 복원
                float  s      = max(_DomeScale, 1e-4);     // 안전 처리
                float  rN     = r / s;                     // 스케일로 반경을 넓히거나 줄임
                float  exponent = 1.0 + _DomeStrength;     // 기존 로직 유지
                float  rpN    = (rN > 0) ? pow(saturate(rN), exponent) : 0;
                float  rp     = rpN * s;

                float2 dir    = (r > 0) ? (dc / r) : float2(0,0);
                float2 localUV1 = center + dir * rp;

                // 3) Distortion
                float2 dUV     = localUV1 * _DistortTiling.xy + _Time.y * _DistortScrollSpeed.xy;
                float2 noise   = tex2D(_DistortTex, dUV).rg * 2 - 1;
                float2 distort = noise * _DistortStrength;

                // 4) Scroll + Distort → final UV
                float2 offset  = _Time.y * _ScrollSpeed.xy;
                float2 localUVs = frac(localUV1 + offset + distort);
                float2 finalUV = rectMin + localUVs * rectSize;

                // 5) Edge Fade Mask (per-sprite fixed)
                float fadeL = (_EdgeFade.x > 0) ? smoothstep(0, _EdgeFade.x, localUV0.x)       : 1.0;
                float fadeR = (_EdgeFade.y > 0) ? smoothstep(0, _EdgeFade.y, 1.0 - localUV0.x) : 1.0;
                float fadeB = (_EdgeFade.z > 0) ? smoothstep(0, _EdgeFade.z, localUV0.y)       : 1.0;
                float fadeT = (_EdgeFade.w > 0) ? smoothstep(0, _EdgeFade.w, 1.0 - localUV0.y) : 1.0;
                float edgeMask = min(min(fadeL, fadeR), min(fadeB, fadeT));

                // 6) Sample & output
                fixed4 col = tex2D(_MainTex, finalUV) * i.color;

                // 밝기 곱 (RGB만)
                col.rgb *= _BrightMul;

                col.a *= edgeMask;
                if (_AlphaCutoff > 0 && col.a < _AlphaCutoff) discard;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

