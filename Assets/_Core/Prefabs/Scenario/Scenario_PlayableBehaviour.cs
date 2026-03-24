using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class Scenario_PlayableBehaviour : PlayableBehaviour
{
    Scenario_PlayableAsset m_asset;
    ScenarioBase m_scenario;
    CharacterComponent m_hero;

    public bool isPlay = false;

    public void Initialize(Scenario_PlayableAsset _asset, ScenarioBase _scenario, CharacterComponent _hero)
    {
        m_asset = _asset;
        m_scenario = _scenario;
        m_hero = _hero;
    }

    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        if (m_asset == null || m_scenario == null)
        {
            Debug.Log($"{m_asset == null}/{m_scenario == null}");
            return;
        }

        if (m_asset.isFinished == true)
            return;

        if (m_hero == null || m_hero.talkbox == null)
        {
            if (m_asset.directionType > DirectionType.NONE)
            {
                if ((m_asset.directionType == DirectionType.Right) == m_hero.element.panel.localScale.x > 0)
                {
                    var scale = m_hero.element.panel.localScale;
                    scale.x *= -1;
                    m_hero.element.panel.localScale = scale;
                }
            }

            if (m_asset.talk.IsActive())
            {
                m_hero.element.txtTalk.text = m_asset.talk;
                m_hero.element.txtTalk.transform.parent.gameObject.SetActive(true);
                TalkboxForceRebuild();
            }
            return;
        }

        isPlay = true;
        if (m_asset.isAnimation == true && m_asset.animationType > CharacterAnimType.NONE)
            m_hero.anim.Play(m_asset.animationType);

        if (m_asset.talk.IsActive())
            m_scenario.Playable_TalkStart(m_hero, m_asset.talk);

        if (m_asset.directionType > DirectionType.NONE)
            m_hero.move.SetFlip(m_asset.directionType == DirectionType.Right);
    }

    public override void OnBehaviourPause(Playable playable, FrameData info)
    {
        if (m_asset.isFinished == true)
        {
            m_scenario.Playable_TimelineFinished();
            return;
        }

        if (isPlay == false)
        {
            if (m_asset.talk.IsActive())
                m_hero.element.txtTalk.transform.parent.gameObject.SetActive(false);

            return;
        }

        if (m_asset.isAnimation == true && m_asset.animationType > CharacterAnimType.NONE)
            m_hero.anim.Play(CharacterAnimType.Idle);

        if (m_asset.talk.IsActive())
            m_scenario.Playable_TalkEndAsync(m_hero).Forget();
    }

    void TalkboxForceRebuild()
    {
        var rtTalkbox = (RectTransform)m_hero.element.txtTalk.transform.parent;
        var layout = rtTalkbox.GetComponent<HorizontalLayoutGroup>();
        var fitter = rtTalkbox.GetComponent<ContentSizeFitter>();

        layout.enabled = fitter.enabled = true;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        rtTalkbox.ForceRebuildLayout();

        if (rtTalkbox.rect.width > 1000)
        {
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var size = rtTalkbox.sizeDelta;
            size.x = 1000;
            rtTalkbox.sizeDelta = size;

            rtTalkbox.ForceRebuildLayout();
        }

        layout.enabled = fitter.enabled = false;
    }

}
