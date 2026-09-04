using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Post
{
    public class PopupPost_Slot : MonoBehaviour, IValidatable
    {
        public PostInfoData postData { get; private set; }
        public Button.ButtonClickedEvent onClick => m_element.btnReceive.onClick;

        public void SetPostData(PostInfoData _postData)
        {
            m_element.txtTitle.text = _postData.title ?? "-";
            postData = _postData;

            TimerAsync().Forget();

            int i = 0;
            var content = m_element.scroll.content;
            for (; i < _postData.rewards.Count; i++)
            {
                var slot = (i == content.childCount ? Instantiate(content.GetChild(0), content) : content.GetChild(i)).GetComponent<ItemComponent>();
                slot.gameObject.SetActive(true);
                slot.SetItemData(_postData.rewards[i]);
            }

            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.anchoredPosition = Vector2.zero;
        }

        void OnDisable()
        {
            m_cts = m_cts.ReleaseCTS();
        }

        CancellationTokenSource m_cts;
        async UniTask TimerAsync()
        {
            m_cts = m_cts.ReleaseCTS(true);
            var token = m_cts.Token;

            if (postData.tick_end > 0)
            {
                var DateTime = Utils.GetDateTime(postData.tick_end);
                TimeSpan ts = DateTime - Utils.GetUTC();

                while (ts.TotalSeconds > 0)
                {
                    m_element.txtTimer.text = ts.ToRemainTime(22);
                    await UniTask.NextFrame(token);
                    ts = DateTime - Utils.GetUTC();
                }

                gameObject.SetActive(false);
            }
            else
                m_element.txtTimer.text = "";

            m_cts = null;
        }

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        //[SerializeField, HideInInspector]
        [SerializeField]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public TextMeshProUGUI txtTitle;
            public TextMeshProUGUI txtTimer;
            public ScrollRect scroll;
            public ButtonHelper btnReceive;

            public void Initialize(Transform _transform)
            {
                txtTitle = _transform.GetComponent<TextMeshProUGUI>("Title/Text");
                txtTimer = _transform.GetComponent<TextMeshProUGUI>("txt_timer");
                scroll = _transform.GetComponent<ScrollRect>("Scroll");
                btnReceive = _transform.GetComponent<ButtonHelper>("btn_receive");


            }
        }
        #endregion VALIDATE

    }
}