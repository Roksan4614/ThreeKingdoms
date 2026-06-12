Shader "Custom/FX/eff_com_DissolveOffset_AB"
{
    Properties {
        _AdditivePower ("Additive Power", Float) = 1.0
        _MainTex ("Texture (Mask Only)", 2D) = "white" {}
        [Toggle] _StepOrSubtract ("Step Or Subtract", Float) = 0
        _TintColor ("Tint Color", Color) = (1,1,1,1)
    }

    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                fixed4 color  : COLOR;
                float4 uv     : TEXCOORD0;
                float4 uv2    : TEXCOORD1; // Custom1.xyzw (x=dissolve, y=U off, z=V off)
            };

            struct v2f {
                float2 uv          : TEXCOORD0;
                fixed4 color       : COLOR;
                float4 vertex      : SV_POSITION;
                float4 customData1 : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float  _StepOrSubtract;
            float  _AdditivePower;
            fixed4 _TintColor;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv.xy, _MainTex);
                o.customData1 = v.uv2;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                float2 uv = i.uv + float2(i.customData1.y, i.customData1.z);

                fixed4 texCol = tex2D(_MainTex, uv);

                float dissolveThreshold = i.customData1.x;
                float dissolveAlpha = (_StepOrSubtract == 0)
                    ? ceil(texCol.g - dissolveThreshold)     // step
                    : max(0, texCol.g - dissolveThreshold);  // subtract

                float outA = dissolveAlpha * texCol.r * i.color.a * _TintColor.a * _AdditivePower;
                float3 outRGB = i.color.rgb * _TintColor.rgb * _AdditivePower;

                return fixed4(outRGB, outA);
            }
            ENDCG
        }
    }
}
