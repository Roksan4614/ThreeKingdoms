Shader "Custom/FX/eff_com_ScrollGradient_AB"
{
    Properties
    {
        _AdditivePower ("Additive Power", Float) = 1.0
        _Opacity ("Opacity", Float) = 1.0

        _MainTex ("Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed", Vector) = (0.5, 0.5, 0, 0)

        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientPower ("Gradient Power", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                float4 color    : COLOR;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 uv       : TEXCOORD0;
                float2 uvGrad   : TEXCOORD1;
                float4 color    : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _AdditivePower;
            float _Opacity;
            float4 _ScrollSpeed;

            sampler2D _GradientTex;
            float4 _GradientTex_ST;
            float _GradientPower;

            v2f vert(appdata_t v)
            {
                v2f o;

                o.position = UnityObjectToClipPos(v.vertex);

                float2 baseUV = TRANSFORM_TEX(v.texcoord, _MainTex);

                // 시간이 증가해도 오프셋 자체가 큰 숫자가 되지 않도록
                // 스크롤 위치를 0~1 범위로 반복시킨다.
                float2 scrollPhase = frac(_ScrollSpeed.xy * _Time.y);

                // 큰 시간값을 원본 UV에 직접 더하지 않는다.
                o.uv = baseUV + scrollPhase;

                o.uvGrad = TRANSFORM_TEX(v.texcoord, _GradientTex);
                o.color = v.color;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half4 texColor = tex2D(_MainTex, i.uv);

                texColor *= half4(i.color);
                texColor.rgb *= _AdditivePower;

                float gradientSample =
                    saturate(tex2D(_GradientTex, i.uvGrad).r);

                float gradient =
                    pow(gradientSample, _GradientPower);

                texColor.a *= gradient * _Opacity;

                return texColor;
            }

            ENDCG
        }
    }

    FallBack "UI/Default"
}