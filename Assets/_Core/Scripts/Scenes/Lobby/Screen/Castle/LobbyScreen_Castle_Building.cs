using System.Linq;
using UnityEngine;

public class LobbyScreen_Castle_Building : MonoBehaviour, IValidatable
{
    enum TimerStepType
    {
        Wait,
        Minute,
        Seconds
    }

    TimerStepType m_timeStepType = TimerStepType.Wait;

    [SerializeField]
    CastleObjectType m_objectType;
    public CastleObjectType objectType => m_objectType;

    ButtonHelper m_button;
    ButtonHelper button
    {
        get
        {
            if (m_button == null)
                m_button = transform.GetComponentInParent<LobbyScreen_Castle>().GetButton(m_objectType);
            return m_button;
        }
    }

    private void Start()
    {
        Signal.instance.CompleteCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            FinishUpgrade();
        });

        Signal.instance.StartCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            if (_castleData.remainUpgradeSeconds > 0)
                LoopUpgrade();
            else
                StartUpgrade();
        });

        Signal.instance.StopCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            StopUpgrade();

            var castleData = DataManager.castle.GetCaslteData(m_objectType);
            button.text = $"{castleData.name}";
            button.transform.ForceRebuildLayout();
        });

        Signal.instance.UpdateCaslteBuildingUpgrade.connect = SlotUpdateCaslteBuildingUpgrade;

    }

    private void OnEnable()
    {
        m_timeStepType = TimerStepType.Wait;

        var castleData = DataManager.castle.GetCaslteData(m_objectType);
        if (castleData.isDoingUpgrade)
        {
            if (castleData.isValidUpgrade)
                LoopUpgrade();
            else
                StopUpgrade();

            if (castleData.remainUpgradeSeconds > 0)
                button.text = $"{castleData.name}";
            else
            {
                var upgradeData = DataManager.castle.building.GetUpgradeData(castleData);
                SlotUpdateCaslteBuildingUpgrade(upgradeData);
            }
        }
        else
        {
            m_element.anim.Play($"Castle_BuildFX_{m_objectType}_None");
            button.text = $"{castleData.name}";
        }
        button.transform.ForceRebuildLayout();
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

        var castleData = DataManager.castle.GetCaslteData(m_objectType);
        button.text = $"{castleData.name}";
        button.transform.ForceRebuildLayout();
        m_timeStepType = TimerStepType.Wait;
    }
    public void LoopUpgrade()
    {
        m_element.anim.Play($"Castle_BuildFX_{m_objectType}_Loop");
    }

    void SlotUpdateCaslteBuildingUpgrade(Data_Castle_Building.CastleBuildingUpgradeData _updateData)
    {
        if (gameObject.activeInHierarchy == false || _updateData.objectType != m_objectType)
            return;

        var ts = _updateData.ts;

        button.text = Utils.MSpace(ts.ToRemainTime(), 24);

        if (ts.Minutes > 0)
        {

            if (m_timeStepType != TimerStepType.Minute)
            {
                m_timeStepType = TimerStepType.Minute;
                button.transform.ForceRebuildLayout();
            }
        }
        else
        {
            if (m_timeStepType != TimerStepType.Seconds)
            {
                m_timeStepType = TimerStepType.Seconds;
                button.transform.ForceRebuildLayout();
            }
        }
    }

    public Transform GetWallyPointRandom()
    {
        if (m_element.wallyPoint == null)
            return null;
        return m_element.wallyPoint[Random.Range(0, m_element.wallyPoint.Length)];
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform, m_objectType);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Animator anim;
        public Transform[] wallyPoint;

        public void Initialize(Transform _transform, CastleObjectType _objectType)
        {
            anim = _transform.GetComponent<Animator>();

            wallyPoint = _transform.parent?.Find($"WallyPoint/{_objectType.ToString()}")?.GetComponentsInChildren<Transform>().Skip(1)?.ToArray();
        }
    }
    #endregion VALIDATE

}
