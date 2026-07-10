using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyStoryMode_Slot_Node : MonoBehaviour, IValidatable
{
    int m_curIdx;
    List<Table_StoryMode_Node.TableStoryModeNodeData> m_data;

    public bool isOpenNode => m_element.objLock.activeSelf == false;

    private void Start()
    {
        m_element.button.onClick.AddListener(() => OnButtonAsync_Enter().Forget());
        m_element.btnChange.onClick.AddListener(OnButton_Change);
    }

    public void SetStoryNode(List<Table_StoryMode_Node.TableStoryModeNodeData> _data)
    {
        // 다음에 오픈할 차례라면
        {
            var nextOpenOrderNumber = DataManager.storyMode.nextOpenOrderNumber;
            bool isNextOpen = _data[0].order_num >= nextOpenOrderNumber;

            if (isNextOpen == true)
            {
                m_element.txtChoice.gameObject.SetActive(false);
                m_element.btnChange.gameObject.SetActive(false);
                m_element.button.text = "";

                m_element.objLock.SetActive(true);
                ((RectTransform)m_element.objLock.transform.GetChild(0)).SetAnchoredPositionY(30);

                var nodeData = TableManager.storyNode.GetNode_OrderNum(nextOpenOrderNumber)[0];
                m_element.txtDesc.text = $"{nodeData.chapter_key}-{nodeData.stage_key} 클리어시 해제";

                m_element.button.interactable = false;
                m_element.objBadge.SetActive(false);

                transform.ForceRebuildLayout();
                return;
            }
        }

        m_data = _data;
        m_element.txtChoice.gameObject.SetActive(false);
        m_element.btnChange.gameObject.SetActive(_data.Count > 1);

        var clearKey = DataManager.storyMode.nodeKeyNewClear;
        if (clearKey.IsActive())
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                if (m_data[i].node_key == clearKey)
                {
                    m_curIdx = i;
                    break;
                }
            }
        }

        SetNodeData();

        // 마지막 플레이한 것보다 나중이라면
        {
            bool overLastPlay = _data[0].order_num > DataManager.storyMode.nextPlayOrderNumber;
            if (overLastPlay)
            {
                ((RectTransform)m_element.objLock.transform.GetChild(0)).anchoredPosition = Vector2.zero;
                m_element.objLock.SetActive(true);
                m_element.button.interactable = false;
            }
            else
            {
                m_element.button.interactable = true;
                m_element.objLock.SetActive(false);
            }
        }

        transform.ForceRebuildLayout();
    }

    void SetNodeData()
    {
        var data = m_data[m_curIdx];

        int idxChangeSibling = m_element.objLock.transform.GetSiblingIndex();
        // 조건이 안된다면 ??? 로 표기해주자
        if (DataManager.storyMode.IsUnlock(data.node_key))
        {
            m_element.button.text = data.name;
            m_element.txtDesc.text = data.desc;
            idxChangeSibling--;

            m_element.objLock.SetActive(false);
            m_element.button.interactable = true;
        }
        else
        {
            m_element.button.text = "???";
            m_element.txtDesc.text = "";

            m_element.objLock.SetActive(true);
            m_element.button.interactable = false;

            idxChangeSibling++;
        }

        m_element.btnChange.transform.SetSiblingIndex(idxChangeSibling);

        var reqSeq = DataManager.storyMode.GetChoiceSeq(data.node_key, true);
        if (reqSeq.IsActive() == true)
        {
            m_element.txtChoice.gameObject.SetActive(true);
            m_element.txtChoice.text = reqSeq;
        }
        else
            m_element.txtChoice.gameObject.SetActive(false);

        bool isComplete = DataManager.storyMode.IsComplete(data.node_key);
        m_element.objBadge.SetActive(isComplete);
        if (isComplete && DataManager.storyMode.nodeKeyNewClear == data.node_key)
            RewardStartAsync().Forget();
    }

    async UniTask OnButtonAsync_Enter()
    {
        m_element.button.interactable = false;

        var result = await PopupManager.instance.OpenModalAsync("_입장하시겠습니까?");

        if (result != StatusType.Success)
        {
            m_element.button.interactable = true;
            return;
        }

#if UNITY_EDITOR
        result = await PopupManager.instance.OpenModalAsync("EDITOR: 입장할꺼야??\n취소하면 저장만 할거야");

        if (result != StatusType.Success)
        {
            m_element.button.interactable = true;
            DataManager.storyMode.TestSave(m_data[m_curIdx]);
            return;
        }
#endif

        DataManager.storyMode.EnterAsync(m_data[m_curIdx].node_key).Forget();
    }

    void OnButton_Change()
    {
        m_curIdx = (m_curIdx + 1) % m_data.Count;
        SetNodeData();
    }

    async UniTask RewardStartAsync()
    {
        var reward = TableManager.storyNode.GetNode(DataManager.storyMode.nodeKeyNewClear);
        DataManager.storyMode.nodeKeyNewClear = null;

        var rt = (RectTransform)m_element.objBadge.transform;

        var prevScale = rt.localScale;
        rt.localScale *= 2;
        await rt.DOScale(prevScale, 0.1f).SetEase(Ease.OutBack);

        // 보상이 영웅이라면
        if (reward.reward_character.IsActive())
        {
            var heroName = TableManager.stringHero.GetHeroName(reward.reward_character);
            PopupManager.instance.AlertShow($"[{heroName}]을_얻었습니다.", 70);

            var heroInfoData = DataManager.userInfo.GetHeroInfoData(reward.reward_character);

            if (heroInfoData.isActive == false)
            {

                PopupHeroInfo popupHeroInfo = await PopupManager.instance.OpenPopupAsync<PopupHeroInfo>(PopupType.Hero_HeroInfo);
                popupHeroInfo.AutoCloseAsync(5f).Forget();
                heroInfoData = new(reward.reward_character, GradeType.Normal);

                await popupHeroInfo.SetHeroInfoDataAsync(heroInfoData, true);
            }
            else
            {
                PopupManager.instance.AlertShow($"[{heroName}]의_영혼석을_획득했습니다.");
                DataManager.userInfo.AddHeroSoul(reward.reward_character, 10);
            }
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public Button btnChange;

        public TextMeshProUGUI txtDesc;
        public TextMeshProUGUI txtChoice;

        public GameObject objLock;
        public GameObject objBadge;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>("Panel");
            btnChange = _transform.GetComponent<Button>("Panel/btn_change");

            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");
            txtChoice = _transform.GetComponent<TextMeshProUGUI>("txt_choice");

            objLock = _transform.Find("Panel/Lock").gameObject;
            objBadge = _transform.Find("Panel/Badge").gameObject;
        }
    }
    #endregion VALIDATE

}
