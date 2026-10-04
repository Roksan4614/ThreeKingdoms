using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Rev9.Edit.CustomCharacter
{

    public class Scene_HeroCustom_PopupLoad : MonoBehaviour
    {
        public UnityAction<string, Dictionary<EditCustomPartsType, int>, Dictionary<string, int>, List<int>> actionLoad { get; set; }

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
            Dictionary<EditCustomPartsType, int> dbParts = new();
            Dictionary<string, int> dbHeadParts = new();
            List<int> idxDecal = new();

            for (var t = EditCustomPartsType.NONE + 1; t < EditCustomPartsType.MAX; t++)
            {
                var partsName = _parts.Find(t.ToString())?.GetChild(0).name;
                var baseParts = m_baseCharacterParts.Find(t.ToString());

                for (int i = 0; i < baseParts.childCount; i++)
                {
                    if (baseParts.GetChild(i).name == partsName)
                    {
                        dbParts.Add(t, i);
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
                        dbHeadParts.Add(pHead.GetChild(i).name, j);
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
                        idxDecal.Add(j);
                        break;
                    }
                }
            }

            actionLoad(_fileName, dbParts, dbHeadParts, idxDecal);
            gameObject.SetActive(false);
        }
    }

}