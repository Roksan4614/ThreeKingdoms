using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;

public class Banner_BossRaid : MonoBehaviour, IValidatable
{
    CancellationTokenSource m_cts;

    void Start()
    {
        m_element.button.onClick.AddListener(() => OnButtonAsync_OpenPopup().Forget());

        TimerAsync().Forget();
    }

    async UniTask OnButtonAsync_OpenPopup()
    {
        m_element.button.interactable = false;
        await PopupManager.instance.OpenPopupAndWait(PopupType.LobbyBossRaid);
        m_element.button.interactable = true;
    }

    async UniTask TimerAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        var dataRaid = DataManager.bossRaid.data;

        if (dataRaid.tickNextRound == 0)
        {
            m_element.button.text = "_미출현_";
            await UniTask.WaitUntil(() => DataManager.bossRaid.data.tickNextRound > 0, cancellationToken: destroyCancellationToken);
        }

        var dtEnd = DataManager.bossRaid.data.dtNextRound.AddSeconds(Configure.instance.timeGapFromServer);
        while (true)
        {
            var ts = dtEnd - DateTime.UtcNow;

            if (ts.TotalSeconds <= 0)
                break;

            m_element.button.text = $"({ts.ToRemainTime(20, _isStartMinute: true)})";
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        }

        m_element.button.text = "진행중";

        await UniTask.WaitUntil(() => DataManager.bossRaid.data.tickNextRound == 0, cancellationToken: destroyCancellationToken);

        TimerAsync().Forget();
    }

    private void OnDestroy()
        => m_cts = m_cts.ReleaseCTS();

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>();
        }
    }
    #endregion VALIDATE

}
