using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Character_Worker_Talkbox : Character_Worker
{
    public Character_Worker_Talkbox(CharacterComponent _owner) : base(_owner)
    {
        m_txtTalk = m_owner.element.txtTalk;
        m_rtTalkbox = (RectTransform)m_txtTalk.transform.parent;
        m_layout = m_rtTalkbox.GetComponent<HorizontalLayoutGroup>();
        m_fitter = m_rtTalkbox.GetComponent<ContentSizeFitter>();
        SetActive(false);
    }

    RectTransform m_rtTalkbox;
    TextMeshProUGUI m_txtTalk;
    HorizontalLayoutGroup m_layout;
    ContentSizeFitter m_fitter;

    public string text => m_txtTalk.text;

    public bool isTyping { get; private set; } = false;
    //public async UniTask WaitTyping() => await UniTask.WaitUntil(() => isTyping == false, cancellationToken:m_cts.Token);

    void Init(params string[] _talks)
    {
        SetActive(true);
        SetFlip(m_owner.move.isFlip);

        m_txtTalk.text = string.Join("", _talks);

        m_layout.enabled = m_fitter.enabled = true;
        m_fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        m_rtTalkbox.ForceRebuildLayout();

        if (m_rtTalkbox.rect.width > 1300)
        {
            m_fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var size = m_rtTalkbox.sizeDelta;
            size.x = 1300;
            m_rtTalkbox.sizeDelta = size;

            m_rtTalkbox.ForceRebuildLayout();
        }

        m_layout.enabled = m_fitter.enabled = false;
    }

    public void SetFlip(bool _isFlip)
    {
        if (m_rtTalkbox.gameObject.activeSelf == false)
            return;

        if (_isFlip == m_rtTalkbox.pivot.x > 0.5f)
        {
            var pivot = m_rtTalkbox.pivot;
            pivot.x = _isFlip ? .4f : .6f;
            m_rtTalkbox.pivot = pivot;

            var anchPos = m_rtTalkbox.anchoredPosition;
            anchPos.x = 0;
            m_rtTalkbox.anchoredPosition = anchPos;
        }
    }

    public async UniTask StartAsyncClickDisable(CancellationToken _token, params string[] _talks)
    {
        await StartAsync(_token, _talks);
        await UniTask.WaitUntil(() => ControllerManager.isClickDown
        || Input.GetKeyDown(KeyCode.Return)
        || Input.GetKeyDown(KeyCode.Space), cancellationToken: _token);
        SetActive(false);
    }

    //public async UniTask StartAsyncAutoDisable(float _duration, CancellationToken _token, params string[] _talks)
    //{
    //    await StartAsync(_talks);
    //    await UniTask.WaitForSeconds(_duration, cancellationToken: _token);
    //    SetActive(false);
    //}

    public void Start(CancellationToken _token, params string[] _talks)
        => StartAsync(_token, _talks).Forget();

    bool m_isCancel;
    public async UniTask StartAsync(CancellationToken _token, params string[] _talks)
    {
        await UniTask.WaitUntil(() => ControllerManager.isClick == false, cancellationToken: _token);

        if (isTyping == true)
        {
            m_isCancel = true;
            await UniTask.WaitUntil(() => isTyping == false);
            m_isCancel = false;
        }
        isTyping = true;

        Init(_talks);

        var totalMsg = m_txtTalk.text;
        m_txtTalk.text = "";

        for (int i = 0; i < _talks.Length; i++)
        {
            int idx = 0;
            var msg = _talks[i];
            while (idx < msg.Length && m_isCancel == false)
            {
                var m = msg[idx++];
                m_txtTalk.text += m;

                if (m == '<')
                {
                    while (true)
                    {
                        var fm = msg[idx++];
                        m_txtTalk.text += fm;

                        if (fm == '>')
                            break;
                    }
                    continue;
                }

                await UniTask.WaitForSeconds(0.03f, cancellationToken: _token);

                if (Input.GetKey(KeyCode.Return) || ControllerManager.isClick)
                {
                    m_txtTalk.text = totalMsg;

                    await UniTask.WaitForEndOfFrame(cancellationToken: _token);
                    isTyping = false;
                    return;
                }
            }

            await UniTask.WaitForSeconds(0.2f, cancellationToken: _token);
        }

        await UniTask.WaitForEndOfFrame(cancellationToken: _token);
        isTyping = false;
    }

    public void SetActive(bool _isActive)
        => m_rtTalkbox.gameObject.SetActive(_isActive);
}
