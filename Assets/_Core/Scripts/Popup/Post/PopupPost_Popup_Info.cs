using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Post
{
    public class PopupPost_Popup_Info : MonoBehaviour, IValidatable
    {
        public PostInfoData postData { get; private set; }
        public Action<PopupPost_Slot> clickReceive { get; set; }
        public Action<PopupPost_Slot> clickDelete { get; set; }

        bool isReceiveRewards => postData.rewards.Count == 0 || postData.isReceiveReward == true;
        StatusType m_status;
        CancellationTokenSource m_cts;

        private void Awake()
        {
            transform.GetComponent<Button>("Dimm").onClick.AddListener(Close);
            transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);

            m_element.btnConfirm.onClick.AddListener(() => OnButtonAsync_Receive().Forget());
            m_element.btnDelete.onClick.AddListener(() => OnButtonAsync_Delete().Forget());
        }

        private void OnDisable()
        {
            m_cts = m_cts.ReleaseCTS();
        }

        public async UniTask<bool> OpenAsync(PostInfoData _postData)
        {
            gameObject.SetActive(true);

            postData = _postData;
            Utils.SetActivePunch(m_element.panel, true);

            m_element.txtPostTitle.text = _postData.title;

            if (_postData.content.IsActive() == true)
            {
                m_element.txtContent.text = _postData.content;
                var p = m_element.txtContent.transform.parent;
                p.gameObject.SetActive(true);
                p.ForceRebuildLayout();
            }
            else
                m_element.txtContent.transform.parent.gameObject.SetActive(false);

            if (_postData.rewards.Count > 0)
            {
                m_element.rewards.gameObject.SetActive(true);

                var content = m_element.rewards.content;
                int i = 0;
                for (; i < _postData.rewards.Count; i++)
                {
                    var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i)).GetComponent<ItemComponent>();
                    slot.gameObject.SetActive(true);
                    slot.SetItemData(_postData.rewards[i]);
                }

                for (; i < content.childCount; i++)
                    content.GetChild(i).gameObject.SetActive(false);
            }
            else
                m_element.rewards.gameObject.SetActive(false);

            m_element.btnConfirm.text = isReceiveRewards ? "_확인_" : "_받기_";
            m_element.panel.ForceRebuildLayout();

            TimerAsync().Forget();

            m_status = StatusType.Wait;
            await UniTask.WaitUntil(() => m_status != StatusType.Wait);

            return m_status == StatusType.Success;
        }

        async UniTask TimerAsync()
        {
            m_cts = m_cts.ReleaseCTS(true);
            var token = m_cts.Token;

            if (postData.tick_end > 0)
            {
                m_element.txtTimer.transform.parent.gameObject.SetActive(true);

                var DateTime = Utils.GetDateTime(postData.tick_end);
                TimeSpan ts = DateTime - Utils.GetUTC();

                while (ts.TotalSeconds > 0)
                {
                    m_element.txtTimer.text = ts.ToRemainTime(22);
                    await UniTask.NextFrame(token);
                    ts = DateTime - Utils.GetUTC();
                }

                Close();
            }
            else
                m_element.txtTimer.transform.parent.gameObject.SetActive(false);

            m_cts = null;
        }

        async UniTask OnButtonAsync_Receive()
        {
            if (ThreeKingdoms.Client.Server.PrototypeContentNotice.ShowIfServer()) return;
            if (isReceiveRewards == false && await PostWorker.instance.API_ReceivePost(postData) == true)
                m_status = StatusType.Success;

            Close();
        }

        async UniTask OnButtonAsync_Delete()
        {
            if (postData.rewards.Count > 0 && postData.isReceiveReward == false)
            {
                PopupManager.instance.AlertShow("아직_받을_보상이_있습니다.");
                return;
            }

            if (await PostWorker.instance.API_DeletePost(postData) == true)
            {
                m_status = StatusType.Success;
                Close();
            }
        }

        public void Close()
        {
            Utils.SetActivePunch(m_element.panel, false, _callback: () =>
            {
                if (m_status == StatusType.Wait)
                    m_status = StatusType.Cancel;

                gameObject.SetActive(false);
            });
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public TextMeshProUGUI txtPostTitle;
            public TextMeshProUGUI txtContent;
            public TextMeshProUGUI txtTimer;

            public ButtonHelper btnConfirm;
            public ButtonHelper btnDelete;

            public ScrollRect rewards;

            public void Initialize(Transform _transform)
            {
                txtPostTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_post_title");
                txtContent = _transform.GetComponent<TextMeshProUGUI>("Panel/Content/Text");
                txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_title/Timer/Text");

                btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_confirm");
                btnDelete = _transform.GetComponent<ButtonHelper>("Panel/Button/btn_delete");

                rewards = _transform.GetComponent<ScrollRect>("Panel/Reward");
            }

            public Transform panel => txtPostTitle.transform.parent;
        }
        #endregion VALIDATE

    }
}