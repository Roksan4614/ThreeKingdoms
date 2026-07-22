Shader "Custom/FX/eff_com_Aura_BoxDissolve_AB"
{
    Properties
    {
        // 실제 오오라 이미지
        _MainTex ("Aura Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // 흰색 = 분리되는 영역
        // 검은색 = 원본이 유지되는 영역
        _BreakMaskTex ("Breakable Area Mask", 2D) = "black" {}

        // 마스크 조절
        [Toggle] _InvertBreakMask ("Invert Break Mask", Float) = 0

        // 0 = 마스크 영향 없음
        // 1 = 원본 마스크
        // 1 이상 = 어두운 회색 영역까지 강하게 적용
        _MaskStrength ("Mask Strength", Range(0, 8)) = 1

        // 양수 = 마스크 영향 범위 확장
        // 음수 = 마스크 영향 범위 축소
        _MaskExpansion ("Mask Expansion", Range(-1, 1)) = 0

        // 블록 분할 수
        _GridCount ("Grid Count XY", Vector) = (18, 14, 0, 0)

        // 이동 기준점
        _Center ("Movement Center", Vector) = (0.5, 0.5, 0, 0)

        // 셀별 반복 속도
        _SpawnSpeed ("Spawn Speed", Range(0.01, 8)) = 1.0

        // 한 주기 중 파편이 존재하는 비율
        _LifeRatio ("Fragment Life Ratio", Range(0.05, 1)) = 0.7

        // 파편을 생성하는 셀 비율
        _Density ("Emission Density", Range(0, 1)) = 0.65

        // 랜덤 패턴 변경값
        _Seed ("Random Seed", Float) = 0

        // 이동 거리
        _MoveDistance ("Move Distance", Range(0, 0.5)) = 0.08

        // 0 = 중심으로부터 방사형
        // 1 = 중심 기준 좌우 분리
        _HorizontalBias ("Horizontal Direction Bias", Range(0, 1)) = 1

        // 상하 방향 랜덤 편차
        _VerticalJitter ("Vertical Jitter", Range(0, 1)) = 0.2

        // 이동 곡선
        _MoveEase ("Move Ease", Range(0.25, 4)) = 1.8

        // 원본 위치에서 분리되는 시간
        _DetachEnd ("Detach End", Range(0.001, 0.5)) = 0.1

        // 원래 자리가 다시 복구되기 시작하는 시점
        _RefillStart ("Source Refill Start", Range(0.05, 0.95)) = 0.35

        // 이동 파편이 사라지기 시작하는 시점
        _FadeStart ("Fragment Fade Start", Range(0.05, 0.95)) = 0.35

        // 이동 파편 밝기
        _FragmentBrightness ("Fragment Brightness", Range(0, 4)) = 1

        // Animator 등에서 시간을 직접 제어할 때 사용
        [Toggle] _UseManualTime ("Use Manual Time", Float) = 0
        _ManualTime ("Manual Time", Float) = 0

        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcBlend ("Source Blend", Float) = 5

        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstBlend ("Destination Blend", Float) = 10

        _AlphaClip ("Alpha Clip", Range(0, 0.5)) = 0.001
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "False"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual

        Blend [_SrcBlend] [_DstBlend]

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color  : COLOR;
                float2 uv     : TEXCOORD0;
            };

            sampler2D _MainTex;
            sampler2D _BreakMaskTex;

            fixed4 _Color;

            float _InvertBreakMask;
            float _MaskStrength;
            float _MaskExpansion;

            float4 _GridCount;
            float4 _Center;

            float _SpawnSpeed;
            float _LifeRatio;
            float _Density;
            float _Seed;

            float _MoveDistance;
            float _HorizontalBias;
            float _VerticalJitter;
            float _MoveEase;

            float _DetachEnd;
            float _RefillStart;
            float _FadeStart;

            float _FragmentBrightness;

            float _UseManualTime;
            float _ManualTime;

            float _AlphaClip;

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);

                return frac(value.x * value.y);
            }

            float2 SafeNormalize(float2 value)
            {
                return value *
                    rsqrt(max(dot(value, value), 0.000001));
            }

            float IsInsideUV(float2 uv)
            {
                float insideX =
                    step(0.0, uv.x) *
                    step(uv.x, 1.0);

                float insideY =
                    step(0.0, uv.y) *
                    step(uv.y, 1.0);

                return insideX * insideY;
            }

            float GetEffectTime()
            {
                float useManual =
                    step(0.5, _UseManualTime);

                return lerp(
                    _Time.y,
                    _ManualTime,
                    useManual
                );
            }

            float2 GetGrid()
            {
                return max(
                    _GridCount.xy,
                    float2(1.0, 1.0)
                );
            }

            float2 GetCell(float2 uv)
            {
                return floor(uv * GetGrid());
            }

            float2 GetCellCenter(float2 cell)
            {
                return (cell + 0.5) / GetGrid();
            }

            // ------------------------------------------------------------
            // 마스크 계산
            //
            // 최종 반환값:
            // 0 = 원본이 완전히 유지되는 영역
            // 1 = 블록 분리 효과가 완전히 적용되는 영역
            // ------------------------------------------------------------
            float GetBreakMask(float2 uv)
            {
                float maskValue =
                    tex2D(
                        _BreakMaskTex,
                        uv
                    ).r;

                // 흑백 영향 반전
                float invert =
                    step(0.5, _InvertBreakMask);

                maskValue =
                    lerp(
                        maskValue,
                        1.0 - maskValue,
                        invert
                    );

                // 영향 범위 확장 및 축소
                maskValue =
                    saturate(
                        maskValue +
                        _MaskExpansion
                    );

                // 마스크 강도 조절
                //
                // Strength 1:
                // 원본 마스크 유지
                //
                // Strength 1 이상:
                // 회색 영역을 더 빠르게 흰색으로 강화
                //
                // Strength 0:
                // 마스크 영향 제거
                if (_MaskStrength <= 0.0001)
                {
                    return 0.0;
                }

                maskValue =
                    1.0 -
                    pow(
                        1.0 - maskValue,
                        _MaskStrength
                    );

                return saturate(maskValue);
            }

            // ------------------------------------------------------------
            // 셀별 반복 상태
            //
            // enabled:
            // 해당 셀이 파편 생성에 사용되는지
            //
            // alive:
            // 현재 파편이 살아 있는 상태인지
            //
            // age:
            // 파편 생명 진행도
            // ------------------------------------------------------------
            void GetCellState(
                float2 cell,
                out float enabled,
                out float alive,
                out float age
            )
            {
                float randomEnable =
                    Hash21(
                        cell * 1.913 +
                        _Seed * 3.17 +
                        7.31
                    );

                enabled =
                    step(
                        randomEnable,
                        _Density
                    );

                float randomPhase =
                    Hash21(
                        cell +
                        _Seed +
                        19.73
                    );

                float phase =
                    frac(
                        GetEffectTime() *
                        _SpawnSpeed +
                        randomPhase
                    );

                float lifeRatio =
                    max(
                        _LifeRatio,
                        0.001
                    );

                alive =
                    enabled *
                    (
                        1.0 -
                        step(
                            lifeRatio,
                            phase
                        )
                    );

                age =
                    saturate(
                        phase /
                        lifeRatio
                    );
            }

            // ------------------------------------------------------------
            // 파편 이동 방향
            // ------------------------------------------------------------
            float2 GetMoveDirection(float2 cell)
            {
                float2 cellCenter =
                    GetCellCenter(cell);

                float2 centerDelta =
                    cellCenter -
                    _Center.xy;

                float2 radialDirection =
                    SafeNormalize(centerDelta);

                float side =
                    centerDelta.x >= 0.0
                    ? 1.0
                    : -1.0;

                float verticalRandom =
                    Hash21(
                        cell * 1.37 +
                        _Seed +
                        11.92
                    );

                float verticalDirection =
                    (
                        verticalRandom *
                        2.0 -
                        1.0
                    ) *
                    _VerticalJitter;

                float2 horizontalDirection =
                    SafeNormalize(
                        float2(
                            side,
                            verticalDirection
                        )
                    );

                return SafeNormalize(
                    lerp(
                        radialDirection,
                        horizontalDirection,
                        _HorizontalBias
                    )
                );
            }

            float2 GetMoveOffset(float2 cell)
            {
                float enabled;
                float alive;
                float age;

                GetCellState(
                    cell,
                    enabled,
                    alive,
                    age
                );

                float easedAge =
                    pow(
                        saturate(age),
                        max(
                            _MoveEase,
                            0.001
                        )
                    );

                return
                    GetMoveDirection(cell) *
                    _MoveDistance *
                    easedAge *
                    alive;
            }

            // ------------------------------------------------------------
            // 원래 위치에 붙어 있는 블록의 가시성
            // ------------------------------------------------------------
            float GetAttachedVisibility(float2 cell)
            {
                float enabled;
                float alive;
                float age;

                GetCellState(
                    cell,
                    enabled,
                    alive,
                    age
                );

                float detach =
                    smoothstep(
                        0.0,
                        max(
                            _DetachEnd,
                            0.001
                        ),
                        age
                    );

                float refill =
                    1.0 -
                    smoothstep(
                        _RefillStart,
                        1.0,
                        age
                    );

                float hole =
                    enabled *
                    alive *
                    detach *
                    refill;

                return
                    1.0 -
                    saturate(hole);
            }

            // ------------------------------------------------------------
            // 이동한 블록 샘플링
            // ------------------------------------------------------------
            fixed4 SampleMovingFragment(
                float2 uv,
                fixed4 vertexColor
            )
            {
                float2 estimatedCell =
                    GetCell(uv);

                float2 estimatedOffset =
                    GetMoveOffset(
                        estimatedCell
                    );

                float2 estimatedSourceUV =
                    uv -
                    estimatedOffset;

                float2 sourceCell =
                    GetCell(
                        estimatedSourceUV
                    );

                float2 sourceOffset =
                    GetMoveOffset(
                        sourceCell
                    );

                float2 sourceUV =
                    uv -
                    sourceOffset;

                float2 verifiedCell =
                    GetCell(sourceUV);

                float cellDifference =
                    abs(
                        verifiedCell.x -
                        sourceCell.x
                    ) +
                    abs(
                        verifiedCell.y -
                        sourceCell.y
                    );

                float validCell =
                    1.0 -
                    step(
                        0.001,
                        cellDifference
                    );

                float inside =
                    IsInsideUV(sourceUV);

                float enabled;
                float alive;
                float age;

                GetCellState(
                    sourceCell,
                    enabled,
                    alive,
                    age
                );

                float detached =
                    smoothstep(
                        0.0,
                        max(
                            _DetachEnd,
                            0.001
                        ),
                        age
                    );

                float fade =
                    1.0 -
                    smoothstep(
                        _FadeStart,
                        1.0,
                        age
                    );

                // 마스크는 색으로 출력되지 않고
                // 분리 가능 여부에만 사용
                float breakMask =
                    GetBreakMask(sourceUV);

                fixed4 sourceColor =
                    tex2D(
                        _MainTex,
                        sourceUV
                    ) *
                    vertexColor;

                float visibility =
                    enabled *
                    alive *
                    detached *
                    fade *
                    validCell *
                    inside *
                    breakMask;

                sourceColor.rgb *=
                    _FragmentBrightness;

                sourceColor.a *=
                    visibility;

                return sourceColor;
            }

            // Straight Alpha 방식 Over 합성
            fixed4 AlphaOver(
                fixed4 bottom,
                fixed4 top
            )
            {
                float outputAlpha =
                    top.a +
                    bottom.a *
                    (
                        1.0 -
                        top.a
                    );

                float3 outputColor =
                (
                    top.rgb *
                    top.a +
                    bottom.rgb *
                    bottom.a *
                    (
                        1.0 -
                        top.a
                    )
                ) /
                max(
                    outputAlpha,
                    0.00001
                );

                return fixed4(
                    outputColor,
                    outputAlpha
                );
            }

            v2f vert(appdata input)
            {
                v2f output;

                output.vertex =
                    UnityObjectToClipPos(
                        input.vertex
                    );

                output.uv =
                    input.uv;

                output.color =
                    input.color *
                    _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv =
                    input.uv;

                fixed4 baseColor =
                    tex2D(
                        _MainTex,
                        uv
                    ) *
                    input.color;

                // 0 = 원본 완전 유지
                // 1 = 블록 분리 효과 완전 적용
                float breakMask =
                    GetBreakMask(uv);

                float2 currentCell =
                    GetCell(uv);

                float attachedVisibility =
                    GetAttachedVisibility(
                        currentCell
                    );

                // 마스크 검은 영역:
                // 원본이 항상 100% 유지됨
                //
                // 마스크 흰 영역:
                // 셀의 상태에 따라 원본에 구멍 발생
                float baseVisibility =
                    lerp(
                        1.0,
                        attachedVisibility,
                        breakMask
                    );

                fixed4 baseLayer =
                    baseColor;

                baseLayer.a *=
                    baseVisibility;

                fixed4 movingLayer =
                    SampleMovingFragment(
                        uv,
                        input.color
                    );

                fixed4 finalColor =
                    AlphaOver(
                        baseLayer,
                        movingLayer
                    );

                clip(
                    finalColor.a -
                    _AlphaClip
                );

                return finalColor;
            }

            ENDCG
        }
    }

    Fallback Off
}