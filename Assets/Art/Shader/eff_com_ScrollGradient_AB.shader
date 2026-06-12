Shader "Custom/FX/eff_com_ScrollGradient_AB"
{
    Properties
    {
        _AdditivePower ("Additive Power", Float) = 1.0
        _Opacity("Opacity", Float) = 1.0
        _MainTex ("Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0.5, 0.5, 0, 0)
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
            #include "UnityUI.cginc"

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
                float2 uvGrad : TEXCOORD1;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float _AdditivePower;
            float _Opacity;
            float4 _MainTex_ST;
            float4 _ScrollSpeed;

            sampler2D _GradientTex;
            float _GradientPower;
            float4 _GradientTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.position = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex); 
                o.uvGrad = TRANSFORM_TEX(v.texcoord, _GradientTex); 
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 offset = _ScrollSpeed.xy * _Time.y;
                fixed4 texColor = tex2D(_MainTex, i.uv + offset);
                texColor *= i.color;
                texColor.rgb *= _AdditivePower;
                float grad = pow(tex2D(_GradientTex, i.uvGrad).r, _GradientPower);
                texColor.a = texColor.a * grad * _Opacity;

                return texColor;
            }
            ENDCG
        }
    }
    FallBack "UI/Default"
}
