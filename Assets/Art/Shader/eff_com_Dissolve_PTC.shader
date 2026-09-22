Shader "Custom/FX/eff_com_Dissolve_PTC"
{
 Properties
 {
  _MainTex ("Main Texture (RGBA)", 2D) = "white" {}
  [HDR] _Tint ("Tint / Opacity", Color) = (1,1,1,1)
  _NoiseTex ("Dissolve Noise (R)", 2D) = "gray" {}
  _NoiseScale ("Noise Scale (XY)", Vector) = (3,3,0,0)
  _NoiseStrength ("Noise Contrast", Range(0,3)) = 1
  _Softness ("Dissolve Edge Softness", Range(0.001,0.2)) = 0.035
  _Distortion ("Auto Distortion Amount (Noise UV)", Range(0,0.5)) = 0.08
  _DistortionFrequency ("Distortion Spatial Frequency", Range(0.1,10)) = 2
  _DistortionSpeed ("Distortion Speed", Range(0,5)) = 1
  _NoiseScroll ("Noise Scroll Speed (XY)", Vector) = (0.08,0.12,0,0)
  [Enum(Manual,0,ParticleCustom1X,1)] _ProgressSource ("Dissolve Progress Source", Float) = 1
  _Dissolve ("Manual Dissolve", Range(0,1)) = 0
  [Enum(TEXCOORD0_Z,0,TEXCOORD0_W,1,TEXCOORD1_X,2,TEXCOORD1_Y,3,TEXCOORD1_Z,4,TEXCOORD1_W,5)] _CustomDataSlot ("Custom1.x Stream Location", Float) = 0
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
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _MainTex,_NoiseTex;
   float4 _MainTex_ST,_NoiseTex_ST,_Tint,_NoiseScale,_NoiseScroll;
   float _NoiseStrength,_Softness,_Distortion,_DistortionFrequency,_DistortionSpeed;
   float _ProgressSource,_Dissolve,_CustomDataSlot;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 uv0:TEXCOORD0; float4 uv1:TEXCOORD1; };
   struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 mainUV:TEXCOORD0; float2 noiseUV:TEXCOORD1; float dissolve:TEXCOORD2; float2 phase:TEXCOORD3;
    float2 scrollOffset:TEXCOORD4;
   };
   v2f vert(appdata v)
   {
    v2f o;
    o.vertex=UnityObjectToClipPos(v.vertex);
    o.color=v.color;
    o.mainUV=TRANSFORM_TEX(v.uv0.xy,_MainTex);
    // Time work is per vertex. Distortion remains per pixel so a simple
    // four-vertex billboard still has a genuinely wavy noise mask.
    o.noiseUV=v.uv0.xy*_NoiseScale.xy*_NoiseTex_ST.xy+_NoiseTex_ST.zw;
    o.scrollOffset=frac(_Time.y*_NoiseScroll.xy);
    // Keep the trigonometric arguments bounded as well.
    o.phase=frac(float2(_Time.y*_DistortionSpeed,_Time.y*_DistortionSpeed*0.79+1.7)/6.2831853)*6.2831853;
    float c;
    if(_CustomDataSlot<0.5)c=v.uv0.z;else if(_CustomDataSlot<1.5)c=v.uv0.w;
    else if(_CustomDataSlot<2.5)c=v.uv1.x;else if(_CustomDataSlot<3.5)c=v.uv1.y;
    else if(_CustomDataSlot<4.5)c=v.uv1.z;else c=v.uv1.w;
    o.dissolve=saturate(lerp(_Dissolve,c,step(0.5,_ProgressSource)));
    return o;
   }
   float4 frag(v2f i):SV_Target
   {
    float2 wave=sin(i.noiseUV.yx*_DistortionFrequency*6.2831853+i.phase);
    // Distortion acts on the bounded local pattern, not accumulated scroll.
    float2 localUV=i.noiseUV+wave*_Distortion;
    // Offset is uniform across the particle; ordinary UV sampling is sufficient.
    float noise=tex2D(_NoiseTex,localUV+i.scrollOffset).r;
    noise=saturate((noise-0.5)*_NoiseStrength+0.5);
    // Extending the threshold guarantees no erosion at 0 and full removal at 1.
    float threshold=lerp(-_Softness,1+_Softness,i.dissolve);
    float mask=smoothstep(-_Softness,_Softness,noise-threshold);
    float4 main=tex2D(_MainTex,i.mainUV);
    float3 rgb=main.rgb*_Tint.rgb*i.color.rgb;
    // Particle opacity is multiplied AFTER dissolve, never thresholded.
    float alpha=main.a*saturate(_Tint.a)*saturate(i.color.a)*mask;
    return float4(rgb,alpha);
   }
   ENDCG
  }
 }
 Fallback Off
}
