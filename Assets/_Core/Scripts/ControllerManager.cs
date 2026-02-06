using System.Collections;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

public class ControllerManager : Singleton<ControllerManager>, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
	RectTransform m_pad;
	RectTransform m_padBar;

	[SerializeField] float m_maxRadiusBar = 150;

	[SerializeField]
	CharacterComponent m_character;
	[SerializeField]
	float m_speed = 10;

	public bool isActive => m_pad.gameObject.activeSelf;

	private void Start()
	{
		m_pad = (RectTransform)transform.Find("Pad");
		m_pad.gameObject.SetActive(false);

		m_padBar = (RectTransform)m_pad.Find("Bar");
	}

	private void Update()
	{
		if (isActive == true && m_character.isLive == true)
			m_character.OnConrollerMove(m_padBar.position - m_pad.position);
	}

	public void OnPointerDown(PointerEventData _eventData)
	{
		if (TeamManager.instance.mainHero.isLive == false)
			return;

		RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 startPos);

		m_pad.anchoredPosition = startPos;
		m_pad.gameObject.SetActive(true);
		m_padBar.localPosition = Vector3.zero;

		OnDrag(_eventData);
	}

	public void OnPointerUp(PointerEventData _eventData)
	{
		if (isActive == false)
			return;

		if (m_character.isLive)
			m_character.SetState(TeamManager.instance.teamState);

		m_pad.gameObject.SetActive(false);
	}

	public void OnDrag(PointerEventData _eventData)
	{
		if (isActive == false)
			return;

		RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, _eventData.position, _eventData.pressEventCamera, out Vector2 targetPos);

		m_padBar.anchoredPosition = Vector2.ClampMagnitude(targetPos - m_pad.anchoredPosition, m_maxRadiusBar);
	}
}
