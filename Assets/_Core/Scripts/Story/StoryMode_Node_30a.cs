using Cysharp.Threading.Tasks;
using UnityEngine;

public class StoryMode_Node_30a : StoryModeBaseComponent
{
    protected async override UniTask StartAsync()
    {
        await Phase_First();

        // TEST fist 넘기려고
        //while (m_queTalk.Peek().target != CharacterName.CaoCao.ToString())
        //    m_queTalk.Dequeue();

        SetNextPhase();

        await Phase_Second();
    }

    async UniTask Phase_First()
    {
        var token = m_cts.Token;

        var liuBei = mainHero;
        var guanYu = phase.GetHero(CharacterName.GuanYu);
        var zhangFei = phase.GetHero(CharacterName.ZhangFei);

        CameraManager.instance.SetCameraPosTarget(liuBei.element.cameraPos);

        //hero.anim.PlayAttack(true, true);
        //await UniTask.WaitForSeconds(.3f);
        //hero.anim.PlayAttack(true, true);
        //await UniTask.WaitForSeconds(.5f);
    }
    async UniTask Phase_Second() { }
}
