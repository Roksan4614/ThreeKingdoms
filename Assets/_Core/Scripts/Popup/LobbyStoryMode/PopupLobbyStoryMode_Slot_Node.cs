using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PopupLobbyStoryMode_Slot_Node : MonoBehaviour, IValidatable
{
    int m_curIdx;
    List<Table_StoryMode_Node.TableStoryModeNodeData> m_data;

    private void Start()
    {
        m_element.button.onClick.AddListener(() => DataManager.storyMode.EnterAsync("node_1").Forget());
        m_element.btnChange.onClick.AddListener(OnButton_Change);
    }

    public void SetStoryNode(List<Table_StoryMode_Node.TableStoryModeNodeData> _data)
    {
        //다음에 오픈할 차례라면
        {
            var nextOpenOrderNumber = DataManager.storyMode.nextOpenOrderNumber;
            bool isNextOpen = _data[0].order_num == nextOpenOrderNumber;

            if (isNextOpen == true)
            {
                m_element.txtChoice.gameObject.SetActive(false);
                m_element.btnChange.gameObject.SetActive(false);
                m_element.button.text = "";

                m_element.objLock.SetActive(true);
                ((RectTransform)m_element.objLock.transform.GetChild(0)).SetAnchoredPositionY(30);
                m_element.objLock.transform.GetChild(0).gameObject.SetActive(true);

                var nodeData = TableManager.storyNode.GetNode_OrderNum(nextOpenOrderNumber)[0];
                m_element.txtCharacter.text = $"{nodeData.chapter_key}-{nodeData.stage_key} 클리어시 해제";

                m_element.button.interactable = false;
                return;
            }
        }

        //마지막 플레이한 것보다 나중이라면
        {
            bool overLastPlay = _data[0].order_num > DataManager.storyMode.nextPlayOrderNumber;
            if (overLastPlay)
            {
                ((RectTransform)m_element.objLock.transform).anchoredPosition = Vector2.zero;
                m_element.objLock.SetActive(true);
                m_element.objLock.transform.GetChild(0).gameObject.SetActive(false);
                m_element.button.interactable = false;
            }
            else
                m_element.objLock.SetActive(false);
        }


        m_data = _data;
        m_element.txtChoice.gameObject.SetActive(false);
        m_element.btnChange.gameObject.SetActive(_data.Count > 1);
        SetNodeData();
    }

    void SetNodeData()
    {
        var data = m_data[m_curIdx];
        m_element.button.text = TableManager.storyString.GetString($"{data.node_key.ToUpper()}_TITLE");
        m_element.txtCharacter.text =
            data.character_key.IsActive() ?
            $"-{data.character_key}-" : "";
    }

    void OnButton_Change()
    {
        m_curIdx = (m_curIdx + 1) % m_data.Count;
        SetNodeData();
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public ButtonHelper button;
        public Button btnChange;

        public TextMeshProUGUI txtCharacter;
        public TextMeshProUGUI txtChoice;

        public GameObject objLock;

        public void Initialize(Transform _transform)
        {
            button = _transform.GetComponent<ButtonHelper>("Panel");
            btnChange = _transform.GetComponent<Button>("Panel/btn_change");

            txtCharacter = _transform.GetComponent<TextMeshProUGUI>("Panel/txt_character_name");
            txtChoice = _transform.GetComponent<TextMeshProUGUI>("txt_choice");

            objLock = _transform.Find("Panel/Lock").gameObject;
        }
    }
    #endregion VALIDATE

}
