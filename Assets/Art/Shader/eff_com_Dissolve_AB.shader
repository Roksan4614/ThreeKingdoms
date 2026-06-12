Shader "Custom/FX/eff_com_Dissolve_AB"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Power ("Color Emphasis Power", Float) = 2.0
        _NoiseScale ("Noise UV Scale", Float) = 10.0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float _Power;
            float _NoiseScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 custom1 : TEXCOORD2; // Custom1.xyzw from particle
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float4 custom1 : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                o.custom1 = v.custom1;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 텍스처 색상
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // 컬러 강조: 밝은 영역만 더 강조하는 방식
                float lum = dot(texColor.rgb, float3(0.299, 0.587, 0.114));
                texColor.rgb += lum * (_Power - 1.0);

                // 노이즈 마스크 처리
                float2 noiseUV = i.uv * _NoiseScale;
                float noise = tex2D(_NoiseTex, noiseUV).r;

                // 디졸브: Custom1.x 값 기반
                float dissolve = step(noise, i.custom1.x);

                // 파티클 컬러 적용 및 알파 조절
                fixed4 finalColor = texColor * i.color;
                finalColor.a *= dissolve;

                return finalColor;
            }
            ENDCG
        }
    }
}