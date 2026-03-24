using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scenario_1_1_1_SHU_START : ScenarioBase
{
    protected override async UniTask StartAsync(string _stageKey)
    {
        ControllerManager.instance.gameObject.SetActive(false);
        await PopupManager.instance.ShowDimmAsync(false);

        int idxTalk = 0;

        //장비	관우 형님, 첫 출전인데 살살하쇼ㅋㅋ
        var talkData = m_dbTalk[idxTalk++];
        var hero = GetHeroComp(talkData);

        hero.anim.PlayAttack(true, true);
        await UniTask.WaitForSeconds(.3f);
        hero.anim.PlayAttack(true, true);
        await UniTask.WaitForSeconds(.5f);

        await hero.talkbox.StartAsyncClickDisable(talkData.talkArray);

        //관우	으휴.. 넌 힘 조절이나 잘 하거라.
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //장비	ㅋㅋㅋ
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //장비	그런데 큰형님. 대체 돈은 어디서 난거요?
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //관우	그러게 말입니다. 없는 살림에 너무 무리하시는건 아니신지..
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //질문창 띄워주자
        {
            // 한왕실의 재건을 위해 하는 일이니 그런소리 마시게.
            // 뭔 소리야. 이 지역 돗자리는 내가 다 납품하고 있구만.
            var resultIdx = await PopupManager.instance.OpenTalkSelectAsync(
                m_dbTalk[idxTalk++].message,
                m_dbTalk[idxTalk++].message);

            Queue<TableStringData> talkQueston = new(
                TableManager.scenarioTalk.GetTalkAfterQuestion(_stageKey, resultIdx));

            while (talkQueston.Count > 0)
                await StartTalkAsync(talkQueston.Dequeue());
        }

        ////유비	뭔 소리야. 이 지역 돗자리는 내가 다 납품하고 있구만.
        //await StartTalkAsync(m_dbTalk[idxTalk++]);

        ////관우	..?
        //talkData = m_dbTalk[idxTalk++];
        //hero = GetHeroComp(talkData);

        //hero.talkbox.Start(talkData.talkArray);

        //await UniTask.WaitForSeconds(0.5f);

        ////장비	그렇다네요 형님ㅋㅋ
        //await StartTalkAsync(m_dbTalk[idxTalk++]);
        //hero.talkbox.SetActive(false);

        //유비	자아, 집중하자! 출진이다!!
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //장비	오오!!
        talkData = m_dbTalk[idxTalk++];
        hero = GetHeroComp(talkData);

        hero.talkbox.Start(talkData.talkArray);
        hero.anim.PlayAttack(true);

        //관우	오오!!
        talkData = m_dbTalk[idxTalk++];
        var hero2 = GetHeroComp(talkData);

        hero2.anim.PlayAttack(true, true);
        await hero2.talkbox.StartAsync(talkData.talkArray);
        await UniTask.WaitForSeconds(0.5f);

        await UniTask.WaitUntil(() => ControllerManager.isClick);

        hero.talkbox.SetActive(false);
        hero2.talkbox.SetActive(false);

        ControllerManager.instance.gameObject.SetActive(true);
    }
}
