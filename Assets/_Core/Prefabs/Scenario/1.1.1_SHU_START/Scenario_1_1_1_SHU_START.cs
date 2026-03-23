using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class Scenario_1_1_1_SHU_START : ScenarioBase
{
    protected override async UniTask StartAsync()
    {
        await PopupManager.instance.ShowDimmAsync(false);

        int idxTalk = 0;

        //장비	관우 형님, 첫 출전인데 살살하쇼ㅋㅋ
        var talkData = m_dbTalk[idxTalk++];
        var hero = GetHeroComp(talkData);

        hero.anim.PlayAttack();
        await UniTask.WaitForSeconds(.5f);
        hero.anim.PlayAttack();
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

        //유비	뭔 소리야. 이 지역 돗자리는 내가 다 납품하고 있구만.
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //관우	..?
        talkData = m_dbTalk[idxTalk++];
        hero = GetHeroComp(talkData);

        hero.talkbox.Start(talkData.talkArray);

        await UniTask.WaitForSeconds(0.5f);

        //장비	그렇다네요 형님ㅋㅋ
        await StartTalkAsync(m_dbTalk[idxTalk++]);
        hero.talkbox.SetActive(false);

        //유비	자아, 집중하자! 출진이다!!
        await StartTalkAsync(m_dbTalk[idxTalk++]);

        //장비	오오!!
        talkData = m_dbTalk[idxTalk++];
        hero = GetHeroComp(talkData);

        hero.talkbox.Start(talkData.talkArray);
        hero.anim.PlayAttack();

        //관우	오오!!
        talkData = m_dbTalk[idxTalk++];
        var hero2 = GetHeroComp(talkData);

        hero2.talkbox.Start(talkData.talkArray);
        hero2.anim.PlayAttack();

        await UniTask.WaitForSeconds(.5f);
        await PopupManager.instance.ShowDimmAsync(true, _duration: 1f);

        hero.talkbox.SetActive(false);
        hero2.talkbox.SetActive(false);
    }
}
