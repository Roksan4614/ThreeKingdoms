using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

    public bool isDoing => m_element.pad.gameObject.activeSelf || m_isKeyboardMoving;

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
        m_element.btnCall.onClick.AddListener(() => OnButton_Call());

        Signal.instance.ConnectMainHero.connectLambda = new(this, _mainHero => m_mainHero = _mainHero);
        Signal.instance.StartStage.connectLambda = new(this, _ =>
        {
            TimerCallAsync().Forget();
            DashTimerStartAsync().Forget();
        });

        Signal.instance.ActiveHUD.connectLambda = new(this, _isActive =>
        {
            gameObject.SetActive(_isActive);
        });

        DashButtonInitalize();

        SlotUpdateTeamPosition();

        Signal.instance.UpdateTeamPosition.connect = SlotUpdateTeamPosition;

    }

    void SlotUpdateTeamPosition()
    {
        bool isActive = DataManager.userInfo.myHero.Count(x => x.isBatch == true) > 1;
        m_element.btnCall.gameObject.SetActive(isActive);
    }

    public bool IsControll(CharacterComponent _hero)
        => m_mainHero == _hero && isDoing;

    public void SetActive_Action(bool _isActive)
        => m_element.btnAttack.transform.parent.gameObject.SetActive(_isActive);

    private void Update()
    {
        if (isSwitch == false || m_mainHero?.isLive == false)
            return;

        OnUpdateMove();

        if (m_isKeyboardMode)
        {
            if (Input.anyKeyDown)
            {
                // 공격
                if (Input.GetKeyDown(KeyCode.X))
                    OnButton_Attack();
                // 대쉬
                else if (Input.GetKeyDown(KeyCode.C))
                    OnButton_Dash(false);
                // 콜
                else if (Input.GetKeyDown(KeyCode.V))
                    OnButton_Call();
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

    void OnButton_Call()
    {
        if (m_element.imgCallTimer.gameObject.activeSelf == false)
        {
            m_element.panelCall.localScale = Vector3.one;
            m_element.panelCall.DOPunchScale(Vector3.one * .1f, 0.1f);

            TimerCallAsync().Forget();
            TeamManager.instance.CallToMainHeroAsync().Forget();
        }
    }

    async UniTask TimerCallAsync()
    {
        if (m_mainHero.isLive == false)
            return;

        // TODO: 어딘가에서 값을 가져와야 하지 않을까?
        float durationCall = 5f;

        m_element.imgCallTimer.gameObject.SetActive(true);
        m_element.txtCallTimer.gameObject.SetActive(true);

        float startTime = Time.time, endTime = startTime + durationCall;

        int mspaceValue = (int)(m_element.txtCallTimer.fontSize * 0.5f);
        while (endTime > Time.time)
        {
            m_element.txtCallTimer.text = Utils.MSpace($"{endTime - Time.time:0.0}", mspaceValue);

            float progress = (Time.time - startTime) / (endTime - startTime);
            m_element.imgDashTimer.fillAmount = 1 - progress;

            await UniTask.Yield();
        }

        m_element.imgCallTimer.gameObject.SetActive(false);
        m_element.txtCallTimer.gameObject.SetActive(false);
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
    public static bool isClickDown => instance.isLeftClick_Down || instance.isRightClick_Down || instance.isTouch;

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

        if (isDoing == false)
            return;

        StopControll();
        m_element.pad.gameObject.SetActive(false);
        //m_element.btnDash.transform.parent.gameObject.SetActive(false);
    }

    public void OnDrag(PointerEventData _eventData)
    {
        if (isDoing == false)
            return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 targetPos);

        m_element.padBar.anchoredPosition = Vector2.ClampMagnitude(targetPos - m_element.pad.anchoredPosition, m_maxRadiusBar);
    }

    void StopControll()
    {
        if (m_mainHero?.isLive == true)
            m_mainHero.SetState(TeamManager.instance.teamState);
    }

    Tween m_tweenMoveAction;
    public void SetMoveActionArea(bool _isBottom, bool _isTween = true)
    {
        m_tweenMoveAction?.Kill();

        var target = m_element.rt.offsetMin;
        target.y = _isBottom ? 250 : 455;

        DOTween.To(() => target, _offsetMin => m_element.rt.offsetMin = _offsetMin, target, _isTween ? 0.2f : 0f);
        //m_tweenMoveAction = m_element.rtAction.DOAnchorPosY(_isBottom ? -50f : 130f, _isTween ? 0.1f : 0f);
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;
    [Serializable]
    public struct ElementData
    {
        public RectTransform pad;
        public RectTransform padBar;
        public RectTransform rt;

        public Button btnAttack;

        public Button btnCall;
        public Transform panelCall;
        public TextMeshProUGUI txtCallTimer;
        public Image imgCallTimer;

        public Button btnDash;
        public Transform panelDash;
        public TextMeshProUGUI txtDashTimer;
        public Image imgDashTimer;
        public List<GameObject> iconDashCount;

        public void Initialize(Transform _transform)
        {
            rt = (RectTransform)_transform;
            pad = (RectTransform)_transform.Find("Pad");
            padBar = (RectTransform)pad.Find("Bar");

            btnAttack = _transform.GetComponent<Button>("Action/btn_attack");

            btnCall = _transform.GetComponent<Button>("Action/btn_call");
            panelCall = btnCall.transform.Find("Panel");
            txtCallTimer = panelCall.GetComponent<TextMeshProUGUI>("txt_timer");
            imgCallTimer = panelCall.GetComponent<Image>("Timer");


            btnDash = _transform.GetComponent<Button>("Action/btn_dash");
            panelDash = btnDash.transform.Find("Panel");
            txtDashTimer = panelDash.GetComponent<TextMeshProUGUI>("txt_timer");
            imgDashTimer = panelDash.GetComponent<Image>("Timer");

            iconDashCount = new()
            {
                btnDash.transform.Find("Count_1/BG").gameObject,
                btnDash.transform.Find("Count_2/BG").gameObject
            };
        }
    }
    #endregion
}
