using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class ControllerManager : Singleton<ControllerManager>, IPointerDownHandler, IPointerUpHandler, IDragHandler, IValidatable
{
    [SerializeField] bool m_isKeyboardMode = true;
    [SerializeField] float m_maxRadiusBar = 150;
    [SerializeField] float m_startRotZ = 20;

    CharacterComponent m_mainHero;

    bool m_isKeyboardMoving = false;

    bool m_isDashClick = false;
    bool m_isPush = false;

    public bool isSwitch { get; set; } = false;

    public bool isActive => m_element.pad.gameObject.activeSelf || m_isKeyboardMoving;

    private void Start()
    {
        m_element.pad.gameObject.SetActive(false);

        if (m_isKeyboardMode)
        {
            var dash = m_element.btnDash.transform;

            //dash.parent.gameObject.SetActive(false);
            //dash.SetParent(transform);
            //dash.position = m_element.posDash.position;
        }

        m_element.btnDash.onClick.AddListener(() => OnButton_Dash(false));
        m_element.btnAttack.onClick.AddListener(() => OnButton_Attack());

        Signal.instance.ConnectMainHero.connectLambda = new(this, _mainHero => m_mainHero = _mainHero);
        Signal.instance.StartStage.connectLambda = new(this, _ => DashTimerStartAsync().Forget());

        DashButtonInitalize();
    }

    private void Update()
    {
        if (isSwitch == false || m_mainHero?.isLive == false)
            return;

        OnUpdateMove();

        if (m_isKeyboardMode)
        {
            if (Input.anyKeyDown)
            {
                if (Input.GetKeyDown(KeyCode.P))
                    m_mainHero.talkbox.StartTalkAsync("안녕하세요. 저는 <color=#0000ff>임희동</color>입니다.", "삼국지 킹즈에 오신 걸 환영합니다.").Forget();

                // 공격
                if (Input.GetKeyDown(KeyCode.X))
                    OnButton_Attack();
                // 대쉬
                else if (Input.GetKeyDown(KeyCode.C))
                    OnButton_Dash(false);
                // 스킬
                else
                {
                    for (var i = KeyCode.Alpha1; i < KeyCode.Alpha4; i++)
                    {
                        if (Input.GetKeyDown(i))
                            TeamManager.instance.heroInfo.UseSkill(i - KeyCode.Alpha1);
                    }
                }
            }
            else if (m_isPush)
            {
                if (isRightClick && m_isDashClick == false)
                {
                    OnButton_Dash(true);
                    m_isDashClick = true;
                }
            }
        }
    }

    void OnUpdateMove()
    {
        var lookAt = Vector2.zero;
        if (m_element.pad.gameObject.activeSelf == true)
            lookAt = m_element.padBar.position - m_element.pad.position;
        else if (Input.anyKey)
        {
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
                lookAt.x = -1;
            else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
                lookAt.x = 1;

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                lookAt.y = 1;
            else if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
                lookAt.y = -1;
        }

        if (lookAt != Vector2.zero)
        {
            m_mainHero.OnConrollerMove(lookAt);
            m_isKeyboardMoving = true;
        }
        else if (m_isKeyboardMoving == true)
        {
            m_isKeyboardMoving = false;
            StopControll();
        }
    }

    void OnButton_Attack()
    {
        if (m_mainHero.isLive == false)
            return;

        m_element.btnAttack.transform.localScale = Vector3.one;
        m_element.btnAttack.transform.DOPunchScale(Vector3.one * .05f, 0.1f);

        m_mainHero.attack.ControlAttack();
    }

    public bool isLeftClick_Down => Input.GetMouseButtonDown(0);
    public bool isRightClick_Down => Input.GetMouseButtonDown(1);
    public bool isLeftClick => Input.GetMouseButton(0);
    public bool isRightClick => Input.GetMouseButton(1);
    public bool isTouch => Input.touchCount > 0;
    public static bool isClick => instance.isLeftClick || instance.isRightClick || instance.isTouch;

    public void OnPointerDown(PointerEventData _eventData)
    {
        m_isPush = true;

        if (isLeftClick == false || isSwitch == false)
            return;
        //if (m_isKeyboardMode)
        //    return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 startPos);

        m_element.pad.anchoredPosition = startPos;
        m_element.pad.gameObject.SetActive(true);

        m_element.pad.rotation = Quaternion.Euler(0, 0, m_startRotZ);
        m_element.pad.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutBack);

        m_element.padBar.localPosition = Vector3.zero;

        //m_element.btnDash.transform.parent.gameObject.SetActive(true);

        OnDrag(_eventData);
    }

    public void OnPointerUp(PointerEventData _eventData)
    {
        m_isDashClick = m_isPush = false;

        if (isActive == false)
            return;

        StopControll();
        m_element.pad.gameObject.SetActive(false);
        //m_element.btnDash.transform.parent.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (isActive == false)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 targetPos);

        m_element.padBar.anchoredPosition = Vector2.ClampMagnitude(targetPos - m_element.pad.anchoredPosition, m_maxRadiusBar);
    }

    void StopControll()
    {
        if (m_mainHero?.isLive == true)
            m_mainHero.SetState(TeamManager.instance.teamState);
    }

    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public RectTransform pad;
        public RectTransform padBar;

        public Button btnAttack;

        public Button btnDash;
        public Transform panelDash;
        public TextMeshProUGUI txtDashTimer;
        public Image imgDashTimer;
        public Transform posDash;
        public List<GameObject> iconDashCount;

        public void Initialize(Transform _transform)
        {
            pad = (RectTransform)_transform.Find("Pad");
            padBar = (RectTransform)pad.Find("Bar");

            btnAttack = _transform.GetComponent<Button>("Action/btn_attack");

            btnDash = _transform.GetComponent<Button>("Action/btn_dash");
            panelDash = btnDash.transform.Find("Panel");
            txtDashTimer = panelDash.GetComponent<TextMeshProUGUI>("txt_timer");
            imgDashTimer = panelDash.GetComponent<Image>("Timer");
            posDash = _transform.Find("posDash");

            iconDashCount = new()
            {
                btnDash.transform.Find("Count_1/BG").gameObject,
                btnDash.transform.Find("Count_2/BG").gameObject
            };
        }
    }
}
