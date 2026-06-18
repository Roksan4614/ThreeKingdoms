using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityRandom = UnityEngine.Random;

public class LobbyScreen_Castle_NPCComponent : MonoBehaviour, IValidatable, IPointerDownHandler
{
    public enum NPCEmoticonType
    {
        NONE = -1,

        Exclaim,    //느낌표
        Question,   //물음표
        Think,      //생각
        Idea,       //아이디어
        Angry,      //화남
        Love,       //사랑
        Fluster,    //식은땀
        Slip        //졸림
    }

    float m_speed = 50;
    float m_posTop, m_posBottom;

    CancellationTokenSource m_cts;

    public bool isTestNPC;
    NPCEmoticonType m_emoticonType = NPCEmoticonType.NONE;

    private void OnDestroy()
        => ReleaseCTS();

    public void Initialize(float _posTop, float _posBottom)
    {
        m_posTop = _posTop;
        m_posBottom = _posBottom;
    }

    protected virtual void Update()
    {
        if (isTestNPC == true && m_element.panel.gameObject.activeSelf)
            OnUpdate_NPCMove();
    }

    public void Spawn(Vector3 _localPosition)
    {
        SetBodyAsync(UnityRandom.Range(1, 12)).Forget();
        transform.localPosition = _localPosition;
        OnUpdate_NPCMove();
    }

    int m_idxStreet, m_idxPos;
    public async UniTask StartAsync(int _idxStreet, int _idxPos)
    {
        m_idxStreet = _idxStreet;
        m_idxPos = _idxPos;

        transform.DOKill();
        ReleaseCTS();

        m_cts = new();
        var token = m_cts.Token;

        gameObject.SetActive(true);

        while (true)
        {
            await UniTask.WaitForSeconds(UnityRandom.Range(2f, 10f), cancellationToken: token);

            if (m_emoticonType > NPCEmoticonType.NONE)
            {
                m_element.emoticon.GetChild((int)m_emoticonType).gameObject.SetActive(false);
                m_emoticonType = NPCEmoticonType.NONE;
            }

            m_element.anim.Play("Castle_NPC_Walk");

            var targetPos = LobbyScreen_Castle_NPCManager.instance.GetTargetPosition(_idxStreet, m_idxPos, out m_idxPos);
            SetFlip(targetPos, true);

            float distance = Vector3.Distance(transform.localPosition, targetPos);

            // 2. 시간 = 거리 / 속도
            float duration = distance / (m_speed * UnityRandom.Range(.5f, 1f));

            await transform.DOLocalMove(targetPos, duration).
                OnUpdate(() => OnUpdate_NPCMove())
                .SetEase(Ease.Linear)
                .AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            m_element.anim.Play("Castle_NPC_Idle");

            // 성문뒤에 완전히 숨었으면 좀 바꿔주자
            if ((_idxStreet == 0 && m_idxPos == 16) ||
                (_idxStreet == 3 && m_idxPos == 0))
                SetBodyAsync(UnityRandom.Range(1, 12)).Forget();
        }
    }

    void SetFlip(Vector3 targetPos, bool _isLocal)
    {
        var rot = m_element.panel.rotation;

        var posX = _isLocal ? transform.localPosition.x : transform.position.x;

        if (targetPos.x < posX != (rot.eulerAngles.y == 0))
        {
            rot = Quaternion.Euler(0, rot.eulerAngles.y == 0 ? 180 : 0, 0);
            m_element.panel.rotation = rot;
        }
    }

    public async UniTask SetBodyAsync(int _idx)
    {
        m_element.panel.gameObject.SetActive(false);
        var sprite = await AddressableManager.instance.GetAtlasAsync_CastleNPC(_idx);

        for (int i = 0; i < sprite.Length; i++)
        {
            if (sprite[i] == null)
            {
                gameObject.SetActive(false);
                return;
            }
            m_element.imgBody[i].sprite = sprite[i];
        }

        transform.name = "NPC_" + _idx;
        m_element.panel.gameObject.SetActive(true);
    }

    void OnUpdate_NPCMove()
    {
        var t = Mathf.InverseLerp(m_posTop, m_posBottom, transform.localPosition.y);
        float scaleValue = Mathf.Lerp(0.8f, 1.2f, t);

        transform.localScale = new Vector3(scaleValue, scaleValue, 1);
        m_element.canvas.sortingOrder = 10000 - (int)(transform.localPosition.y * 10);
    }
    void ReleaseCTS()
        => m_cts = m_cts.Release();

    public virtual void OnPointerDown(PointerEventData _eventData)
    {
        transform.DOKill();
        ReleaseCTS();

        m_element.anim.Play("Castle_NPC_Hit");
        StartAsync(m_idxStreet, m_idxPos).Forget();

        List<NPCEmoticonType> touchEmoticon = new() { NPCEmoticonType.Question, NPCEmoticonType.Angry, NPCEmoticonType.Love, NPCEmoticonType.Fluster };

        if (m_emoticonType > NPCEmoticonType.NONE)
            m_element.emoticon.GetChild((int)m_emoticonType).gameObject.SetActive(false);

        m_emoticonType = touchEmoticon[UnityRandom.Range(0, touchEmoticon.Count)];
        m_element.emoticon.GetChild((int)m_emoticonType).gameObject.SetActive(true);

        var touchFingerId = Input.touchCount == 0 ? -1 : Input.GetTouch(Input.touchCount - 1).fingerId;
        SetFlip(CameraManager.GetPosPointer(touchFingerId), false);
    }

    #region VALIDATE
    public virtual void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public RectTransform panel;

        public Image[] imgBody;
        public Animator anim;

        public Canvas canvas;

        public Transform emoticon;

        public void Initialize(Transform _transform)
        {
            panel = (RectTransform)_transform.Find("Pivot");
            imgBody = new[] {
                _transform.GetComponent<Image>("Pivot/Body"),
                _transform.GetComponent<Image>("Pivot/Head")
            };

            anim = _transform.GetComponent<Animator>();
            canvas = _transform.GetComponent<Canvas>();

            emoticon = _transform.Find("Emoticons");
        }
    }
    #endregion VALIDATE

}
