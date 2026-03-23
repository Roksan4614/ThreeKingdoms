using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

public class Scenario_PlayableAsset : PlayableAsset
{
    [SerializeField] ExposedReference<ScenarioBase> m_scenario;
    [SerializeField] ExposedReference<CharacterComponent> m_hero;

    public bool isFinished;

    [Header("Data")]
    public bool isAnimation = false;
    public CharacterAnimType animationType;
    public DirectionType directionType;

    public string talk;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<Scenario_PlayableBehaviour>.Create(graph);
        var behaviour = playable.GetBehaviour();

        behaviour.Initialize(this, m_scenario.Resolve(graph.GetResolver()), m_hero.Resolve(graph.GetResolver()));

        return playable;
    }
}
