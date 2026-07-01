using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using static Data_BossRaid;

public class PopupBossRaidResultComponent : BasePopupComponent
{
    PopupBossRaidResultComponent() : base(PopupType.BossRaidResult) { }

    protected override void Awake()
    {
        m_element.btnConfirm.onClick.AddListener(() =>
        {
            m_element.btnConfirm.interactable = false;
            BossRaidWorker.instance.ExitAsync().Forget();
        });
    }

    public override void OpenPopup(params object[] _args)
    {
        Utils.SetActivePunch(transform, true);

        var dataRaid = DataManager.bossRaid.data;

        // ∫∏Ω∫¿Ã∏ß
        m_element.txtName.text = dataRaid.bossName;

        var uid = DataManager.userInfo.uid;
        var dataRank = DataManager.bossRaid.rankNow;
        for (int i = 0; i < dataRank.Count; i++)
        {
            if (dataRank[i].uid == uid)
            {
                var data = dataRank[i];

                // ∑©≈∑
                string rank = $"{data.rank}¿ß";
                rank += $"\n<color=#666666><size=60%>/{dataRank.Count}\n({(data.point == 0 ? 100 : (data.rank - 1) / (float)dataRank.Count * 100):0.00}%)</size></color>";
                m_element.txtRank.text = rank;

                // ¿‘»˘ µ•πÃ¡ˆ
                m_element.txtDamage.text = $"¿‘»˘_«««ÿ∑Æ\n<size=120%><color=#000000>{data.point:#,0}</color></size>";

                // »πµÊ ∆˜¿Œ∆Æ
                m_element.txtPoint.text = $"»πµÊ_∆˜¿Œ∆Æ\n<size=120%><color=#000000>{1422:#,0}</color></size>";

                break;
            }
        }

        m_element.imgSuccess.SetActive(BossRaidWorker.instance.isSuccessed);
        m_element.imgFail.SetActive(BossRaidWorker.instance.isSuccessed == false);
    }

    #region VALIDATE
    public override void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper btnConfirm;

        public TextMeshProUGUI txtName;
        public TextMeshProUGUI txtRank;
        public TextMeshProUGUI txtDamage;
        public TextMeshProUGUI txtPoint;

        public GameObject imgSuccess;
        public GameObject imgFail;

        public void Initialize(Transform _transform)
        {
            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");

            txtName = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_name");
            txtRank = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_rank");
            txtDamage = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_damage");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_point");

            imgSuccess = _transform.Find("Panel/img_success").gameObject;
            imgFail = _transform.Find("Panel/img_fail").gameObject;
        }
    }
    #endregion VALIDATE

}
