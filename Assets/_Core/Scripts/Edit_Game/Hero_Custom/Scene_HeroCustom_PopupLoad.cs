using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Edit.CustomCharacter
{
    public class LoadData
    {
        public string fileName;
        public Dictionary<EditCustomPartsType, int> dbParts;
        public Dictionary<string, int> dbHeadParts;
        public List<int> idxDecal;
        public Color? color;
    }

    public class Scene_HeroCustom_PopupLoad : MonoBehaviour
    {
        public UnityAction<LoadData> actionLoad { get; set; }

        ScrollRect m_scroll;
        Scene_HeroCustom_PopupLoad_Slot m_baseSlot;
        Transform m_baseCharacterParts;

        private void Awake()
        {
            transform.GetComponent<Button>("Dimm").onClick.AddListener(() => gameObject.SetActive(false));

            m_scroll = transform.GetComponent<ScrollRect>("Panel");
            m_baseSlot = m_scroll.content.GetChild(0).GetComponent<Scene_HeroCustom_PopupLoad_Slot>();
            m_baseSlot.gameObject.SetActive(false);
            m_baseSlot.transform.SetParent(m_scroll.viewport);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                gameObject.SetActive(false);
        }

        public void LoadCharacters(Transform _baseParts)
        {
            gameObject.SetActive(true);

            m_baseCharacterParts = _baseParts;

            var prefabs = EditWorker_CustomHero.instance.GetHeroPrefabPaths();
            var content = m_scroll.content;
            int i = 0;
            for (; i < prefabs.Count; i++)
            {
                var slot = (i == content.childCount ? Instantiate(m_baseSlot, content) : content.GetChild(i).GetComponent<Scene_HeroCustom_PopupLoad_Slot>());
                slot.actionParts = OnButton;
                slot.SetData(prefabs[i]);
            }
            for (; i < content.childCount; i++)
                content.GetChild(i).gameObject.SetActive(false);

            content.ForceRebuildLayout();

            m_scroll.viewport.Find("Empty").gameObject.SetActive(prefabs.Count == 0);
        }

        public void OnButton(string _fileName, Transform _parts)
        {
            LoadData loadData = new();
            loadData.fileName = _fileName;

            loadData.dbParts = new();
            loadData.dbHeadParts = new();
            loadData.idxDecal = new();

            UnityAction<Transform> actionColor = _trns =>
            {
                if (loadData.color == null)
                {
                    var color = _trns.GetComponent<SpriteRenderer>("Color").color;
                    loadData.color = color;
                }
            };

            for (var t = EditCustomPartsType.NONE + 1; t < EditCustomPartsType.MAX; t++)
            {
                var partsName = _parts.Find(t.ToString())?.GetChild(0).name;
                var baseParts = m_baseCharacterParts.Find(t.ToString());

                for (int i = 0; i < baseParts.childCount; i++)
                {
                    if (baseParts.GetChild(i).name == partsName)
                    {
                        loadData.dbParts.Add(t, i);
                        actionColor(baseParts.GetChild(i));
                        break;
                    }
                }
            }

            var pHead = _parts.Find("Head").GetChild(0);
            var pBaseHead = m_baseCharacterParts.Find("Head");
            for (int i = 0; i < pBaseHead.childCount; i++)
            {
                if (pBaseHead.GetChild(i).gameObject.activeSelf == true)
                {
                    pBaseHead = pBaseHead.GetChild(i);
                    break;
                }
            }

            for (var i = 0; i < pHead.childCount; i++)
            {
                var p = pHead.GetChild(i);
                if (p.childCount == 0)
                    continue;
                var partsName = pHead.GetChild(i).GetChild(0).name;

                var ph = pBaseHead.Find(p.name);
                if (ph == null)
                    continue;

                for (var j = 0; j < ph.childCount; j++)
                {
                    if (ph.GetChild(j).name.Equals(partsName))
                    {
                        actionColor(pHead.GetChild(i));
                        loadData.dbHeadParts.Add(pHead.GetChild(i).name, j);
                        break;
                    }
                }
            }

            var pDecal = pHead.Find("Decal");
            var pBaseDecal = pBaseHead.Find("Decal");
            for (int i = 0; i < pDecal.childCount; i++)
            {
                var decalName = pDecal.GetChild(i).name;

                for (int j = 0; j < pBaseDecal.childCount; j++)
                {
                    if (pBaseDecal.GetChild(j).name.Equals(decalName))
                    {
                        loadData.idxDecal.Add(j);
                        break;
                    }
                }
            }

            loadData.color ??= Color.white;

            actionLoad(loadData);
            gameObject.SetActive(false);
        }
    }

}