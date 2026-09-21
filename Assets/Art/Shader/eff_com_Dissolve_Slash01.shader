Shader "Custom/FX/eff_com_Dissolve_Slash01"
{
    Properties
    {
        [HDR] _Tint ("Head Color / Opacity", Color) = (1,0.52,0.64,1)
        [HDR] _TailColor ("Tail Color", Color) = (0.27,0.23,0.65,1)
        [HDR] _RimColor ("Bright Edge Color", Color) = (1.6,1.3,0.75,1)
        _MainTex ("REQUIRED Silhouette Texture (Alpha)", 2D) = "white" {}
        _AlphaCutoff ("Silhouette Threshold", Range(0.01,0.99)) = 0.5
        _RimPixels ("Bright Edge Width (Texture Pixels)", Range(0,16)) = 5
        _RimStart ("Bright Edge Starts Along Arc", Range(0,0.95)) = 0.32
        _ColorSteps ("Color Bands", Range(2,16)) = 5
        _BandBlend ("Smooth Color / Banded Color", Range(0,1)) = 0.65
        _NoiseTex ("Dissolve Noise (R)", 2D) = "gray" {}
        _NoiseScale ("Noise Tiling (Along, Across)", Vector) = (3,1,0,0)
        _Softness ("Dissolve Softness", Range(0.001,0.2)) = 0.008
        _EdgeWidth ("Bright Dissolve Edge", Range(0,0.2)) = 0.025
        _Directional ("Directional Dissolve", Range(0,1)) = 0.65
        [Toggle] _UseCustomData ("Use Particle Custom1.x", Float) = 1
        [Enum(TEXCOORD0_Z,0,TEXCOORD0_W,1,TEXCOORD1_X,2,TEXCOORD1_Y,3,TEXCOORD1_Z,4,TEXCOORD1_W,5)] _CustomDataSlot ("Custom1.x Stream Location", Float) = 0
        [Toggle] _DebugCustomData ("Debug Input (0 Red - 1 Green)", Float) = 0
        [Toggle] _DebugParticleAlpha ("Debug Particle Alpha (Black 0 - White 1)", Float) = 0
        _Dissolve ("Manual Dissolve (Custom Data Off)", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex, _NoiseTex;
            float4 _MainTex_ST, _MainTex_TexelSize, _NoiseScale;
            float4 _Tint, _TailColor, _RimColor;
            float _Softness, _EdgeWidth, _Directional;
            float _AlphaCutoff, _RimPixels, _RimStart, _ColorSteps, _BandBlend;
            float _UseCustomData, _Dissolve, _CustomDataSlot, _DebugCustomData;
            // Select the semantic shown next to Custom1.x in Renderer > Custom Vertex Streams.
            // Position, Color, UV, Custom1X normally packs the scalar into TEXCOORD0.z.
            // Additional UV/animation streams can move that scalar. Do not guess from its name.
            float _DebugParticleAlpha;
            struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 texcoord0:TEXCOORD0; float4 texcoord1:TEXCOORD1; };
            struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float dissolve:TEXCOORD1; float customInput:TEXCOORD2; };
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.texcoord0.xy;
                float customValue;
                if (_CustomDataSlot < 0.5) customValue = v.texcoord0.z;
                else if (_CustomDataSlot < 1.5) customValue = v.texcoord0.w;
                else if (_CustomDataSlot < 2.5) customValue = v.texcoord1.x;
                else if (_CustomDataSlot < 3.5) customValue = v.texcoord1.y;
                else if (_CustomDataSlot < 4.5) customValue = v.texcoord1.z;
                else customValue = v.texcoord1.w;
                o.customInput = saturate(customValue);
                o.dissolve = saturate(lerp(_Dissolve, customValue, step(0.5, _UseCustomData)));
                return o;
            }
            float4 frag(v2f i):SV_Target
            {
                // Diagnostic only: show the incoming particle alpha as opaque grayscale.
                // Never use this mode for the finished effect.
                float particleAlpha = saturate(i.color.a);
                if (_DebugParticleAlpha > 0.5)
                    return float4(particleAlpha, particleAlpha, particleAlpha, 1);
                float2 uv = i.uv;
                float noise = tex2D(_NoiseTex, uv * _NoiseScale.xy).r;
                float field = saturate(lerp(noise, uv.x, _Directional));
                // Threshold extends beyond [0,1], guaranteeing full coverage at 0
                // and fully invisible output at 1 even with noise extrema.
                float threshold = lerp(-_Softness, 1.0 + _Softness, i.dissolve);
                float distanceToCut = field - threshold;
                float dissolveAlpha = smoothstep(-_Softness, _Softness, distanceToCut);
                float edge = 1.0 - smoothstep(0, max(0.001, _EdgeWidth), distanceToCut);
                edge *= smoothstep(0, 0.05, i.dissolve);
                // The texture defines the whole silhouette, including large cutouts.
                // No mesh-distance feather, dark outline or radial tube shading.
                float2 maskUV = TRANSFORM_TEX(uv, _MainTex);
                float alpha = tex2D(_MainTex, maskUV).a;
                float aa = max(fwidth(alpha), 0.001);
                float silhouette = smoothstep(_AlphaCutoff-aa, _AlphaCutoff+aa, alpha);
                float2 offset = float2(0, _MainTex_TexelSize.y * _RimPixels);
                float outwardAlpha = tex2D(_MainTex, maskUV + offset).a;
                float brightRim = saturate((alpha-outwardAlpha) * 2);
                brightRim *= smoothstep(_RimStart, 1, uv.x);
                float ramp = saturate(uv.x * 0.85 + uv.y * 0.15);
                float steps = max(2, floor(_ColorSteps));
                float banded = floor(ramp * (steps-1) + 0.5) / (steps-1);
                ramp = lerp(ramp, banded, _BandBlend);
                float3 rgb = lerp(_TailColor.rgb, _Tint.rgb, ramp);
                rgb = lerp(rgb, _RimColor.rgb, saturate(max(brightRim, edge)));
                // Shape thresholding occurs BEFORE particle opacity is multiplied.
                // Do not put particle alpha into smoothstep/clip: that causes popping.
                // HDR RGB remains separate; never threshold or brighten this opacity.
                float shapeAlpha = saturate(silhouette * dissolveAlpha);
                float finalAlpha = shapeAlpha * saturate(_Tint.a) * particleAlpha;
                float3 finalRGB = rgb * i.color.rgb;
                if (_DebugCustomData > 0.5)
                    finalRGB = float3(1.0-i.customInput, i.customInput, 0);
                return float4(finalRGB, finalAlpha);
            }
            ENDCG
        }
    }
    Fallback Off
}
