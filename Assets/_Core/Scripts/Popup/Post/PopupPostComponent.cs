using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Post
{
    public class PopupPostComponent : BasePopupComponent
    {
        PopupPostComponent() : base(PopupType.Post)
        {
        }

        private void Start()
        {
            LoadDataAsync().Forget();
            Utils.WaitEscape(this, () =>
            {
                Close();
            });
        }

        bool m_isStarted = false;
        async UniTask LoadDataAsync()
        {
            if (PostWorker.isReady == false)
            {
                m_element.scroll.content.gameObject.SetActive(false);
                m_element.txtEmpty.text = "불러오는_중";

                await UniTask.WaitUntil(() => PostWorker.isReady == true);

                m_element.txtEmpty.text = "우편함이_비었습니다.";
                m_element.scroll.content.gameObject.SetActive(true);
            }

            m_isStarted = true;

            var posts = PostWorker.data;
            var content = m_element.scroll.content;
            int i = 0;
            for (; i < posts.Count; i++)
            {
                bool isNew = i == content.childCount;
                var slot = (isNew ? Instantiate(content.GetChild(0), content) : content.GetChild(i)).GetComponent<PopupPost_Slot>();
                slot.gameObject.SetActive(true);
                slot.SetPostData(posts[i]);
            }
            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);
            content.ForceRebuildLayout();

            m_element.txtEmpty.gameObject.SetActive(i == 0);
        }

        public override void OpenPopup(params object[] _args)
        {
            gameObject.SetActive(true);
            Utils.SetActivePunch(m_element.panel, true);

            if (m_isStarted == true && PostWorker.isRedDot == true)
                LoadDataAsync().Forget();

            m_element.scroll.content.anchoredPosition = Vector2.zero;
        }

        public override void Close()
        {
            Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));
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

            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
                txtEmpty = scroll.viewport.GetComponent<TextMeshProUGUI>("txt_empty");
            }
            public Transform panel => scroll.transform.parent;
        }
        #endregion VALIDATE

    }
}