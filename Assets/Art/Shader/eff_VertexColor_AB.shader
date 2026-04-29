Shader "Custom/FX/eff_VertexColor_AB"
{
    Properties
    {
        _TintColor("Tint Color", Color) = (1,1,1,1)
        _AdditivePower ("Additive Power", Float) = 1.0
        _Opacity ("Opacity", Float) = 1.0
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _AdditivePower;
            float _Opacity;
            float4 _MainTex_ST;
            float4 _TintColor;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);
                texColor *= i.color * _TintColor;
                texColor.a *= _Opacity;
                texColor.rgb *= _AdditivePower;
                return texColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
