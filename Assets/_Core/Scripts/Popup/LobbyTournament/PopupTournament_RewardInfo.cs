using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Tournament
{
    public class PopupTournament_RewardInfo : MonoBehaviour, IValidatable
    {
        private void Awake()
        {
            transform.GetComponent<Button>("Panel/btn_close").onClick.AddListener(Close);
            transform.GetComponent<Button>("Panel/btn_confirm").onClick.AddListener(Close);
            transform.GetComponent<Button>().onClick.AddListener(Close);

            transform.GetComponent<TextMeshProUGUI>("Panel/txt_title").text = "보상_정보";
        }

        private void Start()
        {
            var db = TableManager.tournamentReward.list;

            var content = m_element.scroll.content;
            for (int i = 1; i < db.Count; i++)
                Instantiate(content.GetChild(0), content);

            for (int i = 0; i < db.Count; i++)
            {
                content.GetChild(i).GetComponent<PopupTournament_RewardInfo_Slot>()
                    .SetRewardData(db[i]);
            }

            content.ForceRebuildLayout();
        }

        public async UniTask OpenAsync()
        {
            gameObject.SetActive(true);
            Utils.SetActivePunch(m_element.panel, true);

            m_element.scroll.content.anchoredPosition = m_element.scroll.velocity = Vector2.zero;

            await UniTask.WaitUntil(() => gameObject.activeSelf == false);
        }

        public bool CloseEscape()
        {
            if (gameObject.activeSelf == true)
            {
                Close();
                return false;
            }

            return true;
        }

        void Close()
            => Utils.SetActivePunch(m_element.panel, false, _callback: () => gameObject.SetActive(false));

        #region VALIDATE
        public void OnManualValidate() => m_element.Initialize(transform);

        [SerializeField, HideInInspector]
        ElementData m_element;

        [System.Serializable]
        struct ElementData
        {
            public ScrollRect scroll;

            public void Initialize(Transform _transform)
            {
                scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
            }

            public Transform panel => scroll.transform.parent;
        }
        #endregion VALIDATE

    }

}