using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupRewardComponent : BasePopupComponent
{
	PopupRewardComponent() : base(PopupType.Reward) { }

	List<ItemData> m_rewards;
	bool m_isReadyClose = false;
	bool m_isClose = false;

	public override void OpenPopup(params object[] _args)
	{
		m_rewards = (List<ItemData>)_args[0];
	}

	private void Start()
		=> StartAsync().Forget();

	bool m_isSkip;
	async UniTask StartAsync()
	{
		m_isSkip = false;
		m_isClose = m_isReadyClose = false;
		var pReward = transform.Find("Panel/Reward");
		var panel = pReward.parent;

		for (int i = 0; i < m_rewards.Count; i++)
		{
			var slot = i < pReward.childCount ? pReward.GetChild(i) : Instantiate(pReward.GetChild(0), pReward);
		}

		pReward.ForceRebuildLayout(1);

		//panel.GetComponent<ContentSizeFitter>().enabled = false;
		//panel.GetComponent<VerticalLayoutGroup>().enabled = false;

		pReward.GetComponent<ContentSizeFitter>().enabled = false;
		pReward.GetComponent<GridLayoutGroup>().enabled = false;

		for (int i = 0; i < m_rewards.Count; i++)
			pReward.GetChild(i).gameObject.SetActive(false);

		var txtDesc = transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");
		txtDesc.text = "";

		await UniTask.WaitForSeconds(.1f);

		//await UniTask.NextFrame();

		var title = transform.Find("Panel/Title");
		await Utils.SetActivePunchAsync(title, true);
		await UniTask.WaitForSeconds(.5f);

		for (int i = 0; i < m_rewards.Count; i++)
		{
			var slot = pReward.GetChild(i).GetComponent<ItemComponent>();
			slot.SetItemData(m_rewards[i]);
			await Utils.SetActivePunchAsync(slot.transform, true);
			if (m_isSkip == false)
			{
				await UniTask.WaitForSeconds(.5f);
			}
		}

		await UniTask.WaitForSeconds(.5f);
		txtDesc.text = "ºó_°÷À»_´­·¯_´Ý±â";

		m_isReadyClose = true;
		await UniTask.WaitUntil(() => m_isClose == true);

		Utils.SetActivePunch(txtDesc.transform.parent, false, _callback: base.Close);

		List<UniTask> tasks = new();
		for (int i = 0; i < m_rewards.Count; i++)
			tasks.Add(RewardWorker.instance.RunAsync(pReward.GetChild(i).position, _itemData: m_rewards[i]));

		await UniTask.WhenAll(tasks.ToArray());
	}

	private void Update()
	{
		if (m_isSkip == false && ControllerManager.isClick)
			m_isSkip = true;
	}

	public override void Close()
	{
		if (m_isReadyClose == false)
			return;

		m_isClose = true;
	}

	#region VALIDATE
	public override void OnManualValidate() => m_element.Initialize(transform);

	//[SerializeField, HideInInspector]
	[SerializeField]
	ElementData m_element;

	[System.Serializable]
	struct ElementData
	{
		public void Initialize(Transform _transform)
		{
		}
	}
	#endregion VALIDATE

}
