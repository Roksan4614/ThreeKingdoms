using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TooltipWorker : MonoBehaviour, IValidatable
{
    [SerializeField] string m_key;

    CancellationTokenSource m_cts;

    string text
    {
        get => m_element.txtTooltip.text;
        set => m_element.txtTooltip.text = value;
    }

    private void Start()
    {
        if(ControllerManager.instance.isKeyboardMode == false)
        {
            Destroy(gameObject);
            return;
        }

        if (m_key.IsActive() == true)
            text = TableManager.stringTable.GetString(m_key);
        else
            gameObject.SetActive(false);

        m_element.rtBox.gameObject.SetActive(false);

        Destroy(m_element.image);
        m_element.image = null;
    }

    public void Initialize(string _key)
    {
        m_key = _key;
        text = TableManager.stringTable.GetString(m_key);
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

    async UniTask ShowAsync()
    {
        if (text.IsActive() == false)
            return;

        ReleaseCTS();

        m_cts = new();
        var token = m_cts.Token;

        var stayTimer = .5f;
        var endTime = Time.time + stayTimer;

        var rtBox = m_element.rtBox;
        var pointer = CameraManager.pointer;
        var prevPos = pointer.position;

        rtBox.gameObject.SetActive(false);

        while (true)
        {
            await UniTask.WaitForEndOfFrame(cancellationToken: token);

            // 마우스 움직였는지 여부 확인
            var distance = (prevPos - CameraManager.posPointer).sqrMagnitude;
            if (distance > .1f)
            {
                if (rtBox.gameObject.activeSelf == true)
                    rtBox.gameObject.SetActive(false);

                prevPos = pointer.position;
                endTime = Time.time + stayTimer;
                continue;
            }

            if (endTime > Time.time)
                continue;

            if (rtBox.gameObject.activeSelf == false)
            {
                rtBox.gameObject.SetActive(true);
                SetReposition();
            }
        }
    }

    bool isReposition = false;
    void SetReposition()
    {
        if (isReposition == true)
            return;

        isReposition = true;
        var posBox = m_element.rtBox.position;
        m_element.rtBox.ForceRebuildLayout();

        if (m_element.lt.position.x < PopupManager.instance.lt.x)
            posBox.x += PopupManager.instance.lt.x - m_element.lt.position.x;
        else if (m_element.rb.position.x > PopupManager.instance.rb.x)
            posBox.x -= m_element.rb.position.x - PopupManager.instance.rb.x;

        if (m_element.lt.position.y > PopupManager.instance.lt.y)
            posBox.y -= m_element.lt.position.y - PopupManager.instance.lt.y;
        else if (m_element.rb.position.y < PopupManager.instance.rb.y)
            posBox.y += PopupManager.instance.rb.y - m_element.rb.position.y;

        m_element.rtBox.position = posBox;
    }

    private void OnTriggerEnter2D(Collider2D _collision)
    {
        if (_collision.tag == "Pointer")
            ShowAsync().Forget();
    }

    private void OnTriggerExit2D(Collider2D _collision)
    {
        if (_collision.tag == "Pointer" && m_cts != null)
        {
            m_element.rtBox.gameObject.SetActive(false);
            ReleaseCTS();
        }
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTooltip;

        public Transform lt;
        public Transform rb;

        public RectTransform rtBox;
        public Image image;

        public void Initialize(Transform _transform)
        {
            image = _transform.GetComponent<Image>();
            rtBox = _transform.GetComponent<RectTransform>("Box");

            txtTooltip = rtBox.GetComponent<TextMeshProUGUI>("Panel/Text");
            lt = rtBox.Find("Panel/leftTop");
            rb = rtBox.Find("Panel/rightBottom");
        }
    }
    #endregion VALIDATA
}
