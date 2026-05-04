Shader "Custom/FX/eff_BG_ScrollingCloud"
{
    Properties
    {
        _MainTex ("Cloud Texture", 2D) = "white" {}
        _Color ("Tint Color", Color) = (1,1,1,1)

        _Speed1 ("Forward X Scroll Speed", Float) = 0.02
        _Speed2 ("Reverse X Scroll Speed", Float) = 0.02

        _YSpeed1 ("Forward Y Oscillation Speed", Float) = 1.0
        _YSpeed2 ("Reverse Y Oscillation Speed", Float) = 1.0

        _YAmp1 ("Forward Y Amplitude", Float) = 0.02
        _YAmp2 ("Reverse Y Amplitude", Float) = 0.02
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;

            float _Speed1, _Speed2;
            float _YSpeed1, _YSpeed2;
            float _YAmp1, _YAmp2;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float safeFmod(float value, float range)
            {
                return value - range * floor(value / range);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y;

                float2 uv1;
                uv1.x = safeFmod(i.uv.x + time * _Speed1, 1.0);
                uv1.y = i.uv.y + sin(time * _YSpeed1) * _YAmp1;

                float2 mirrored = float2(1.0 - i.uv.x, i.uv.y);
                float2 uv2;
                uv2.x = safeFmod(mirrored.x + time * _Speed2, 1.0);
                uv2.y = mirrored.y + sin(time * _YSpeed2) * _YAmp2;

                fixed4 col1 = tex2D(_MainTex, uv1);
                fixed4 col2 = tex2D(_MainTex, uv2);

                // 단순 평균 처리 (발광 방지)
                fixed4 finalCol = (col1 + col2) * 0.5;

                // SpriteRenderer 색상 및 투명도 적용
                finalCol *= _Color;

                return finalCol;
            }

            ENDCG
        }
    }
}
