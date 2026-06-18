using Cysharp.Threading.Tasks;
using UnityEngine;

public partial class InfoStage_Boss
{

    async UniTask StartBossRaidAsync()
    {
        var dataRaid = DataManager.bossRaid.data;

        var rtTimer = transform.GetComponent<RectTransform>("Timer");
        rtTimer.gameObject.SetActive(true);

        //"여포<color=#6D6D6D><size=80%> 최강무장</size></color>";
        m_element.txtName.text = $"[{TableManager.stringTable.GetGradeType(dataRaid.nowGrade)}] {TableManager.hero.Get(dataRaid.keyBoss).name}";

        var width = -((RectTransform)transform).rect.width;
        var minute = DataManager.bossRaid.timerRunning;
        var dtEnd = dataRaid.dtNextRound.AddMinutes(minute);
        while (true)
        {
            var dtNow = Utils.GetUTC();
            var process = 1 - (dtEnd - dtNow).TotalMinutes / minute;

            var pos = rtTimer.anchoredPosition;
            pos.x = width * (float)process;
            rtTimer.anchoredPosition = pos;

            if (process >= 1)
                break;

            await UniTask.WaitForEndOfFrame();
        }

        rtTimer.gameObject.SetActive(false);
    }
}
