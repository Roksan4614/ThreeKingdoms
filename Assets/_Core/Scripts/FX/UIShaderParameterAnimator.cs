using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UIShaderParameterAnimator : MonoBehaviour
{
    [Header("Material")]

    [Tooltip(
        "애니메이션할 UI 셰이더 머티리얼입니다.\n" +
        "비워두면 현재 Graphic에 지정된 머티리얼을 자동으로 사용합니다."
    )]
    [SerializeField]
    private Material baseMaterial;


    [Header("Dissolve")]

    [Tooltip("셰이더의 디졸브 진행도 프로퍼티 이름")]
    [SerializeField]
    private string dissolveProperty = "_DissolveAmount";

    [Tooltip("Animation 창에서 키를 잡을 디졸브 진행도")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float dissolveAmount = 0.0f;


    [Header("Opacity")]

    [Tooltip("셰이더의 투명도 프로퍼티 이름")]
    [SerializeField]
    private string opacityProperty = "_Opacity";

    [Tooltip("Animation 창에서 키를 잡을 투명도")]
    [SerializeField]
    [Range(0.0f, 1.0f)]
    private float opacity = 1.0f;


    [Header("Dissolve Edge")]

    [Tooltip("셰이더의 디졸브 경계 두께 프로퍼티 이름")]
    [SerializeField]
    private string edgeWidthProperty = "_EdgeWidth";

    [Tooltip("Animation 창에서 키를 잡을 경계 두께")]
    [SerializeField]
    [Min(0.0f)]
    private float edgeWidth = 0.05f;

    [Tooltip("셰이더의 디졸브 경계 강도 프로퍼티 이름")]
    [SerializeField]
    private string edgePowerProperty = "_EdgePower";

    [Tooltip("Animation 창에서 키를 잡을 경계 밝기")]
    [SerializeField]
    [Min(0.0f)]
    private float edgePower = 1.0f;

    [Tooltip("셰이더의 디졸브 경계 색상 프로퍼티 이름")]
    [SerializeField]
    private string edgeColorProperty = "_EdgeColor";

    [Tooltip("Animation 창에서 키를 잡을 경계 색상")]
    [SerializeField]
    [ColorUsage(true, true)]
    private Color edgeColor = Color.white;


    [Header("Dissolve UV")]

    [Tooltip("셰이더의 디졸브 UV 오프셋 프로퍼티 이름")]
    [SerializeField]
    private string dissolveOffsetProperty = "_DissolveOffset";

    [Tooltip("Animation 창에서 키를 잡을 디졸브 텍스처 이동값")]
    [SerializeField]
    private Vector2 dissolveOffset = Vector2.zero;


    private Graphic targetGraphic;
    private Material materialInstance;

    private int materialSourceInstanceId;

    private int dissolvePropertyId;
    private int opacityPropertyId;
    private int edgeWidthPropertyId;
    private int edgePowerPropertyId;
    private int edgeColorPropertyId;
    private int dissolveOffsetPropertyId;

    private float appliedDissolveAmount = float.NaN;
    private float appliedOpacity = float.NaN;
    private float appliedEdgeWidth = float.NaN;
    private float appliedEdgePower = float.NaN;

    private Color appliedEdgeColor;
    private Vector2 appliedDissolveOffset;

    private bool hasAppliedEdgeColor;
    private bool hasAppliedDissolveOffset;


    public float DissolveAmount
    {
        get => dissolveAmount;
        set
        {
            dissolveAmount = Mathf.Clamp01(value);
            ApplyProperties(false);
        }
    }

    public float Opacity
    {
        get => opacity;
        set
        {
            opacity = Mathf.Clamp01(value);
            ApplyProperties(false);
        }
    }

    public float EdgeWidth
    {
        get => edgeWidth;
        set
        {
            edgeWidth = Mathf.Max(0.0f, value);
            ApplyProperties(false);
        }
    }

    public float EdgePower
    {
        get => edgePower;
        set
        {
            edgePower = Mathf.Max(0.0f, value);
            ApplyProperties(false);
        }
    }

    public Color EdgeColor
    {
        get => edgeColor;
        set
        {
            edgeColor = value;
            ApplyProperties(false);
        }
    }

    public Vector2 DissolveOffset
    {
        get => dissolveOffset;
        set
        {
            dissolveOffset = value;
            ApplyProperties(false);
        }
    }


    private void Reset()
    {
        CacheGraphic();

        if (targetGraphic != null)
        {
            baseMaterial = targetGraphic.material;
        }

        CachePropertyIds();
    }

    private void Awake()
    {
        CacheGraphic();
        CachePropertyIds();
    }

    private void OnEnable()
    {
        CacheGraphic();
        CachePropertyIds();
        EnsureMaterialInstance();
        ApplyProperties(true);
    }

    private void LateUpdate()
    {
        /*
         * Animator뿐 아니라 다른 스크립트에서 직접 필드값을 변경했을 때도
         * 값이 누락되지 않도록 검사합니다.
         *
         * 실제 값이 달라졌을 때만 머티리얼을 갱신합니다.
         */
        ApplyProperties(false);
    }

    private void OnValidate()
    {
        dissolveAmount = Mathf.Clamp01(dissolveAmount);
        opacity = Mathf.Clamp01(opacity);
        edgeWidth = Mathf.Max(0.0f, edgeWidth);
        edgePower = Mathf.Max(0.0f, edgePower);

        CacheGraphic();
        CachePropertyIds();
        EnsureMaterialInstance();
        ApplyProperties(true);
    }

    private void OnDidApplyAnimationProperties()
    {
        /*
         * Animation 창 또는 Animator가 이 컴포넌트의 값을 변경하면
         * 변경된 값을 즉시 머티리얼에 전달합니다.
         */
        EnsureMaterialInstance();
        ApplyProperties(false);
    }

    private void OnDisable()
    {
        ReleaseMaterialInstance();
    }

    private void OnDestroy()
    {
        ReleaseMaterialInstance();
    }


    private void CacheGraphic()
    {
        if (targetGraphic == null)
        {
            targetGraphic = GetComponent<Graphic>();
        }
    }

    private void CachePropertyIds()
    {
        dissolvePropertyId = GetPropertyId(dissolveProperty);
        opacityPropertyId = GetPropertyId(opacityProperty);
        edgeWidthPropertyId = GetPropertyId(edgeWidthProperty);
        edgePowerPropertyId = GetPropertyId(edgePowerProperty);
        edgeColorPropertyId = GetPropertyId(edgeColorProperty);
        dissolveOffsetPropertyId = GetPropertyId(dissolveOffsetProperty);
    }

    private static int GetPropertyId(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return -1;
        }

        return Shader.PropertyToID(propertyName);
    }

    private void EnsureMaterialInstance()
    {
        CacheGraphic();

        if (targetGraphic == null)
        {
            return;
        }

        /*
         * Base Material이 비어 있다면 현재 Graphic의 머티리얼을
         * 최초 소스로 자동 등록합니다.
         */
        if (baseMaterial == null)
        {
            Material currentMaterial = targetGraphic.material;

            if (currentMaterial != null && currentMaterial != materialInstance)
            {
                baseMaterial = currentMaterial;
            }
        }

        if (baseMaterial == null)
        {
            return;
        }

        int currentSourceId = baseMaterial.GetInstanceID();

        bool requiresNewInstance =
            materialInstance == null ||
            materialSourceInstanceId != currentSourceId;

        if (!requiresNewInstance)
        {
            if (targetGraphic.material != materialInstance)
            {
                targetGraphic.material = materialInstance;
                targetGraphic.SetMaterialDirty();
            }

            return;
        }

        ReleaseMaterialInstance();

        materialInstance = new Material(baseMaterial)
        {
            name = $"{baseMaterial.name} (UI Animated Instance)",
            hideFlags = HideFlags.HideAndDontSave
        };

        materialSourceInstanceId = currentSourceId;

        targetGraphic.material = materialInstance;
        targetGraphic.SetMaterialDirty();

        ResetAppliedValueCache();
    }

    private void ApplyProperties(bool force)
    {
        EnsureMaterialInstance();

        if (materialInstance == null)
        {
            return;
        }

        bool changed = false;

        changed |= ApplyFloat(
            dissolvePropertyId,
            dissolveAmount,
            ref appliedDissolveAmount,
            force
        );

        changed |= ApplyFloat(
            opacityPropertyId,
            opacity,
            ref appliedOpacity,
            force
        );

        changed |= ApplyFloat(
            edgeWidthPropertyId,
            edgeWidth,
            ref appliedEdgeWidth,
            force
        );

        changed |= ApplyFloat(
            edgePowerPropertyId,
            edgePower,
            ref appliedEdgePower,
            force
        );

        changed |= ApplyColor(
            edgeColorPropertyId,
            edgeColor,
            ref appliedEdgeColor,
            ref hasAppliedEdgeColor,
            force
        );

        changed |= ApplyVector(
            dissolveOffsetPropertyId,
            dissolveOffset,
            ref appliedDissolveOffset,
            ref hasAppliedDissolveOffset,
            force
        );

        if (changed && targetGraphic != null)
        {
            /*
             * UI Mask나 Stencil용 materialForRendering이 사용되는 경우까지
             * 변경 내용을 다시 반영하도록 머티리얼을 Dirty 처리합니다.
             */
            targetGraphic.SetMaterialDirty();
        }
    }

    private bool ApplyFloat(
        int propertyId,
        float value,
        ref float appliedValue,
        bool force
    )
    {
        if (propertyId < 0)
        {
            return false;
        }

        if (!materialInstance.HasProperty(propertyId))
        {
            return false;
        }

        if (!force && Mathf.Approximately(value, appliedValue))
        {
            return false;
        }

        materialInstance.SetFloat(propertyId, value);
        appliedValue = value;

        return true;
    }

    private bool ApplyColor(
        int propertyId,
        Color value,
        ref Color appliedValue,
        ref bool hasAppliedValue,
        bool force
    )
    {
        if (propertyId < 0)
        {
            return false;
        }

        if (!materialInstance.HasProperty(propertyId))
        {
            return false;
        }

        if (!force && hasAppliedValue && value == appliedValue)
        {
            return false;
        }

        materialInstance.SetColor(propertyId, value);

        appliedValue = value;
        hasAppliedValue = true;

        return true;
    }

    private bool ApplyVector(
        int propertyId,
        Vector2 value,
        ref Vector2 appliedValue,
        ref bool hasAppliedValue,
        bool force
    )
    {
        if (propertyId < 0)
        {
            return false;
        }

        if (!materialInstance.HasProperty(propertyId))
        {
            return false;
        }

        if (!force && hasAppliedValue && value == appliedValue)
        {
            return false;
        }

        materialInstance.SetVector(
            propertyId,
            new Vector4(value.x, value.y, 0.0f, 0.0f)
        );

        appliedValue = value;
        hasAppliedValue = true;

        return true;
    }

    private void ResetAppliedValueCache()
    {
        appliedDissolveAmount = float.NaN;
        appliedOpacity = float.NaN;
        appliedEdgeWidth = float.NaN;
        appliedEdgePower = float.NaN;

        hasAppliedEdgeColor = false;
        hasAppliedDissolveOffset = false;
    }

    private void ReleaseMaterialInstance()
    {
        if (materialInstance == null)
        {
            return;
        }

        if (targetGraphic != null && targetGraphic.material == materialInstance)
        {
            targetGraphic.material = baseMaterial;
            targetGraphic.SetMaterialDirty();
        }

        DestroyMaterial(materialInstance);

        materialInstance = null;
        materialSourceInstanceId = 0;

        ResetAppliedValueCache();
    }

    private static void DestroyMaterial(Material material)
    {
        if (material == null)
        {
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            Object.DestroyImmediate(material);
            return;
        }
#endif

        Object.Destroy(material);
    }


    public void SetDissolveAmount(float value)
    {
        DissolveAmount = value;
    }

    public void SetOpacity(float value)
    {
        Opacity = value;
    }

    public void SetEdgeWidth(float value)
    {
        EdgeWidth = value;
    }

    public void SetEdgePower(float value)
    {
        EdgePower = value;
    }

    public void SetDissolveOffsetX(float value)
    {
        dissolveOffset.x = value;
        ApplyProperties(false);
    }

    public void SetDissolveOffsetY(float value)
    {
        dissolveOffset.y = value;
        ApplyProperties(false);
    }
}