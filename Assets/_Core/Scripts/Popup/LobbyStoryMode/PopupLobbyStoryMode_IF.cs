using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyStoryMode_IF : MonoBehaviour, IValidatable
{
    public bool Open(Table_StoryMode_Node.TableStoryModeNodeData _nodeData)
    {
        gameObject.SetActive(_nodeData?.isActive ?? false);

        if (gameObject.activeSelf == false)
            return false;

        if (DataManager.storyMode.lastHistory.key == _nodeData.node_key)
        {
            DataManager.storyMode.lastHistory = default;
            PopupManager.instance.AlertShow("시간이_어긋나_버렸습니다");
            Utils.SetActivePunch(m_element.panel, true);
        }

        m_element.txtTitle.text = _nodeData.name;

        var slot = m_element.scroll.content.GetChild(0);
        var idx = slot.Find("Node").GetSiblingIndex();

        //첫번째꺼 넣어주자
        var node = slot.GetChild(idx++).GetComponent<PopupLobbyStoryMode_Slot_Node>();
        node.SetStoryNode(new() { _nodeData });
        //node.SetInteractable(false);
        node.SetActive_Choice(false);

        var nextData = TableManager.storyNode.GetNode(_nodeData.next_node_key);
        while (nextData.isActive == true)
        {
            node = slot.childCount == idx ? Instantiate(node, slot) : slot.GetChild(idx).GetComponent<PopupLobbyStoryMode_Slot_Node>();
            node.SetStoryNode(new() { nextData });
            idx++;

            if (DataManager.storyMode.IsComplete(nextData.node_key) == false || nextData.next_node_key.IsActive() == false)
                break;

            //node.SetInteractable(false);
            nextData = TableManager.storyNode.GetNode(nextData.next_node_key);
        }

        for (; idx < slot.childCount; idx++)
            slot.GetChild(idx).gameObject.SetActive(false);

        slot.ForceRebuildLayout();

        if (nextData.next_node_key.IsActive() == false && nextData.node_key == DataManager.storyMode.lastHistory.key)
        {
            PopupManager.instance.AlertShow("어긋난_시간선의_끝에_도달했습니다.");
            DataManager.storyMode.lastHistory = default;
        }

        bool isCompleteLastNode = nextData.next_node_key.IsActive() == false && DataManager.storyMode.IsComplete(nextData.node_key);

        m_element.txtDesc.text = isCompleteLastNode ?
             "시간을_돌려_되돌아갑니다." : "시간이_어긋나_있습니다.";

        m_element.btnConfirm.text = isCompleteLastNode ? "돌아가기_" : "포기하기_";
        m_element.btnConfirm.onClick.RemoveAllListeners();
        m_element.btnConfirm.onClick.AddListener(() =>
        {
            if (DataManager.storyMode.isLockUI == true)
                return;

            m_element.btnConfirm.interactable = false;
            if (nextData.next_node_key.IsActive())
            {
                PopupManager.instance.OpenModalAsync("포기하시겠습니까??_", _callback: _result =>
                {
                    if (_result == StatusType.Success)
                    {
                        PopupManager.instance.AlertShow("시간을_돌려_되돌아갑니다.");

                        Utils.SetActivePunch(m_element.panel, false);
                        Utils.SetActivePunch(transform.parent, true);

                        DataManager.storyMode.ResetIFMode(_nodeData.node_key);
                    }
                    else
                        m_element.btnConfirm.interactable = true;
                }).Forget();
            }
            else
            {
                PopupManager.instance.AlertShow("시간을_돌려_되돌아갑니다.");

                Utils.SetActivePunch(m_element.panel, false);
                Utils.SetActivePunch(transform.parent, true);

                DataManager.storyMode.ResetIFMode(_nodeData.node_key);
            }
        });

        return true;
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public TextMeshProUGUI txtTitle;
        public ScrollRect scroll;
        public TextMeshProUGUI txtDesc;

        public ButtonHelper btnConfirm;

        public Transform parentPanel;

        public void Initialize(Transform _transform)
        {
            txtTitle = _transform.GetComponent<TextMeshProUGUI>("Panel/Scroll/txt_title");
            scroll = _transform.GetComponent<ScrollRect>("Panel/Scroll");
            txtDesc = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_desc");

            btnConfirm = _transform.GetComponent<ButtonHelper>("Panel/btn_confirm");
        }

        public Transform panel => scroll.transform.parent;
    }
    #endregion VALIDATE

}
