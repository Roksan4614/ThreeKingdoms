using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Controller_Skill : Controller_Attack
{
    public bool isReady => m_elementSkill.imgTimer.gameObject.activeSelf == false;
    public bool isDrag = false;

    float m_magnitude;

    protected override void Start()
    {
        button = transform.GetComponent<Button>();
        button.onClick.AddListener(OnButton_Skill);

        SetInteractable(false);

        Signal.instance.ConnectMainHero.connectLambda = new(this, _hero =>
        {
            m_hero = _hero;
            m_pointer = m_hero.element.skillRange;
        });
    }

    public void OnButton_Skill()
    {
        if (ControllerManager.instance.isSwitch == true)
            TeamManager.instance.heroInfo.UseSkill(0);
    }

    public void SetInteractable(bool _isInteractable)
        => button.interactable = _isInteractable;

    public void Update()
    {
        if (m_pointer == null || isDrag == false)
            return;

        var mousePosition = CameraManager.instance.GetMousePosition();
        var dist = (m_element.startPosition.position - mousePosition);

        if (Mathf.Approximately(m_magnitude, dist.sqrMagnitude) == false)
        {
            m_magnitude = dist.sqrMagnitude;
            var targetPos = m_hero.transform.position +
                ((mousePosition - m_element.startPosition.position).normalized * dist.sqrMagnitude * m_power);

            targetPos.z = m_pointer.position.z;
            m_hero.attack.OnDrag_ControllSkill(targetPos);
        }
        // 드래그 중에 키보드 모드에서 오른쪽 클릭 하면 취소 누르자
        else if (ControllerManager.instance.isKeyboardMode == true &&
            ControllerManager.instance.isRightClick_Down)
        {
            m_pointer.gameObject.SetActive(false);
            OnPointerUp(null);
        }
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if (m_hero.isLive == false || m_pointer == null)
            return;

        var mousePosition = CameraManager.instance.GetMousePosition();
        var dist = (m_element.startPosition.position - mousePosition);

        if (dist.sqrMagnitude > 0.5f)
        {
            ControllerManager.instance.isSwitch = false;
            isDrag = true;
            m_element.startPosition.gameObject.SetActive(true);
            button.interactable = false;
            m_pointer.gameObject.SetActive(true);
        }
        else if (m_pointer.gameObject.activeSelf == true)
        {
            m_pointer.gameObject.SetActive(false);
            isDrag = false;
        }
    }
    public override void OnPointerUp(PointerEventData eventData)
    {
        if (button.interactable == false)
        {
            Utils.AfterSecond(() => button.interactable = true);

            ControllerManager.instance.isSwitch = true;
            isDrag = false;
            m_element.startPosition.gameObject.SetActive(false);
            m_hero.attack.OnUp_ControllSkill();
        }
    }

    public void UpdateColltime(float _duration, float _progress)
    {
        var imgTimer = m_elementSkill.imgTimer;
        button.interactable = _progress >= 1f;
        imgTimer.fillAmount = 1 - _progress;

        if (button.interactable == false)
        {
            m_elementSkill.txtTimer.text = Utils.MSpace($"{_duration * imgTimer.fillAmount:0.0}", 45);
            if (imgTimer.gameObject.activeSelf == false)
            {
                imgTimer.gameObject.SetActive(true);
                m_elementSkill.txtTimer.gameObject.SetActive(true);
            }
        }
        else if (imgTimer.gameObject.gameObject.activeSelf == true)
        {
            button.transform.DOPunchScale(Vector3.one * .05f, 0.1f);

            imgTimer.gameObject.gameObject.SetActive(false);
            m_elementSkill.txtTimer.gameObject.SetActive(false);
        }
    }

    public override void OnManualValidate()
    {
        m_elementSkill.Initialize(transform);
        base.OnManualValidate();
    }

    [SerializeField, HideInInspector]
    ElementDataSkill m_elementSkill;

    [Serializable]
    struct ElementDataSkill
    {
        public Image imgTimer;
        public TextMeshProUGUI txtTimer;

        //public List<Image> icons;
        //public Color colorIconTimer;

        public void Initialize(Transform _transform)
        {
            imgTimer = _transform.GetComponent<Image>("Timer");
            txtTimer = _transform.GetComponent<TextMeshProUGUI>("txt_timer");

            //icons = new(){
            //    _transform.GetComponent<Image>("Icon"),
            //    _transform.GetComponent<Image>("Icon/Image"),
            //};

            //ColorUtility.TryParseHtmlString("#B1B1B1", out colorIconTimer);
        }
    }

}
