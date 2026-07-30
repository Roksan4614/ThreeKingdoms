Shader "Custom/FX/eff_UI_FlowingNoise"
{
    Properties
    {
        _MainTex        ("Main Texture", 2D) = "white" {}
        _Color          ("Tint", Color) = (1,1,1,1)

        // 흐르는 레이어용 노이즈 텍스처
        _NoiseTex       ("Noise Texture", 2D) = "gray" {}
        _NoiseStrength  ("Noise Strength", Range(0,1)) = 0.3

        // 픽셀 기준 타일링(게이지 크기와 무관한 패턴 유지)
        _NoisePPU       ("Pixels Per Noise Tile", Float) = 64
        _NoiseScale     ("Extra Noise Scale", Float) = 1.0
        _ScrollSpeed    ("Scroll Speed", Float) = 1.0
        _ScrollDir      ("Scroll Direction (XY)", Vector) = (1,0,0,0)

        // ==== Distortion 전용 텍스처/파라미터 ====
        _DistortTex     ("Distortion Texture", 2D) = "gray" {}
        _DistortStrength("Distort Strength", Float) = 0.01
        _DistortScale   ("Distort UV Scale", Float) = 1.0
        _DistortSpeed   ("Distort Scroll Speed", Float) = 0.5
        _DistortDir     ("Distort Scroll Dir (XY)", Vector) = (0.7,0.3,0,0)

        _PreviewNoClip  ("(Editor) Ignore ClipRect", Float) = 0
    }

    SubShader
    {
        Tags{
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;    float4 _MainTex_ST;
            sampler2D _NoiseTex;   float4 _NoiseTex_ST;
            sampler2D _DistortTex; float4 _DistortTex_ST;

            fixed4 _Color;
            float  _NoiseStrength;
            float  _NoisePPU;
            float  _NoiseScale;
            float  _ScrollSpeed;
            float4 _ScrollDir;

            float  _DistortStrength;
            float  _DistortScale;
            float  _DistortSpeed;
            float4 _DistortDir;

            float  _PreviewNoClip;

            #ifdef UNITY_UI_CLIP_RECT
            uniform float4 _ClipRect; // (xmin,ymin,xmax,ymax)
            #endif

            struct appdata_t
            {
                float4 vertex   : POSITION;   // local rect space (px)
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex    : SV_POSITION;
                float2 uvMain    : TEXCOORD0;
                float2 localPx   : TEXCOORD1;
                fixed4 color     : COLOR;
                #ifdef UNITY_UI_CLIP_RECT
                float2 worldPos  : TEXCOORD2;
                #endif
            };

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex  = UnityObjectToClipPos(v.vertex);
                o.uvMain  = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.localPx = v.vertex.xy;
                o.color   = v.color * _Color;

                #ifdef UNITY_UI_CLIP_RECT
                float3 world = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldPos = world.xy;
                #endif
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 1) 메인 텍스처는 그대로
                fixed4 col = tex2D(_MainTex, i.uvMain) * i.color;

                // 2) Noise UV (픽셀 기준 + 스크롤)
                float2 baseNoiseUV = (i.localPx / max(_NoisePPU, 1e-5)) * _NoiseScale;
                float2 flowScroll  = _ScrollDir.xy * (_Time.y * _ScrollSpeed);

                // 3) Distortion UV 계산
                float2 noiseUV = baseNoiseUV;
                if (_DistortStrength != 0.0)
                {
                    float2 dScroll = _DistortDir.xy * (_Time.y * _DistortSpeed);
                    float2 dUV     = baseNoiseUV * _DistortScale + dScroll;

                    // DistortTex의 RG를 사용해 -1~1 범위 왜곡 벡터 생성
                    float2 dSample = tex2D(_DistortTex, TRANSFORM_TEX(dUV, _DistortTex)).rg * 2.0 - 1.0;
                    noiseUV += dSample * _DistortStrength;
                }

                // 최종 노이즈 샘플
                float2 uvFinal = TRANSFORM_TEX(noiseUV + flowScroll, _NoiseTex);
                float3 noiseRGB = tex2D(_NoiseTex, uvFinal).rgb;

                col.rgb += noiseRGB * _NoiseStrength;

                // 4) RectMask2D Clip 처리
                #ifdef UNITY_UI_CLIP_RECT
                    float clipFactor = 1.0;
                    #if defined(UNITY_EDITOR)
                        if (_PreviewNoClip < 0.5)
                        {
                            bool invalid = (_ClipRect.z <= _ClipRect.x) || (_ClipRect.w <= _ClipRect.y);
                            if (!invalid) clipFactor = UnityGet2DClipping(i.worldPos, _ClipRect);
                        }
                    #else
                        clipFactor = UnityGet2DClipping(i.worldPos, _ClipRect);
                    #endif
                    col.a *= clipFactor;
                    #ifdef UNITY_UI_ALPHACLIP
                        clip(col.a - 0.001);
                    #endif
                #endif

                return col;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
