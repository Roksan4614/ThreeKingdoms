using Cysharp.Threading.Tasks;
using Rev9.Tournament;
using System;
using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupTournamentHistory_Slot : MonoBehaviour, IValidatable
{
    CancellationTokenSource m_cts;

    Color m_clrPrevRevenge;

    public bool isTimeourRevenge => m_cts == null;

    private void Awake()
    {
        m_clrPrevRevenge = m_element.btnRevenge.image.color;
    }

    private void OnDestroy()
    {
        m_cts = m_cts.ReleaseCTS();
    }

    public void SetHistoryData(TournamentHistoryData _historyData, UnityAction<TournamentHistoryData> _callback)
    {
        bool isRevenge = _historyData.isRevenge;

        m_element.objAttack.SetActive(_historyData.isAttack);
        m_element.objDefence.SetActive(_historyData.isAttack == false);
        m_element.txtType.text = _historyData.isAttack ? "_공격_" : "_방어_";

        m_element.txtResult.text = $"{(_historyData.isWin ? "WIN" : "LOSE")}\n<color=#555555><size=80%>({(_historyData.isWin ? "+" : "")}{_historyData.resultPoint}p)</size></color>";
        if (ColorUtility.TryParseHtmlString($"#{(_historyData.isWin ? Palette.htmlString_Up : Palette.htmlString_Down)}", out Color clr))
            m_element.txtResult.color = clr;

        m_element.profile.SetProfileData(_historyData.indexProfile, _historyData.skin);

        m_element.objRevenge.gameObject.SetActive(isRevenge);

        m_element.txtNickname.text = _historyData.nickname;
        m_element.txtNickname.rt.SetAnchoredPositionY(isRevenge ? 33 : 0);

        m_cts = m_cts.ReleaseCTS();
        if (isRevenge == true)
        {
            if (_historyData.tick == 0)
            {
                m_element.btnRevenge.text = $"복수성공";
                m_element.objRevenge.transform.ForceRebuildLayout();
                if (ColorUtility.TryParseHtmlString("#05009C", out Color clrSuccess))
                    m_element.btnRevenge.image.color = clrSuccess;
                m_element.btnRevenge.interactable = false;
            }
            else
                TimerRevengeAsync(_historyData.dtEndRevenge).Forget();
        }

        m_element.button.onClick.RemoveAllListeners();
        m_element.button.onClick.AddListener(() => _callback?.Invoke(_historyData));

        m_element.btnRevenge.onClick.RemoveAllListeners();
        m_element.btnRevenge.onClick.AddListener(() =>
        {
            var batchData = _historyData.batchData;

            if (_historyData.teamDefence == null)
            {
                List<HeroInfoData> heroes = new();
                heroes.AddRange(batchData.heroes);
                for (int i = 0; i < (int)(heroes.Count * .5f); i++)
                {
                    var h = heroes[i];
                    var h_last = heroes[heroes.Count - i - 1];

                    var temp = h.sortIdx;

                    (h.sortIdx, h_last.sortIdx) = (h_last.sortIdx, temp);
                    (heroes[i], heroes[heroes.Count - i - 1]) = (h, h_last);
                }

                _historyData.teamDefence = heroes;

                var historyData = _historyData;
                batchData.heroes = heroes;
                historyData.batchData = batchData;

                _callback?.Invoke(historyData);
            }
            else
            {
                var historyData = _historyData;
                historyData.batchData.heroes = historyData.teamDefence;
                _callback?.Invoke(historyData);
            }
        });
    }

    async UniTask TimerRevengeAsync(System.DateTime _dtEnd)
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        TimeSpan ts = _dtEnd - Utils.GetUTC();
        m_element.btnRevenge.text = $"복수_<size=90%>({ts.ToRemainTime(25)})</size>";
        m_element.objRevenge.transform.ForceRebuildLayout();
        m_element.btnRevenge.image.color = m_clrPrevRevenge;
        m_element.btnRevenge.interactable = true;

        int prev = (int)ts.TotalSeconds;
        while (ts.TotalSeconds > 0)
        {
            ts = _dtEnd - Utils.GetUTC();
            var sec = (int)ts.TotalSeconds;
            if (sec != prev || sec <= 10)
            {
                prev = sec;

                m_element.btnRevenge.text = $"복수_<size=90%>({ts.ToRemainTime(25)})</size>";
                m_element.objRevenge.transform.ForceRebuildLayout();
            }

            await UniTask.NextFrame(token);
        }

        m_element.btnRevenge.text = $"<color=#555555>시간초과</color>";
        m_element.btnRevenge.image.color = Color.white;
        m_element.btnRevenge.interactable = false;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    //[SerializeField]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public Image imgPanel;
        public Button button;

        public GameObject objAttack;
        public GameObject objDefence;

        public TextMeshProUGUI txtType;
        public TextMeshProUGUI txtResult;
        public ProfileIconCompoent profile;
        public TextPanelHelper txtNickname;

        public ButtonHelper btnRevenge;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<Button>();

            imgPanel = _transform.GetComponent<Image>("Panel");
            objAttack = _transform.Find("Panel/Attack").gameObject;
            objDefence = _transform.Find("Panel/Defence").gameObject;

            txtType = _transform.GetComponent<TextMeshProUGUI>("Panel/Type/Text");
            txtResult = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_result");
            txtNickname = _transform.GetComponent<TextPanelHelper>("Panel/txt_nickname");

            profile = _transform.GetComponent<ProfileIconCompoent>("Panel/Slot_Profile");

            btnRevenge = _transform.GetComponent<ButtonHelper>("Panel/btn_revenge");
        }

        public GameObject objRevenge => btnRevenge.gameObject;
    }
    #endregion VALIDATE

}
