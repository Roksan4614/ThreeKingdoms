using Cysharp.Threading.Tasks;
using UnityEngine;

public class PopupTournament_Ranking : PopupLobbyBossRaid_PopupRanking
{
    bool m_isClose;
    public async UniTask OpenPopupAsync()
    {
        Utils.SetActivePunch(transform, true);

        m_element.scroll.content.anchoredPosition = Vector2.zero;

        await UniTask.WaitUntil(() => m_isClose == true);
    }

    protected override void OnButton_Close()
    {
        m_isClose = true;
        Utils.SetActivePunch(transform, false, _callback:()=> m_isClose = false);
    }

    #region VALIDATE
    public override void OnManualValidate()
    {
        base.OnManualValidate();
        m_elementTournament.Initialize(transform);
    }

    [SerializeField, HideInInspector]
    ElementDataTournament m_elementTournament;

    [System.Serializable]
    struct ElementDataTournament
    {
        public void Initialize(Transform _transform)
        {
        }
    }
    #endregion VALIDATE

}
