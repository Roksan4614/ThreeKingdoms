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
        m_element.button.onClick.AddListener(() =>
        {
            PopupManager.instance.OpenPopup(PopupType.LobbyBossRaid).Forget();
        });

        TimerAsync().Forget();
    }

    async UniTask TimerAsync()
    {
        m_cts = m_cts.Release(true);
        var token = m_cts.Token;

        var dataRaid = DataManager.bossRaid.data;

        if (dataRaid.tickNextRound == 0)
        {
            m_element.txtTimer.text = "_미출현_";
            await UniTask.WaitUntil(() => DataManager.bossRaid.data.tickNextRound > 0, cancellationToken: destroyCancellationToken);
        }

        var dtEnd = DataManager.bossRaid.data.dtNextRound.AddSeconds(Configure.instance.timeGapFromServer);
        while (true)
        {
            var ts = dtEnd - DateTime.UtcNow;

            if (ts.TotalSeconds <= 0)
                break;

            m_element.txtTimer.text = $"({ts.ToRemainTime(20, _isStartMinute: true)})";
            await UniTask.WaitForEndOfFrame(cancellationToken: destroyCancellationToken);
        }

        m_element.txtTimer.text = "진행중";

        await UniTask.WaitUntil(() => DataManager.bossRaid.data.tickNextRound == 0, cancellationToken: destroyCancellationToken);

        TimerAsync().Forget();
    }

    private void OnDestroy()
        => m_cts = m_cts.Release();

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public TextMeshProUGUI txtTimer;
        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>();
            txtTimer = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_timer");
        }
    }
    #endregion VALIDATE

}
