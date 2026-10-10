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

    bool m_isGateSub = false;

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
        if (m_objectType == CastleObjectType.Gate && name.Contains("Castle_Sub"))
            m_isGateSub = true;

        Signal.instance.CompleteCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            // 이전 레벨이 2,3,4 면 2로 가기위해 1_End를 실행해줘야해
            // 5 6 7 8 이면 3으로 가기 위해 2_End를..
            // 9여야 마지막 4를 가기 위해 3_End ㄱㄱ
            var prevLevel = _castleData.level - 1;
            var prevAniIdx = prevLevel < 4 ? 1 : prevLevel < 9 ? 2 : 3;
            FinishUpgrade(prevAniIdx);
        });

        Signal.instance.StartCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            if (_castleData.remainUpgradeSeconds > 0)
                LoopUpgrade(_castleData.aniIdxUpgrade);
            else
                StartUpgrade(_castleData.aniIdxUpgrade);
        });

        Signal.instance.StopCaslteBuildingUpgrade.connectLambda = new(this, _castleData =>
        {
            if (gameObject.activeInHierarchy == false || _castleData.type != m_objectType)
                return;

            StopUpgrade(_castleData.aniIdxUpgrade);

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
                LoopUpgrade(castleData.aniIdxUpgrade);
            else
                StopUpgrade(castleData.aniIdxUpgrade);

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
            m_element.anim.Play($"Lv{castleData.aniIdxUpgrade:0#}_Idle");
            button.text = $"{castleData.name}";
        }
        button.transform.ForceRebuildLayout();
    }

    public void StartUpgrade(int _aniIdx)
    {
        if (m_isGateSub)
            return;
        m_element.anim.Play($"Lv{_aniIdx:0#}_Start");
    }
    public void StopUpgrade(int _aniIdx)
    {
        if (m_isGateSub)
            return;
        m_element.anim.Play($"Lv{_aniIdx:0#}_Loop");
    }
    public void FinishUpgrade(int _aniIdx)
    {
        m_element.anim.Play($"Lv{_aniIdx:0#}_End");

        var castleData = DataManager.castle.GetCaslteData(m_objectType);
        button.text = $"{castleData.name}";
        button.transform.ForceRebuildLayout();
        m_timeStepType = TimerStepType.Wait;
    }
    public void LoopUpgrade(int _aniIdx)
    {
        if (m_isGateSub)
            return;
        m_element.anim.Play($"Lv{_aniIdx:0#}_Loop");
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
