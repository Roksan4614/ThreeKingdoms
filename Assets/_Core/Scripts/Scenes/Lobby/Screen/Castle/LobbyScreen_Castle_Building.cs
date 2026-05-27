using UnityEngine;

public class LobbyScreen_Castle_Building : MonoBehaviour, IValidatable
{
    [SerializeField]
    CastleObjectType m_objectType;
    public CastleObjectType objectType => m_objectType;

    private void Start()
    {
        Signal.instance.CompleteCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (_castleData.type == m_objectType)
                FinishUpgrade();
        });

        Signal.instance.StartCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (_castleData.type == m_objectType)
            {
                if (_castleData.remainUpgradeSeconds > 0)
                    LoopUpgrade();
                else
                    StartUpgrade();
            }
        });

        Signal.instance.StopCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (_castleData.type == m_objectType)
                StopUpgrade();
        });
    }

    private void OnEnable()
    {
        var castleData = DataManager.castle.GetCaslteData(m_objectType);
        if (castleData.isDoingUpgrade)
        {
            if (castleData.isValidUpgrade)
                LoopUpgrade();
            else
                StopUpgrade();
        }
        else
            m_element.anim.Play($"Castle_BuildFX_{m_objectType}_None");
    }

    public void StartUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_Act");
    }
    public void StopUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_LoopStop");
    }
    public void FinishUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_End");
    }
    public void LoopUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_Loop");
    }


    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Animator anim;

        public void Initialize(Transform _transform)
        {
            anim = _transform.GetComponent<Animator>();
        }
    }
    #endregion VALIDATE

}
