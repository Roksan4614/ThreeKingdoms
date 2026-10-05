using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Rev9.Edit.CustomCharacter
{
    public class Scene_HeroCustom : MonoBehaviour
    {
        public class PartsData
        {
            public ButtonHelper button;
            public int index;
        }

        [SerializeField] AnimatorController m_baseAnimController;

        TMP_InputField m_infFildName;
        GameObject m_objBaseCharacter;
        GameObject m_objCustomCharacter;
        Scene_HeroCustom_PopupDecal m_popupDecal;
        Scene_HeroCustom_PopupLoad m_popupLoad;

        EditCustomPartsType m_curPartsType = EditCustomPartsType.NONE;
        string m_curHeadParts;
        int m_prevPartsIndex;

        List<PartsData> m_dbHeadParts = new();
        Dictionary<EditCustomPartsType, PartsData> m_dbParts = new();

        Transform trnsCharacterParts => m_objCustomCharacter.transform.Find("Character/Panel/Parts");
        Transform trnsCharacterHeadParts => trnsCharacterParts.Find("Head").GetChild(m_dbParts[EditCustomPartsType.Head].index);
        Transform trnsCharacterHeadDecal => trnsCharacterHeadParts.Find("Decal");

        private void Awake()
        {
            m_infFildName = transform.GetComponent<TMP_InputField>("Canvas/Panel/inf_filename");
            m_popupDecal = transform.GetComponent<Scene_HeroCustom_PopupDecal>("Canvas/Popup/Decal");
            m_popupDecal.gameObject.SetActive(false);
            m_popupLoad = transform.GetComponent<Scene_HeroCustom_PopupLoad>("Canvas/Popup/Load");
            m_popupLoad.gameObject.SetActive(false);
            m_popupLoad.actionLoad = OnButton_ResetCharacter;

            m_objBaseCharacter = transform.Find("Character/BASE").gameObject;
            m_objBaseCharacter.SetActive(false);

            var parts = m_objBaseCharacter.transform.Find("Character/Panel/Parts");
            m_popupDecal.Initialize(parts);

            // 파츠 가져오기
            var customParts = transform.Find("Canvas/Panel/Custom_Parts");
            for (var i = EditCustomPartsType.NONE + 1; i < EditCustomPartsType.MAX; i++)
            {
                var tab = i;
                var idx = (int)i;
                var btn = (idx == customParts.childCount ? Instantiate(customParts.GetChild(0), customParts) : customParts.GetChild(idx))
                    .GetComponent<ButtonHelper>();
                btn.name = i.ToString();
                btn.onClick.AddListener(() =>
                {
                    m_curPartsType = tab;
                    m_curHeadParts = "";
                    OnButton_Parts();
                    OnButton_HeadParts();
                });

                m_dbParts.Add(i, new() { button = btn });
            }

            // 머리 파츠 가져오자
            {
                var customHeadParts = transform.Find("Canvas/Panel/Custom_HeadParts");
                var head = parts.Find("Head").GetChild(0);
                var idx = 0;
                for (int i = 0; i < head.childCount; i++)
                {
                    var partsName = head.GetChild(i).name;
                    if (partsName == "Sprite_face" ||
                         partsName == "Decal")
                        continue;

                    var btn = (idx == customHeadParts.childCount ? Instantiate(customHeadParts.GetChild(0), customHeadParts) : customHeadParts.GetChild(idx))
                    .GetComponent<ButtonHelper>();
                    btn.name = partsName;
                    btn.onClick.AddListener(() =>
                    {
                        m_curPartsType = EditCustomPartsType.NONE;
                        m_curHeadParts = partsName;
                        OnButton_Parts();
                        OnButton_HeadParts();
                    });

                    m_dbHeadParts.Add(new PartsData() { button = btn });

                    idx++;
                }
            }

            m_popupDecal.actionItem = OnButton_Decal;
            // BUTTON
            transform.GetComponent<Button>("Canvas/Panel/Button/btn_reset").onClick.AddListener(()
                => OnButton_ResetCharacter());
            transform.GetComponent<Button>("Canvas/Panel/Button/btn_save").onClick.AddListener(()
                => OnButtonAsync_Save().Forget());
            transform.GetComponent<Button>("Canvas/Panel/Button/btn_random").onClick.AddListener(OnButton_Random);
            transform.GetComponent<Button>("Canvas/Panel/btn_head_decal").onClick.AddListener(() => m_popupDecal.gameObject.SetActive(true));
            transform.GetComponent<Button>("Canvas/Panel/btn_load").onClick.AddListener(()
                => m_popupLoad.LoadCharacters(trnsCharacterParts));
            transform.GetComponent<Button>("Canvas/Panel/btn_color").onClick.AddListener(OnButton_Color);

        }

        private void Start()
        {
            OnButton_ResetCharacter();
        }

        DateTime dtControll;
        private void Update()
        {
            if (m_infFildName.isFocused == true)
                return;

            bool isShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            if (Input.GetKeyDown(KeyCode.R))
                OnButton_Random();

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Slash))
            {
                if (m_curHeadParts.IsActive())
                {
                    var btn = m_dbHeadParts.Find(x => x.button.name == m_curHeadParts);
                    btn.index = 0;
                    btn.button.text = $"{btn.button.name.Replace("Grooming_", "G_")} {trnsCharacterHeadParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetHeadParts(m_curHeadParts, btn.index);
                }
                else if (m_curPartsType > EditCustomPartsType.NONE)
                {
                    var btn = m_dbParts[m_curPartsType];

                    btn.index = 0;
                    btn.button.text = $"{btn.button.name} {trnsCharacterParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetParts(m_curPartsType, btn.index);
                }
            }

            if (Input.GetKeyDown(KeyCode.UpArrow) || (isShift && Input.GetKey(KeyCode.UpArrow)) ||
                Input.GetKeyDown(KeyCode.W) || (isShift && Input.GetKey(KeyCode.W)))
            {
                if ((DateTime.Now - dtControll).TotalSeconds < 0.1f)
                    return;

                dtControll = DateTime.Now;

                if (m_curPartsType == EditCustomPartsType.NONE && m_curHeadParts.IsActive() == false)
                    m_curPartsType = EditCustomPartsType.NONE + 1;

                if (m_curPartsType > EditCustomPartsType.NONE)
                {
                    m_dbParts[m_curPartsType].button.SetDrawSelect(false);
                    m_curPartsType--;

                    if (m_curPartsType == EditCustomPartsType.NONE)
                    {
                        m_curHeadParts = m_dbHeadParts[m_dbHeadParts.Count - 1].button.name;
                        m_dbHeadParts.Find(x => x.button.name == m_curHeadParts).button.SetDrawSelect(true);
                    }
                    else
                        m_dbParts[m_curPartsType].button.SetDrawSelect(true);
                }
                else if (m_curHeadParts.IsActive() == true)
                {
                    var idx = m_dbHeadParts.FindIndex(x => x.button.name == m_curHeadParts);
                    m_dbHeadParts[idx].button.SetDrawSelect(false);
                    idx--;
                    if (idx < 0)
                    {
                        m_curHeadParts = "";
                        m_curPartsType = EditCustomPartsType.MAX - 1;
                        m_dbParts[m_curPartsType].button.SetDrawSelect(true);
                    }
                    else
                    {
                        m_dbHeadParts[idx].button.SetDrawSelect(true);
                        m_curHeadParts = m_dbHeadParts[idx].button.name;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.DownArrow) || (isShift && Input.GetKey(KeyCode.DownArrow)) ||
                Input.GetKeyDown(KeyCode.S) || (isShift && Input.GetKey(KeyCode.S)))
            {
                if ((DateTime.Now - dtControll).TotalSeconds < 0.1f)
                    return;

                dtControll = DateTime.Now;

                if (m_curPartsType == EditCustomPartsType.NONE && m_curHeadParts.IsActive() == false)
                    m_curHeadParts = m_dbHeadParts[m_dbHeadParts.Count - 1].button.name;

                if (m_curPartsType > EditCustomPartsType.NONE)
                {
                    m_dbParts[m_curPartsType].button.SetDrawSelect(false);
                    m_curPartsType++;

                    if (m_curPartsType == EditCustomPartsType.MAX)
                    {
                        m_curPartsType = EditCustomPartsType.NONE;
                        m_curHeadParts = m_dbHeadParts[0].button.name;
                        m_dbHeadParts.Find(x => x.button.name == m_curHeadParts).button.SetDrawSelect(true);
                    }
                    else
                        m_dbParts[m_curPartsType].button.SetDrawSelect(true);
                }
                else if (m_curHeadParts.IsActive() == true)
                {
                    var idx = m_dbHeadParts.FindIndex(x => x.button.name == m_curHeadParts);
                    m_dbHeadParts[idx].button.SetDrawSelect(false);
                    idx++;
                    if (idx == m_dbHeadParts.Count)
                    {
                        m_curHeadParts = "";
                        m_curPartsType = EditCustomPartsType.NONE + 1;
                        m_dbParts[m_curPartsType].button.SetDrawSelect(true);
                    }
                    else
                    {
                        m_dbHeadParts[idx].button.SetDrawSelect(true);
                        m_curHeadParts = m_dbHeadParts[idx].button.name;
                    }
                }
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || (isShift && Input.GetKey(KeyCode.LeftArrow)) ||
                Input.GetKeyDown(KeyCode.A) || (isShift && Input.GetKey(KeyCode.A)))
            {
                if ((DateTime.Now - dtControll).TotalSeconds < 0.1f)
                    return;

                dtControll = DateTime.Now;
                if (m_curHeadParts.IsActive())
                {
                    var btn = m_dbHeadParts.Find(x => x.button.name == m_curHeadParts);

                    btn.index--;
                    if (btn.index < 0)
                        btn.index = trnsCharacterHeadParts.Find(m_curHeadParts).childCount - 1;

                    btn.button.text = $"{btn.button.name.Replace("Grooming_", "G_")} {trnsCharacterHeadParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetHeadParts(m_curHeadParts, btn.index);
                }
                else if (m_curPartsType > EditCustomPartsType.NONE)
                {
                    var btn = m_dbParts[m_curPartsType];
                    m_prevPartsIndex = btn.index;
                    btn.index--;
                    if (btn.index < 0)
                        btn.index = trnsCharacterParts.Find(m_curPartsType.ToString()).childCount - 1;

                    btn.button.text = $"{btn.button.name} {trnsCharacterParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetParts(m_curPartsType, btn.index);
                }
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || (isShift && Input.GetKey(KeyCode.RightArrow)) ||
                Input.GetKeyDown(KeyCode.D) || (isShift && Input.GetKey(KeyCode.D)))
            {
                if ((DateTime.Now - dtControll).TotalSeconds < 0.1f)
                    return;

                dtControll = DateTime.Now;
                if (m_curHeadParts.IsActive())
                {
                    var btn = m_dbHeadParts.Find(x => x.button.name == m_curHeadParts);
                    btn.index++;

                    if (trnsCharacterHeadParts.Find(m_curHeadParts).childCount == btn.index)
                        btn.index = 0;

                    btn.button.text = $"{btn.button.name.Replace("Grooming_", "G_")} {trnsCharacterHeadParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetHeadParts(m_curHeadParts, btn.index);
                }
                else if (m_curPartsType > EditCustomPartsType.NONE)
                {
                    var btn = m_dbParts[m_curPartsType];
                    m_prevPartsIndex = btn.index;

                    btn.index++;
                    if (trnsCharacterParts.Find(m_curPartsType.ToString()).childCount == btn.index)
                        btn.index = 0;

                    btn.button.text = $"{btn.button.name} {trnsCharacterParts.Find(btn.button.name).GetChild(btn.index).name}";
                    SetParts(m_curPartsType, btn.index);

                }
            }
        }

        async UniTask OnButtonAsync_Save()
        {
            if (EditWorker_CustomHero.instance.HasFileAleady(m_infFildName.text))
            {
                var result = await PopupManager.instance.OpenModalAsync("이미 파일이 존재함.\n덮어 씌울꺼??", "확인", "취소");
                if (result != StatusType.Success)
                    return;
            }

            var newCharacter = Instantiate(m_objCustomCharacter, m_objCustomCharacter.transform.parent);

            var pCharacterParts = newCharacter.transform.Find("Character/Panel/Parts");
            for (int i = 0; i < pCharacterParts.childCount; i++)
            {
                var pParts = pCharacterParts.GetChild(i);
                for (int j = 0; j < pParts.childCount; j++)
                {
                    var objParts = pParts.GetChild(j).gameObject;
                    if (objParts.activeSelf == false)
                        Destroy(objParts);
                    else if (objParts.name.Equals("None"))
                    {
                        Destroy(pParts.gameObject);
                        break;
                    }
                }
            }

            var pCharacterHeadParts = pCharacterParts.Find("Head").GetChild(m_dbParts[EditCustomPartsType.Head].index);
            for (int i = 0; i < pCharacterHeadParts.childCount; i++)
            {
                var pParts = pCharacterHeadParts.GetChild(i);
                for (int j = 0; j < pParts.childCount; j++)
                {
                    var objParts = pParts.GetChild(j).gameObject;
                    if (objParts.activeSelf == false)
                        Destroy(objParts);
                    else if (objParts.name.Equals("None"))
                    {
                        Destroy(pParts.gameObject);
                        break;
                    }
                }
            }

            await UniTask.NextFrame();

            pCharacterParts.GetComponent<Animator>().runtimeAnimatorController = m_baseAnimController;

            if (EditWorker_CustomHero.instance.SavePrefab(newCharacter, m_infFildName.text) == true)
                PopupManager.instance.AlertShow("저장 성공!!");
            else
                PopupManager.instance.AlertShow("저장 실패!!");

            Destroy(newCharacter.gameObject);
        }

        void SetHeadParts(string _headParts, int _index)
        {
            var pParts = trnsCharacterHeadParts.Find(_headParts);

            for (int i = 0; i < pParts.childCount; i++)
                pParts.GetChild(i).gameObject.SetActive(i == _index);

        }
        void SetParts(EditCustomPartsType _partsType, int _index, bool _isReset = false)
        {
            var pParts = trnsCharacterParts.Find(_partsType.ToString());

            for (int i = 0; i < pParts.childCount; i++)
                pParts.GetChild(i).gameObject.SetActive(i == _index);

            if (_isReset == false && _partsType == EditCustomPartsType.Head)
            {
                var idxHeadParts = GetHeadParts();

                int idx = 0;
                foreach (var b in m_dbHeadParts)
                    SetHeadParts(b.button.name, idxHeadParts[idx++]);

                var pDecal = trnsCharacterHeadDecal;
                for (int i = 0; i < m_popupDecal.buttons.Count; i++)
                {
                    pDecal.GetChild(i).gameObject.SetActive(m_popupDecal.buttons[i].isDrawSelect);
                }
            }
        }

        List<int> GetHeadParts(GameObject _character = null)
        {
            List<int> result = new();

            var headParts = _character ? _character.transform.Find("Character/Panel/Parts/Head").GetChild(0)
                : trnsCharacterHeadParts;

            if (_character == null)
                foreach (var d in m_dbHeadParts)
                    result.Add(d.index);
            else
            {
                var baseHeadParts = trnsCharacterHeadParts;
                foreach (var d in m_dbHeadParts)
                {
                    var partsItemName = headParts.Find(d.button.name).GetChild(0).name;

                    var parts = baseHeadParts.Find(d.button.name);
                    for (int i = 0; i < parts.childCount; i++)
                    {
                        if (parts.GetChild(i).name == partsItemName)
                        {
                            result.Add(i);
                            break;
                        }
                    }
                }
            }

            return result;
        }

        void OnButton_ResetCharacter(LoadData _lodaData = null)
        {
            if (m_objCustomCharacter != null)
                Destroy(m_objCustomCharacter);

            m_objCustomCharacter = Instantiate(m_objBaseCharacter, m_objBaseCharacter.transform.parent);
            m_objCustomCharacter.gameObject.SetActive(true);
            m_objCustomCharacter.GetComponent<CharacterComponent>().SetHeroData_Test();

            m_infFildName.text = _lodaData?.fileName ?? "NONE";

            foreach (var b in m_dbParts)
            {
                var btn = b.Value;

                btn.index = _lodaData?.dbParts.GetValueOrDefault(b.Key) ?? 0;
                btn.button.text = $"{btn.button.name} {trnsCharacterParts.Find(btn.button.name).GetChild(btn.index).name}";
                btn.button.SetDrawSelect(false);

                SetParts(b.Key, btn.index, true);
            }

            foreach (var btn in m_dbHeadParts)
            {
                btn.index = _lodaData?.dbHeadParts.GetValueOrDefault(btn.button.name) ?? 0;
                btn.button.text = $"{btn.button.name.Replace("Grooming_", "G_")} {trnsCharacterHeadParts.Find(btn.button.name).GetChild(btn.index).name}";
                btn.button.SetDrawSelect(false);

                if (_lodaData?.fileName != null)
                    SetHeadParts(btn.button.name, btn.index);
            }

            for (int i = 0; i < trnsCharacterHeadDecal.childCount; i++)
            {
                bool isActive = _lodaData?.idxDecal?.Contains(i) ?? false;
                trnsCharacterHeadDecal.GetChild(i).gameObject.SetActive(isActive);
                m_popupDecal.buttons[i].SetDrawSelect(isActive);
            }

            m_curHeadParts = "";
            m_curPartsType = EditCustomPartsType.NONE;

            if (_lodaData?.fileName != null)
            {
                m_characterColor = _lodaData?.color ?? Color.white;
                SetColor();
            }
        }

        void OnButton_HeadParts()
        {
            foreach (var b in m_dbHeadParts)
                b.button.SetDrawSelect(b.button.name == m_curHeadParts);
        }

        void OnButton_Parts()
        {
            foreach (var b in m_dbParts)
                b.Value.button.SetDrawSelect(b.Key == m_curPartsType);
        }

        void OnButton_Decal(string _itemName, bool _isActive)
        {
            trnsCharacterHeadDecal.Find(_itemName).gameObject.SetActive(_isActive);
        }

        void OnButton_Random()
        {
            foreach (var btn in m_dbHeadParts)
            {
                btn.index = UnityEngine.Random.Range(0, trnsCharacterHeadParts.Find(btn.button.name).childCount);
                btn.button.text = $"{btn.button.name.Replace("Grooming_", "G_")} {trnsCharacterHeadParts.Find(btn.button.name).GetChild(btn.index).name}";
                btn.button.SetDrawSelect(false);

                SetHeadParts(btn.button.name, btn.index);
            }

            foreach (var b in m_dbParts)
            {
                var btn = b.Value;

                btn.index = UnityEngine.Random.Range(0, trnsCharacterParts.Find(btn.button.name).childCount);
                btn.button.text = $"{btn.button.name} {trnsCharacterParts.Find(btn.button.name).GetChild(btn.index).name}";
                btn.button.SetDrawSelect(false);

                SetParts(b.Key, btn.index);
            }

            for (int i = 0; i < trnsCharacterHeadDecal.childCount; i++)
            {
                bool isActive = UnityEngine.Random.value < 0.5f;
                trnsCharacterHeadDecal.GetChild(i).gameObject.SetActive(isActive);
                m_popupDecal.buttons[i].SetDrawSelect(isActive);
            }

            m_curHeadParts = "";
            m_curPartsType = EditCustomPartsType.NONE;
        }

        Color m_characterColor = Color.white;
        void OnButton_Color()
        {
#if UNITY_EDITOR
            Editor_ColorPicker.Open((selectedColor) =>
            {
                m_characterColor = selectedColor;
                SetColor();
            }, m_characterColor);
#endif
        }

        void SetColor()
        {
            var pParts = trnsCharacterParts;
            for (var i = 0; i < pParts.childCount; i++)
            {
                for (int j = 0; j < pParts.GetChild(i).childCount; j++)
                {
                    var pPart = pParts.GetChild(i).GetChild(j);
                    var imgPart = pPart.GetComponent<SpriteRenderer>("Color");
                    if (imgPart != null)
                    {
                        var color = m_characterColor;
                        color.a = imgPart.color.a;
                        imgPart.color = color;
                    }
                }
            }

            var pHeadParts = trnsCharacterHeadParts;
            for (var i = 0; i < pHeadParts.childCount; i++)
            {
                for (int j = 0; j < pHeadParts.GetChild(i).childCount; j++)
                {
                    var pPart = pHeadParts.GetChild(i).GetChild(j);
                    var imgPart = pPart.GetComponent<SpriteRenderer>("Color");
                    if (imgPart != null)
                    {

                        var color = m_characterColor;
                        color.a = imgPart.color.a;
                        imgPart.color = color;
                    }
                }
            }
        }

        private void OnApplicationQuit()
        {
            WorkerManager.Release();
        }
    }

    public enum EditCustomPartsType
    {
        NONE = -1,

        Body,
        Head,
        Weapon,
        Sub,

        MAX
    }
}