Shader "Custom/FX/eff_com_RotationMasking"
{
    Properties
    {
        [NoScaleOffset]
        _MainTex ("Main Texture", 2D) = "white" {}

        [NoScaleOffset]
        _MaskTex ("Mask Gradient", 2D) = "white" {}

        [NoScaleOffset]
        _GradientTex ("Color Gradient", 2D) = "white" {}


        //------------------------------------------
        // Mask Transform
        //------------------------------------------

        // 0 = Center
        // +X = Right
        // -X = Left
        // +Y = Up
        // -Y = Down
        _MaskOffset ("Mask Offset XY", Vector) = (0, 0, 0, 0)

        // 1,1 = Original Size
        // > 1 = Larger
        // < 1 = Smaller
        //
        // X/Y 개별 조절 가능
        _MaskScale ("Mask Scale XY", Vector) = (1, 1, 0, 0)


        //------------------------------------------
        // Gradient
        //------------------------------------------

        _GradientOffset ("Gradient Offset XY", Vector) = (0, 0, 0, 0)


        //------------------------------------------
        // Main Rotation
        //------------------------------------------

        _RotationOffset ("Rotation Offset", Range(-360, 360)) = 0


        //------------------------------------------
        // Material Color
        //------------------------------------------

        [HDR]
        _Color ("Material Color", Color) = (1,1,1,1)
    }


    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        Blend SrcAlpha OneMinusSrcAlpha


        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"


            sampler2D _MainTex;
            sampler2D _MaskTex;
            sampler2D _GradientTex;


            float4 _MaskOffset;
            float4 _MaskScale;

            float4 _GradientOffset;

            float _RotationOffset;

            fixed4 _Color;


            struct appdata
            {
                float4 vertex : POSITION;

                fixed4 color : COLOR;

                float2 uv : TEXCOORD0;


                //------------------------------------------
                // Particle System Custom Data 1
                //
                // X = Texture Rotation
                //------------------------------------------

                float4 custom1 : TEXCOORD1;
            };


            struct v2f
            {
                float4 vertex : SV_POSITION;

                fixed4 color : COLOR;


                //------------------------------------------
                // 회전하는 Main Texture UV
                //------------------------------------------

                float2 mainUV : TEXCOORD0;


                //------------------------------------------
                // 고정 Mask UV
                //------------------------------------------

                float2 maskUV : TEXCOORD1;


                //------------------------------------------
                // 고정 Gradient UV
                //------------------------------------------

                float2 gradientUV : TEXCOORD2;
            };


            //------------------------------------------
            // UV Rotation
            //------------------------------------------

            float2 RotateUV(
                float2 uv,
                float angle
            )
            {
                float2 centeredUV =
                    uv - 0.5;


                float s;
                float c;

                sincos(
                    angle,
                    s,
                    c
                );


                float2 rotatedUV;


                rotatedUV.x =
                    centeredUV.x * c -
                    centeredUV.y * s;


                rotatedUV.y =
                    centeredUV.x * s +
                    centeredUV.y * c;


                return
                    rotatedUV + 0.5;
            }


            //------------------------------------------
            // Mask UV Transform
            //
            // Scale > 1
            // → Mask가 화면에서 커짐
            //
            // Scale < 1
            // → Mask가 화면에서 작아짐
            //
            // Offset은 실제 보이는 Mask 위치 기준
            //------------------------------------------

            float2 TransformMaskUV(
                float2 uv,
                float2 offset,
                float2 scale
            )
            {
                //------------------------------------------
                // 0 방지
                //------------------------------------------

                scale =
                    max(
                        abs(scale),
                        float2(
                            0.001,
                            0.001
                        )
                    );


                //------------------------------------------
                // Mask 중심을 기준으로
                // Scale / Position 처리
                //------------------------------------------

                float2 transformedUV =
                    (
                        uv -
                        0.5 -
                        offset
                    )
                    /
                    scale
                    +
                    0.5;


                return transformedUV;
            }


            v2f vert(appdata v)
            {
                v2f o;


                o.vertex =
                    UnityObjectToClipPos(
                        v.vertex
                    );


                //------------------------------------------
                // Main Texture Rotation
                //
                // Custom1.X
                //
                // 0     =   0°
                // 0.25  =  90°
                // 0.5   = 180°
                // 1     = 360°
                // -1    = -360°
                //------------------------------------------

                float rotation =
                    (
                        v.custom1.x *
                        UNITY_TWO_PI
                    )
                    +
                    radians(
                        _RotationOffset
                    );


                //------------------------------------------
                // Main Texture
                //
                // Main만 회전
                //------------------------------------------

                o.mainUV =
                    RotateUV(
                        v.uv,
                        rotation
                    );


                //------------------------------------------
                // Mask Texture
                //
                // Main 회전과 완전히 독립
                //
                // 위치와 크기를 별도로 조절
                //------------------------------------------

                o.maskUV =
                    TransformMaskUV(
                        v.uv,
                        _MaskOffset.xy,
                        _MaskScale.xy
                    );


                //------------------------------------------
                // Gradient
                //
                // Main 회전과 독립
                //------------------------------------------

                o.gradientUV =
                    v.uv +
                    _GradientOffset.xy;


                //------------------------------------------
                // Particle Color
                //------------------------------------------

                o.color =
                    v.color;


                return o;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                //------------------------------------------
                // Main Texture
                //------------------------------------------

                fixed4 mainTex =
                    tex2D(
                        _MainTex,
                        i.mainUV
                    );


                //------------------------------------------
                // Mask
                //
                // R Channel만 사용
                //
                // White = Visible
                // Black = Invisible
                //------------------------------------------

                fixed mask =
                    tex2D(
                        _MaskTex,
                        i.maskUV
                    ).r;


                //------------------------------------------
                // Mask Texture 범위 밖은
                // 무조건 0 처리
                //
                // Wrap Mode가 Repeat여도
                // Mask가 반복되지 않도록 방지
                //------------------------------------------

                fixed insideMaskUV =

                    step(
                        0.0,
                        i.maskUV.x
                    )
                    *
                    step(
                        i.maskUV.x,
                        1.0
                    )
                    *
                    step(
                        0.0,
                        i.maskUV.y
                    )
                    *
                    step(
                        i.maskUV.y,
                        1.0
                    );


                mask *=
                    insideMaskUV;


                //------------------------------------------
                // Color Gradient
                //
                // RGB만 사용
                // Alpha는 사용하지 않음
                //------------------------------------------

                fixed3 gradientColor =
                    tex2D(
                        _GradientTex,
                        i.gradientUV
                    ).rgb;


                //------------------------------------------
                // Final
                //------------------------------------------

                fixed4 result;


                //------------------------------------------
                // RGB
                //------------------------------------------

                result.rgb =
                    mainTex.rgb *
                    gradientColor *
                    _Color.rgb;


                //------------------------------------------
                // Alpha
                //------------------------------------------

                result.a =
                    mainTex.a *
                    mask *
                    _Color.a;


                //------------------------------------------
                // Particle System Color / Alpha
                // 가장 마지막에 적용
                //------------------------------------------

                result *=
                    i.color;


                return result;
            }

            ENDCG
        }
    }
}