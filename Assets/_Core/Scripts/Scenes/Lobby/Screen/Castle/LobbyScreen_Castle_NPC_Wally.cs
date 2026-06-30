using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyScreen_Castle_NPC_Wally : MonoBehaviour, IValidatable, IPointerDownHandler
{
    LobbyScreen_Castle m_castle;
    CancellationTokenSource m_cts;

    float m_percentCatch;
    bool m_isShow;

    private void Awake()
    {
        m_castle = transform.GetComponentInParent<LobbyScreen_Castle>();
    }

    private void OnDisable()
    {
        Release_CTS();
    }

    //private void OnEnable()
    //{
    //    if (m_cts == null && )
    //        SpawnStartAsync().Forget();
    //}

    public void OnPointerDown(PointerEventData _eventData)
    {
        if (m_isShow == false)
            return;

        Release_CTS();
        if (UnityEngine.Random.value < m_percentCatch)
        {
            m_element.anim.Play("Castle_Wally_Hit");
            DataManager.castle.HitWally();
        }
        else
        {
            m_percentCatch *= 1.5f;
            // 도망
            m_element.anim.Play("Castle_Wally_End");
            Utils.AfterSecond(() =>
            {
                if (gameObject.activeInHierarchy == false)
                    return;
                SpawnStartAsync(true).Forget();
            }, UnityEngine.Random.Range(1f, 1.5f));
        }
    }

    public async UniTask SpawnStartAsync(bool _isAgain = false)
    {
        if (_isAgain == false)
            m_percentCatch = .1f;

        Release_CTS();
        m_cts = new();
        var token = m_cts.Token;

        gameObject.SetActive(true);
        while (true)
        {
            m_element.anim.Play("Castle_Wally_On");

            var posRandom = m_castle.GetWallyPointRandom();

            transform.SetParent(posRandom);
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localEulerAngles = Vector3.zero;

            m_isShow = true;
            DataManager.castle.SetWallyUISpawn(true);

            var rnd = UnityEngine.Random.value;
            var count = rnd < .2f ? 1 : rnd < .8f ? 2 : 3;

            await UniTask.WaitForSeconds(1.5f * count, cancellationToken: token);
            DataManager.castle.SetWallyUISpawn(false);

            m_isShow = false;
            m_element.anim.Play("Castle_Wally_Off");
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f), cancellationToken: token);
        }
    }

    void Release_CTS()
    {
        m_cts = m_cts.ReleaseCTS();
        DataManager.castle.SetWallyUISpawn(false);
    }

    //float GetSpawnDuration()
    //{
    //    DataManager.castle.GetCaslteData(CastleObjectType.Gate);

    //    DataManager.castle.GetGatePublicOrderRate();
    //}

    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Animator anim;
        public float idleDuration;

        public void Initialize(Transform _transform)
        {
            anim = _transform.GetComponent<Animator>();

            // 애니메이터에 등록된 모든 애니메이션 클립 가져오기
            AnimationClip[] clips = anim.runtimeAnimatorController.animationClips;

            foreach (AnimationClip clip in clips)
            {
                if (clip.name.Contains("Idle"))
                {
                    idleDuration = clip.length;
                    break;
                }
            }
        }
    }
}
