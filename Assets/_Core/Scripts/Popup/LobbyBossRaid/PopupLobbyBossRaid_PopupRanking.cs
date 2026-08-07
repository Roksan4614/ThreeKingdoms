using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PopupLobbyBossRaid_PopupRanking : MonoBehaviour, IValidatable
{
    public enum TabType
    {
        NONE = -1,

        Point,
        PrevRaid,

        Tutorial_Point = 0,
        Tutorial_Win,
        Tutorial_Winning,
    }

    protected TabType m_curTabType = TabType.NONE;

    PopupUserInfoComponent m_popupUserInfo;

    private void Awake()
    {
        m_element.imgPanel.enabled = true;

        m_element.btnClose.onClick.AddListener(OnButton_Close);

        for (var i = 0; i < m_element.tabs.Length; i++)
        {
            var tabType = (TabType)i;
            m_element.tabs[i].onClick.AddListener(() => OnButton_Tab(tabType));
        }
    }

    public bool CloseEscape()
    {
        if (gameObject.activeSelf == true)
        {
            if (m_popupUserInfo?.EscapeClose() == false)
                return false;

            OnButton_Close();
            return false;
        }

        return true;
    }

    public virtual void OpenPopup()
    {
        Utils.SetActivePunch(transform, true);

        m_element.scroll.content.anchoredPosition = Vector2.zero;

        OnButton_Tab(TabType.Point);
    }

    protected void OnButton_Tab(TabType _tabType)
    {
        if (m_curTabType == _tabType)
            return;

        m_curTabType = _tabType;

        for (var i = 0; i < m_element.tabs.Length; i++)
            m_element.tabs[i].SetDrawSelect(i == (int)_tabType);

        SetRankingAsync().Forget();
    }



    protected virtual async UniTask SetRankingAsync()
    {
        await UniTask.NextFrame();

        var rankerData = (m_curTabType == TabType.Point ? DataManager.bossRaid.rankPoint : DataManager.bossRaid.rankPrevRaid);

        // 포인트 랭킹일 경우 내 위아래로 20명씩임.
        if (m_curTabType == TabType.Point)
            rankerData.ranker = GetRankerUserRange(rankerData);
        
        SetScrollRankerData(rankerData);
    }

    protected List<RankerUserData> GetRankerUserRange(RankerData _rankerData, int _range = 20)
    {
        var index = _rankerData.ranker.FindIndex(x => x.uid == _rankerData.my.uid);
        int startIndex = Mathf.Max(0, index - 20);
        int endIndex = Mathf.Min(_rankerData.ranker.Count - 1, index + 20);

        return _rankerData.ranker.GetRange(startIndex, endIndex - startIndex + 1);
    }

    protected virtual void SetScrollRankerData(RankerData _rankerData, bool _isForceFindMe = false)
    {
        // 포디움 세우자
        for (int i = 0; i < 3; i++)
            m_element.podiums[i].SetRankerInfo(m_curTabType, _rankerData.ranker[i], _rankerData => OnButtonAsync_UserInfo(_rankerData).Forget());


        m_element.scroll.Initialize<PopupLobbyBossRaid_PopupRanking_Item>(_rankerData.ranker.Count,
            (_item, _idxData) =>
            {
                _item.SetRankerInfoAsync(m_curTabType, _rankerData.ranker[_idxData], _rankerData => OnButtonAsync_UserInfo(_rankerData).Forget()).Forget();
#if UNITY_EDITOR
                _item.name = _rankerData.ranker[_idxData].rank.ToString();
#endif
            });

        var content = m_element.scroll.content;

        // 유저 찾기
        UnityAction<bool> actionFind = _isTween =>
        {
            for (int i = 0; i < _rankerData.ranker.Count; i++)
            {
                if (_rankerData.ranker[i].uid == _rankerData.my.uid)
                {
                    m_element.scroll.MoveToIndex(i, _isTween);
                    break;
                }
            }
        };

        // 콘텐츠 위치 설정
        if (m_curTabType == TabType.Point || _isForceFindMe == true)
            actionFind(false);
        else
            content.anchoredPosition = Vector2.zero;

        // 내 정보쪽으로 가기
        m_element.myRankInfo.SetRankerInfoAsync(m_curTabType, _rankerData.my, _rankerData => actionFind(true)).Forget();
    }

    Dictionary<int, List<HeroInfoData>> m_dbBatch = new();
    bool m_isOpenUserInfo;
    protected async UniTask OnButtonAsync_UserInfo(RankerUserData _rankerData)
    {
        if (m_isOpenUserInfo == true || _rankerData.uid == DataManager.userInfo.uid)
            return;
        m_isOpenUserInfo = true;

        // TODO: TestUserInfo
        UserInfoData userInfo = new()
        {
            nickname = _rankerData.nickname,
            profileIdx = -1,
            region = Random.Range(0, (int)RegionType.MAX) + RegionType.NONE + 1,
            uid = _rankerData.uid,
            batchHeroes = new(),
            treasures = new(),
            descript = $"즈르라스뜨부이쩨.\n미냐 자붓 [{_rankerData.nickname}]."
        };

        {
            for (int i = 0; i < 3; i++)
                userInfo.treasures.Add(TableManager.treasure.list.Where(x => x.isActive).ToArray().RandomFirst().key);

            if (m_dbBatch.ContainsKey(userInfo.uid))
                userInfo.batchHeroes = m_dbBatch[userInfo.uid];
            else
            {
                List<HeroPositionType> dbPosition = new();
                for (var i = HeroPositionType.NONE + 1; i < HeroPositionType.MAX; i++)
                    dbPosition.Add(i);
                dbPosition = dbPosition.SortBy(x => Random.value);

                var dbHero = TableManager.hero.GetHeroList().SortBy(x => Random.value).ToList();
                for (int i = 0; i < 4; i++)
                {
                    var key = dbHero[i].key;
                    userInfo.batchHeroes.Add(new HeroInfoData(
                        key,
                        _grade: Random.Range(0, (int)GradeType.MAX) + GradeType.NONE + 1,
                        _heroPositionType: dbPosition[i],
                        _skin: key,
                        _enchantLevel: Random.Range(10, 17),
                        _isMine: true,
                        _isMain: i == 0,
                        _relicLevel: Random.Range(50, 100)
                        ));
                }
                m_dbBatch.Add(userInfo.uid, userInfo.batchHeroes);
            }
        }

        m_popupUserInfo = await PopupManager.instance.OpenPopupAsync<PopupUserInfoComponent>(PopupType.UserInfo, userInfo);

        await UniTask.WaitUntil(() => m_popupUserInfo.statusType != StatusType.Wait, cancellationToken: destroyCancellationToken);

        m_popupUserInfo = null;
        m_isOpenUserInfo = false;
    }

    protected virtual void OnButton_Close()
    {
        Utils.SetActivePunch(transform, false);
    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    protected ElementData m_element;

    [System.Serializable]
    protected struct ElementData
    {
        public Image imgPanel;

        public Button btnClose;
        public TextMeshProUGUI txtTitle;
        public TextMeshProUGUI txtPoint;

        public ButtonHelper[] tabs;

        public LoopScrollHelper scroll;

        public PopupLobbyBossRaid_PopupRanking_PodiumItem[] podiums;
        public PopupLobbyBossRaid_PopupRanking_Item baseRanker;
        public PopupLobbyBossRaid_PopupRanking_Item myRankInfo;

        public void Initialize(Transform _transform)
        {
            imgPanel = _transform.GetComponent<Image>();

            btnClose = _transform.GetComponent<Button>("Top/btn_back");
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Top/txt_title");
            txtPoint = _transform.GetComponent<TextMeshProUGUI>("txt_point");

            tabs = _transform.Find("Tab").GetComponentsInChildren<ButtonHelper>();

            scroll = _transform.GetComponent<LoopScrollHelper>("Scroll");
            podiums = _transform.Find("Podium").GetComponentsInChildren<PopupLobbyBossRaid_PopupRanking_PodiumItem>();
            baseRanker = scroll.content.GetChild(0).GetComponent<PopupLobbyBossRaid_PopupRanking_Item>();
            myRankInfo = scroll.transform.GetComponent<PopupLobbyBossRaid_PopupRanking_Item>("MyInfo");
        }
    }
    #endregion VALIDATE

}
