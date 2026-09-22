Shader "Custom/FX/eff_com_InkRing"
{
 Properties
 {
  _MainTex ("Start Ring (Alpha)", 2D) = "white" {}
  [Toggle(_END_RING)] _UseEndRing ("Use End Ring (2 Samples)", Float) = 1
  _EndTex ("End Ring (Alpha)", 2D) = "white" {}
  _SourceRadius ("Start Texture Ring Radius", Range(0.05,0.49)) = 0.379
  _EndSourceRadius ("End Texture Ring Radius", Range(0.05,0.49)) = 0.344
  _AlphaGain ("Start Alpha Gain", Range(0,5)) = 1
  _EndAlphaGain ("End Alpha Gain", Range(0,5)) = 3.2
  _EndWidth ("End Texture Width Adjustment", Range(0.25,3)) = 1
  [HDR] _Tint ("Body Color / Opacity", Color) = (0.1,0.45,0.04,1)
  [HDR] _EdgeColor ("Inner Color", Color) = (0.01,0.025,0.005,1)
  _GradientWidth ("Radial Color Gradient Width (UV)", Range(0.01,0.3)) = 0.12
  _Radius ("Initial Ring Radius (UV)", Range(0.05,0.45)) = 0.34
  _EndRadius ("Final Ring Radius (UV)", Range(0,0.3)) = 0.14
  _Width ("Base Width Multiplier", Range(0.25,3)) = 1
  _MidStretch ("Mid Progress Extra Width", Range(0,2)) = 0.6
  _RoundStart ("End Texture Blend Starts", Range(0,0.9)) = 0.5
  _CollapsePower ("Contraction Curve", Range(0.3,3)) = 0.8
  _CenterFade ("Center Absorption Radius (UV)", Range(0.001,0.1)) = 0.02
  [Toggle] _ShapePreview ("Shape Preview (Ignore Lifetime Fade)", Float) = 0
  [Enum(Manual,0,ParticleCustom1X,1)] _ProgressSource ("Progress Source", Float) = 1
  _Progress ("Manual Progress", Range(0,1)) = 0
  [Enum(TEXCOORD0_Z,0,TEXCOORD0_W,1,TEXCOORD1_X,2,TEXCOORD1_Y,3,TEXCOORD1_Z,4,TEXCOORD1_W,5)] _CustomDataSlot ("Custom1.x Stream Location", Float) = 0
 }
 SubShader
 {
  Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
  Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
  Cull Off
  ZWrite Off
  Pass
  {
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #pragma shader_feature_local _END_RING
   #include "UnityCG.cginc"
   sampler2D _MainTex,_EndTex;
   float4 _Tint,_EdgeColor;
   float _SourceRadius,_EndSourceRadius,_AlphaGain,_EndAlphaGain,_EndWidth;
   float _GradientWidth,_Radius,_EndRadius,_Width,_MidStretch,_RoundStart,_CollapsePower,_CenterFade;
   float _ShapePreview,_ProgressSource,_Progress,_CustomDataSlot;
   struct appdata { float4 vertex:POSITION; float4 color:COLOR; float4 uv0:TEXCOORD0; float4 uv1:TEXCOORD1; };
   struct v2f { float4 vertex:SV_POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; float3 shape:TEXCOORD1; float2 phase:TEXCOORD2; };
   v2f vert(appdata v)
   {
    v2f o; o.vertex=UnityObjectToClipPos(v.vertex);o.color=v.color;o.uv=v.uv0.xy;
    float c;
    if(_CustomDataSlot<0.5)c=v.uv0.z;else if(_CustomDataSlot<1.5)c=v.uv0.w;
    else if(_CustomDataSlot<2.5)c=v.uv1.x;else if(_CustomDataSlot<3.5)c=v.uv1.y;
    else if(_CustomDataSlot<4.5)c=v.uv1.z;else c=v.uv1.w;
    float p=saturate(lerp(_Progress,c,step(0.5,_ProgressSource)));
    float mid=4*p*(1-p);mid*=mid;
    o.shape.x=lerp(min(_EndRadius,_Radius),_Radius,pow(1-p,_CollapsePower));
    o.shape.y=rcp(max(0.001,_Width*(1+_MidStretch*mid)));
    o.shape.z=o.shape.y/max(0.001,_EndWidth);
    o.phase.x=smoothstep(_RoundStart,1,p);
    float life=smoothstep(0,0.05,p)*(1-smoothstep(0.85,1,p));
    o.phase.y=lerp(life,1,step(0.5,_ShapePreview));return o;
   }
   float inBounds(float2 uv,float sr)
   {
    return step(0,sr)*step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
   }
   float4 frag(v2f i):SV_Target
   {
    float2 pos=i.uv-0.5;
    float r=length(pos);
    float2 direction=pos/max(r,0.00001);
    // Translate the ring radially. dr_source/dr_screen is independent of radius.
    // No atan2, procedural noise, loops, neighbor samples, or polar repetition.
    float delta=r-i.shape.x;
    float sr=_SourceRadius+delta*i.shape.y;
    float2 uv=0.5+direction*max(0,sr);
    float a=saturate(tex2D(_MainTex,uv).a*_AlphaGain)*inBounds(uv,sr);
    #if defined(_END_RING)
     float er=_EndSourceRadius+delta*i.shape.z;
     float2 euv=0.5+direction*max(0,er);
     float b=saturate(tex2D(_EndTex,euv).a*_EndAlphaGain)*inBounds(euv,er);
     a=lerp(a,b,i.phase.x);
    #endif
    float grad=smoothstep(-_GradientWidth*0.5,_GradientWidth*0.5,delta);
    float3 rgb=lerp(_EdgeColor.rgb,_Tint.rgb,grad)*i.color.rgb;
    float alpha=a*smoothstep(0,_CenterFade,r)*i.phase.y*saturate(_Tint.a)*saturate(i.color.a);
    return float4(rgb,alpha);
   }
   ENDCG
  }
 }
 Fallback Off
}

