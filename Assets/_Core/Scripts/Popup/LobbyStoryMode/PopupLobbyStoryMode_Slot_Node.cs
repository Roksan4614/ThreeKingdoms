using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
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

    public void SetStoryNode(List<Table_StoryMode_Node.TableStoryModeNodeData> _data, RegionType _region = RegionType.NONE)
    {
        m_curIdx = 0;
        gameObject.SetActive(true);

        // 다음에 오픈할 차례라면
        {
            var nextOpenOrderNumber = DataManager.storyMode.nextOpenOrderNumber;
            bool isNextOpen = _data[0].order_num >= nextOpenOrderNumber;

            if (isNextOpen == true)
            {
                SetActive_Choice(false);
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

        m_data = _data.SortBy(x => x.region_type == DataManager.userInfo.region ? -1 : 0);
        if (_region > RegionType.NONE)
        {
            for (int i = 0; i < m_data.Count; i++)
            {
                if (m_data[i].region_type != _region)
                    m_data.RemoveAt(i--);
            }
        }
        // 하나라 묶지 않고 풀어버릴거야.
        //// 처음꺼는 좀 예외로 둘거야.
        //else if (m_data[0].order_num == 1)
        //{
        //    // 안깬게 있으면 그걸 먼저 보여줄거고, 다 깻으면 그냥 내 국가꺼 보여줄거야.
        //    for (int i = 0; i < m_data.Count; i++)
        //    {
        //        if (DataManager.storyMode.IsComplete(m_data[i].node_key) == false)
        //        {
        //            m_curIdx = i;
        //            break;
        //        }
        //    }
        //}

        SetActive_Choice(false);
        m_element.btnChange.gameObject.SetActive(m_data.Count > 1);

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
            bool overLastPlay = m_data[0].order_num > DataManager.storyMode.nextPlayOrderNumber;
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

    public void SetInteractable(bool _interactable)
    {
        m_element.button.interactable = _interactable;
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
            SetActive_Choice(true);
            m_element.txtChoice.text = reqSeq;
        }
        else
            SetActive_Choice(false);

        bool isComplete = DataManager.storyMode.IsComplete(data.node_key);
        m_element.objBadge.SetActive(isComplete);
        if (isComplete)
        {
            if (DataManager.storyMode.nodeKeyNewClear == data.node_key)
                RewardStartAsync().Forget();
        }
    }

    public void SetActive_Choice(bool _isActive)
        => m_element.txtChoice.gameObject.SetActive(_isActive);

    async UniTask OnButtonAsync_Enter()
    {
        if (DataManager.storyMode.isLockUI == true)
            return;

        m_element.button.interactable = false;

        var result = await PopupManager.instance.OpenModalAsync("_입장하시겠습니까?");

        if (result != StatusType.Success)
        {
            m_element.button.interactable = true;
            return;
        }

#if SERVICE_DEV
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
        DataManager.storyMode.isLockUI = true;

        var storyNode = TableManager.storyNode.GetNode(DataManager.storyMode.nodeKeyNewClear);
        DataManager.storyMode.nodeKeyNewClear = null;

        var rt = (RectTransform)m_element.objBadge.transform;

        rt.localScale = Vector3.one * 2;
        await rt.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);

        // 보상이 영웅이라면
        if (storyNode.reward_character.IsActive() == true)
        {
            // 첫번째인데 내 국가가 아니면 군주 추가해줘야해.
            if (storyNode.order_num <= 3 && storyNode.region_type != DataManager.userInfo.region)
            {
                var startHero = storyNode.region_type == RegionType.WU ?
                    CharacterName.SunJian.ToString() :
                    TableManager.region.Get(storyNode.region_type).master;

                storyNode.reward_character = $"{startHero},{storyNode.reward_character}";
            }

            var rewards = storyNode.reward_character.Replace(" ", "").Split(',');

            //일단 보상에 넣어주자
            //배치하다가 꺼버릴수도 있어서
            List<string> newHero = new();
            foreach (var key in rewards)
            {
                DataManager.userInfo.AddHeroSoul(key, 10);
                newHero.Add(key);
            }

            //연출
            for (int i = 0; i < rewards.Length; i++)
            {
                var heroKey = rewards[i];
                await UniTask.WaitForSeconds(.5f, cancellationToken: destroyCancellationToken);

                var heroName = KoreanHelper.AppendJosa(TableManager.stringHero.GetHeroName(heroKey), KoreanHelper.JosaType.EulLeul, "[{0}]");
                PopupManager.instance.AlertShow($"{heroName}_얻었습니다.", 70);

                var heroInfoData = DataManager.userInfo.GetHeroInfoData(heroKey);

                if (newHero.Contains(heroKey) == true)
                {
                    PopupHeroInfo popupHeroInfo = await PopupManager.instance.OpenPopupAsync<PopupHeroInfo>(PopupType.Hero_HeroInfo);
                    popupHeroInfo.AutoCloseAsync(5f).Forget();
                    heroInfoData = new(heroKey, GradeType.Normal);

                    await popupHeroInfo.SetHeroInfoDataAsync(heroInfoData, true, true);

                    await UniTask.WaitUntil(() => popupHeroInfo == null, cancellationToken: destroyCancellationToken);
                }
                else
                {
                    await PopupManager.instance.AlertShowAsync($"[{heroName}]의_영혼석을_획득했습니다.");
                }
            }
        }
        else if (storyNode.reward_currency_type.IsActive())
        {
            if (Enum.TryParse(storyNode.reward_currency_type, out ItemType currency))
            {
                RewardWorker.instance.AddAsset(currency == ItemType.Gold ?
                    storyNode.rewardCurrencyAmount : 0, currency == ItemType.Rice ? storyNode.rewardCurrencyAmount : 0,
                    m_element.objBadge.transform, false);
            }
        }

        DataManager.storyMode.isLockUI = false;

        await UniTask.WaitUntil(() => PopupManager.instance.isAlerting == false, cancellationToken: destroyCancellationToken);
        PopupManager.instance.AlertShow("보상을_모두_수령했습니다.");
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
