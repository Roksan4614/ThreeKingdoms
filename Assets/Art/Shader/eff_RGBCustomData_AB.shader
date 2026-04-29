Shader "Custom/FX/eff_RGBCustomData_AB"
{
    Properties
    {
        _Opacity("Opacity", Float) = 1.0
        _AdditivePower ("Additive Power", Float) = 1.0
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientPower ("Gradient Power", Float) = 1.0
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
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
                float4 uv : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                float4 uv3 : TEXCOORD2;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float4 customData1 : TEXCOORD1;
                float4 customData2 : TEXCOORD2;
            };

            sampler2D _MainTex;
            float _AdditivePower; 
            float4 _MainTex_ST;
            float _Opacity;
            sampler2D _GradientTex;
            float _GradientPower;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.customData1 = v.uv2;
                o.customData2 = v.uv3;
                o.color = v.color;
                return o;
            }

fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv);

                float RChannelArea = texColor.r;
                float GChannelArea = texColor.g;
                float BChannelArea = texColor.b;

                float4 finalColor = i.color * RChannelArea + i.customData1 * GChannelArea + i.customData2 * BChannelArea;

                float grad = pow(tex2D(_GradientTex, i.uv).g, _GradientPower);

                finalColor.rgb *= _AdditivePower;
                finalColor.a = texColor.a * i.color.a * _Opacity * grad;

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
