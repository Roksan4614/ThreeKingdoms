using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;

public class Scenario_1_1_1_SHU_START : ScenarioBase
{
    public override async UniTask StartAsync(string _stageKey)
    {
        await TutorialAsync();

        await UniTask.WaitUntil(() => Input.GetKey(KeyCode.M));
    }

    async UniTask TutorialAsync()
    {
        ControllerManager.instance.enabled = false;

        var talk = TableManager.scenarioTalk.GetTalk("1.1.1_TUTORIAL", true);
        var mainHero = TeamManager.instance.mainHero;

        var enemy = m_elementBase.enemy.First();
        enemy.gameObject.SetActive(false);

        mainHero.buff.Add(BuffType.DEBUFF_NO_SKILL);

        await UniTask.WaitForSeconds(1f);

        // 음??
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        enemy.gameObject.SetActive(true);
        enemy.anim.Play(CharacterAnimType.Attack);
        enemy.SetHeroData("");
        CameraManager.instance.SetCameraPosTarget(enemy.element.cameraPos, false);

        //얼빠지게 생긴 넘이다!! 죽여라!!
        await enemy.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);

        //"앞에 황건적이네. 어쩌지?"
        CameraManager.instance.SetCameraPosTarget(mainHero.element.cameraPos, false);
        await mainHero.talkbox.StartAsyncClickDisable(talk.Dequeue().talkArray);
        enemy.talkbox.SetActive(false);

        //"응? 적이 있으면 스스로 공격하는구나!!"
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        mainHero.move.MoveTarget(m_elementBase.enemy.First(), true);
        enemy.move.MoveTarget(mainHero, true);

        // 잡을 때까지 기다리기
        await UniTask.WaitUntil(() => enemy.isLive == false);

        ControllerManager.instance.enabled = true;
        //"키보드 조작으로 내가 움직일 수 있을거 같은데?"
        mainHero.talkbox.Start(talk.Dequeue().talkArray);

        var prevPos = mainHero.transform.position;
        await UniTask.WaitUntil(() => (prevPos - mainHero.transform.position).sqrMagnitude > 2f);

        //"돌진과 공격을 사용해보자"
        await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);

        bool isAttack = false, isDash = false;
        while (isAttack == false || isDash == false)
        {
            if (isAttack == false)
                isAttack = mainHero.attack.isAttack;
            if (isDash == false)
                isDash = mainHero.move.isDash;

            await UniTask.WaitForEndOfFrame();
        }

        enemy.target.SetTarget(null);
        enemy.Respawn(false);
        enemy.move.MoveTarget(mainHero, true);
        mainHero.move.MoveTarget(enemy, true);

        var hashHero = mainHero.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);
        var hashEnemy = enemy.buff.Add(BuffType.BUFF_NO_TAKEN_DAMAGE);

        mainHero.buff.RemoveAll(BuffType.DEBUFF_NO_SKILL);
        //영웅 스킬 사용
        await mainHero.talkbox.StartAsync(talk.Dequeue().talkArray);
        await UniTask.WaitUntil(() => mainHero.attack.isUseSkill);
        mainHero.talkbox.SetActive(false);

        mainHero.buff.Remove(hashHero.hash);

        mainHero.buff.RemoveAll();
        enemy.buff.RemoveAll();

        await UniTask.WaitUntil(() => enemy.isLive == false);

        //"연회권? 동료를 얻을 수 있으려나? 주막에 가보자."

        await mainHero.talkbox.StartAsyncAutoDisable(3f, destroyCancellationToken, talk.Dequeue().talkArray);

    }
}
