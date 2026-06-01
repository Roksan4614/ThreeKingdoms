using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;

public class LobbyScreen_Castle_NPC_Wally : MonoBehaviour, IValidatable, IPointerDownHandler
{
    LobbyScreen_Castle m_castle;
    CancellationTokenSource m_cts;

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
        if (UnityEngine.Random.value < .1f)
        {
            DataManager.castle.HitWally();
            m_element.anim.Play("Castle_Wally_Hit");
        }
        else
        {
            // µµ¸Á
            m_element.anim.Play("Castle_Wally_End");
            Utils.AfterSecond(() =>
            {
                if (gameObject.activeInHierarchy == false)
                    return;
                SpawnStartAsync().Forget();
            }, UnityEngine.Random.Range(1f, 1.5f));
        }
    }

    public async UniTask SpawnStartAsync()
    {
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
            await UniTask.WaitForSeconds(1.5f * UnityEngine.Random.Range(1, 3), cancellationToken: token);
            DataManager.castle.SetWallyUISpawn(false);

            m_isShow = false;
            m_element.anim.Play("Castle_Wally_Off");
            await UniTask.WaitForSeconds(UnityEngine.Random.Range(0.5f, 2f), cancellationToken: token);
        }
    }

    void Release_CTS()
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
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

        public void Initialize(Transform _transform)
        {
            anim = _transform.GetComponent<Animator>();
        }
    }
}
