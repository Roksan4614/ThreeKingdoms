using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Post
{
    public class PopupPostComponent : BasePopupComponent
    {
        PopupPostComponent() : base(PopupType.Post) { }

        private void Start()
        {
            for (var i = 0; i < m_element.popup.childCount; i++)
                m_element.popup.GetChild(i).gameObject.SetActive(false);

            Utils.WaitEscape(this, () =>
            {
                if (m_element.popupInfo.gameObject.activeSelf == true)
                {
                    m_element.popupInfo.Close();
                    return;
                }
                Close();
            }, _isMenuPopup: true);

            m_element.btnReceiveAll.onClick.AddListener(() => OnButtonAsync_ReceiveAll().Forget());
            m_element.btnDeleteAll.onClick.AddListener(() => OnButtonAsync_DeleteAll().Forget());

            LoadDataAsync().Forget();

            // setlocalization
            {
                m_element.btnReceiveAll.text = "일괄_받기";
                m_element.btnDeleteAll.text = "일괄_삭제";
            }
        }

        public override void OpenPopup(params object[] _args)
        {
            gameObject.SetActive(true);
            Utils.SetActivePunch(m_element.panel, true);

            if (PostWorker.isRedDot == true)
                LoadDataAsync().Forget();

            m_element.scroll.content.anchoredPosition = Vector2.zero;
        }

        async UniTask LoadDataAsync()
        {
            var posts = PostWorker.data;
            var content = m_element.scroll.content;
            int i = 0;
            for (; i < posts.Count; i++)
            {
                bool isNew = i == content.childCount;
                var slot = (isNew ? Instantiate(content.GetChild(0), content) : content.GetChild(i)).GetComponent<PopupPost_Slot>();
                slot.gameObject.SetActive(true);
                slot.SetPostData(posts[i]);

                if (slot.click == null)
                {
                    slot.click = _slot => OnButtonAsync_OpenInfo(_slot).Forget();
                    slot.clickConfirm = _slot => OnButtonAsync_Confirm(_slot).Forget();
                }
            }

            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.ForceRebuildLayout();

            m_element.txtEmpty.gameObject.SetActive(i == 0);
        }

        public override void Close()
        {
            PostWorker.instance.SetReddotRefresh_ClosePost();
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
        }

        async UniTask OnButtonAsync_OpenInfo(PopupPost_Slot _slot)
        {
            //await Utils.SetActivePunchAsync(m_element.panel, false);
            bool isUpdated = await m_element.popupInfo.OpenAsync(_slot.postData);

            if (isUpdated)
            {
                var postData = PostWorker.instance.GetPostData(_slot.postData.index);

                if (postData == null)
                    _slot.gameObject.SetActive(false);
                else
                    _slot.SetPostData(postData);
            }
            else
                _slot.TimerAsync().Forget();

            //Utils.SetActivePunch(m_element.panel, true);
        }

        async UniTask OnButtonAsync_Confirm(PopupPost_Slot _slot)
        {
            if (ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
            // 아이템을 이미 받았다면 팝업을 열어주고, 아니면 받기가 실행되도록 하자            
            if (_slot.isReceivedRewards)
            {
                OnButtonAsync_OpenInfo(_slot).Forget();
                return;
            }

            if (await PostWorker.instance.API_ReceivePost(_slot.postData))
                _slot.SetPostData(PostWorker.instance.GetPostData(_slot.postData.index));
        }

        async UniTask OnButtonAsync_ReceiveAll()
        {
            if (ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
            if (await PostWorker.instance.API_ReceivePost() == true)
                await LoadDataAsync();
        }

        async UniTask OnButtonAsync_DeleteAll()
        {
            if (await PostWorker.instance.API_DeletePost() == true)
                await LoadDataAsync();

        }

        #region VALIDATE
        public override void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ScrollRect scroll;
            public TextMeshProUGUI txtEmpty;

            public ButtonHelper btnReceiveAll;
            public ButtonHelper btnDeleteAll;

            public PopupPost_Popup_Info popupInfo;
            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
                txtEmpty = scroll.viewport.GetComponent<TextMeshProUGUI>("txt_empty");

                btnReceiveAll = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
                btnDeleteAll = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_delete");

                popupInfo = _transform.GetComponent<PopupPost_Popup_Info>("Popup/Info");
            }

            public Transform panel => scroll.transform.parent;
            public Transform popup => popupInfo.transform.parent;
        }
        #endregion VALIDATE

    }
}