Shader "Custom/FX/FX_LineNoise_FrameSequence"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Jitter)]
        _JitterStrength ("Jitter Strength", Float) = 1
        _NoiseScale ("Noise Scale", Float) = 80
        _FrameRate ("Frame Rate", Float) = 8

        [Header(Edge Only)]
        _EdgeOnly ("Edge Only 0 Off 1 On", Float) = 0
        _EdgeWidth ("Edge Width", Float) = 1.5

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

            float _JitterStrength;
            float _NoiseScale;
            float _FrameRate;

            float _EdgeOnly;
            float _EdgeWidth;

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

            float hash11(float p)
            {
                p = frac(p * 0.1031);
                p *= p + 33.33;
                p *= p + p;
                return frac(p);
            }

            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(a, b, u.x),
                    lerp(c, d, u.x),
                    u.y
                );
            }

            float2 GetFrameOffset(float frame, float seed)
            {
                float x = hash11(frame + seed);
                float y = hash11(frame + seed + 17.37);

                return float2(x, y) * 1000.0;
            }

            float2 GetJitter(float2 uv, float frame)
            {
                float2 p = uv * _NoiseScale;

                float2 offsetX = GetFrameOffset(frame, 3.11);
                float2 offsetY = GetFrameOffset(frame, 19.73);

                float nx = noise(p + offsetX);
                float ny = noise(p + offsetY);

                float strength = _JitterStrength * 0.001;

                return float2(nx - 0.5, ny - 0.5) * strength;
            }

            float GetEdgeMask(float2 uv)
            {
                float2 texel = _MainTex_TexelSize.xy * _EdgeWidth;

                float center = tex2D(_MainTex, uv).a;

                float left  = tex2D(_MainTex, uv + float2(-texel.x, 0)).a;
                float right = tex2D(_MainTex, uv + float2( texel.x, 0)).a;
                float down  = tex2D(_MainTex, uv + float2(0, -texel.y)).a;
                float up    = tex2D(_MainTex, uv + float2(0,  texel.y)).a;

                float edge = 0.0;
                edge += abs(center - left);
                edge += abs(center - right);
                edge += abs(center - down);
                edge += abs(center - up);

                return saturate(edge);
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
                float frameRate = max(_FrameRate, 0.001);

                float rawFrame = floor(_Time.y * frameRate);

                // frame 값이 계속 커지면서 노이즈 좌표 정밀도 문제가 생기는 것을 방지
                float frame = fmod(rawFrame, 256.0);

                float2 jitter = GetJitter(i.uv, frame);

                if (_EdgeOnly > 0.5)
                {
                    float edgeMask = GetEdgeMask(i.uv);
                    jitter *= edgeMask;
                }

                fixed4 col = tex2D(_MainTex, i.uv + jitter);
                col *= i.color;

                return col;
            }

            ENDCG
        }
    }
}