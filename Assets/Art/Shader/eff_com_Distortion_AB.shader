Shader "Custom/FX/eff_com_Distortion_AB"
{
    Properties
    {
        _MainTex ("Aura Texture", 2D) = "white" {}
        _NoiseTex ("Distortion Noise Texture", 2D) = "gray" {}
        _MaskTex ("Edge Mask Texture", 2D) = "white" {}

        _Color ("Tint Color", Color) = (1,1,1,1)

        _MainScrollSpeed ("Main Texture Scroll Speed", Float) = 1.0
        _MainScrollDirection ("Main Texture Scroll Direction", Vector) = (1, 0, 0, 0)

        _DistortionStrength ("Distortion Strength", Float) = 0.1
        _DistortionScrollSpeed ("Distortion Scroll Speed", Float) = 1.0
        _DistortionScrollDirection ("Distortion Scroll Direction", Vector) = (1, 0, 0, 0)
        _NoiseTiling ("Noise Tiling", Float) = 1.0
        _DistortionEdgeFade ("Distortion Edge Fade", Float) = 0.1

        _Brightness ("Brightness", Float) = 1.0
        _AlphaThreshold ("Black Cutoff Threshold", Float) = 0.1
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            sampler2D _MaskTex;

            float4 _MainTex_ST;
            float4 _Color;

            float _MainScrollSpeed;
            float2 _MainScrollDirection;

            float _DistortionStrength;
            float _DistortionScrollSpeed;
            float2 _DistortionScrollDirection;
            float _NoiseTiling;
            float _DistortionEdgeFade;

            float _Brightness;
            float _AlphaThreshold;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 mainScrollOffset = _Time.y * _MainScrollSpeed * normalize(_MainScrollDirection);
                float2 mainUV = i.uv + mainScrollOffset;

                float2 noiseScroll = _Time.y * _DistortionScrollSpeed * normalize(_DistortionScrollDirection);
                float2 noiseUV = frac(i.uv * _NoiseTiling + noiseScroll);
                float2 noise = tex2D(_NoiseTex, noiseUV).rg * 2.0 - 1.0;

                // ✅ UV 경계 마스크 계산
                float edgeFadeU = smoothstep(0.0, _DistortionEdgeFade, i.uv.x) * smoothstep(1.0, 1.0 - _DistortionEdgeFade, i.uv.x);
                float edgeFadeV = smoothstep(0.0, _DistortionEdgeFade, i.uv.y) * smoothstep(1.0, 1.0 - _DistortionEdgeFade, i.uv.y);
                float edgeFade = edgeFadeU * edgeFadeV;

                // ✅ 왜곡 세기에 마스크 적용
                float2 distortedUV = mainUV + noise * _DistortionStrength * edgeFade;

                fixed4 texColor = tex2D(_MainTex, distortedUV);
                fixed4 maskColor = tex2D(_MaskTex, i.uv);
                fixed4 tint = _Color * i.color;

                float luminance = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                float baseAlpha = saturate((luminance - _AlphaThreshold) / (1.0 - _AlphaThreshold));
                float finalAlpha = baseAlpha * tint.a * maskColor.r;

                fixed3 rgb = texColor.rgb * tint.rgb * _Brightness;
                rgb *= finalAlpha;

                return fixed4(rgb, finalAlpha);
            }
            ENDCG
        }
    }
}
