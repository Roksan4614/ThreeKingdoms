using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityRandom = UnityEngine.Random;

public class LobbyScreen_Castle_NPCComponent : MonoBehaviour, IValidatable
{
    float m_speed = 50;
    float m_posTop, m_posBottom;

    CancellationTokenSource m_cts;

    private void OnDisable()
        => ReleaseCTS();

    public void Initialize(float _posTop, float _posBottom)
    {
        m_posTop = _posTop;
        m_posBottom = _posBottom;
    }

    public void Spawn(Vector3 _localPosition)
    {
        SetBodyAsync(UnityRandom.Range(1, 12)).Forget();
        transform.localPosition = _localPosition;
        OnUpdate_NPCMove();
    }
    public async UniTask StartAsync(int _idxStreet, int _idxPos)
    {
        ReleaseCTS();
        m_cts = new();
        var token = m_cts.Token;

        gameObject.SetActive(true);

        var rt = (RectTransform)transform;

        while (true)
        {
            await UniTask.WaitForSeconds(UnityRandom.Range(2f, 10f), cancellationToken: token);

            m_element.anim.Play("Castle_NPC_Walk");

            var targetPos = LobbyScreen_Castle_NPCManager.instance.GetTargetPosition(_idxStreet, _idxPos, out _idxPos);

            var rot = rt.rotation;
            if (targetPos.x < transform.localPosition.x != (rot.eulerAngles.y == 0))
            {
                rot = Quaternion.Euler(0, rot.eulerAngles.y == 0 ? 180 : 0, 0);
                rt.rotation = rot;
            }

            float distance = Vector3.Distance(transform.localPosition, targetPos);

            // 2. 시간 = 거리 / 속도
            float duration = distance / (m_speed * UnityRandom.Range(.5f, 1f));

            await transform.DOLocalMove(targetPos, duration).
                OnUpdate(() => OnUpdate_NPCMove())
                .SetEase(Ease.Linear)
                .AsyncWaitForCompletion().AsUniTask().AttachExternalCancellation(token);

            m_element.anim.Play("Castle_NPC_Idle");
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
    {
        if (m_cts != null)
        {
            m_cts.Cancel();
            m_cts.Dispose();
            m_cts = null;
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Transform panel;

        public Image[] imgBody;
        public Animator anim;

        public Canvas canvas;

        public void Initialize(Transform _transform)
        {
            panel = _transform.Find("Pivot");
            imgBody = new[] {
                _transform.GetComponent<Image>("Pivot/Body"),
                _transform.GetComponent<Image>("Pivot/Head")
            };

            anim = _transform.GetComponent<Animator>();
            canvas = _transform.GetComponent<Canvas>();
        }
    }
    #endregion VALIDATE

}
