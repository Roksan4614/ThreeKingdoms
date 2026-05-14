using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LobbyScreen_Castle_NPCManager : Singleton<LobbyScreen_Castle_NPCManager>, IValidatable
{
    float m_randomPosRange = 30;
    System.DateTime m_dtSpawn;

    protected override void OnAwake()
    {
        m_element.npc.transform.SetParent(transform);
        m_element.npc.gameObject.SetActive(false);

        //test
        var test = transform.GetComponent<LobbyScreen_Castle_NPCComponent>("List/Test");
        test.Initialize(m_element.localPosTop.y, m_element.localPosBottom.y);
        test.transform.SetParent(test.transform.parent.parent);
    }

    void OnEnable()
        => SetSpawnNPC();

    void OnDisable()
        => m_dtSpawn = System.DateTime.Now.AddMinutes(1f);

    void SetSpawnNPC()
    {
        bool isRebatch = m_dtSpawn < System.DateTime.Now;

        var parent = m_element.parentList;
        int i = 0, max = 0;
        for (int idxStreet = 0; idxStreet < m_element.streets.Count; idxStreet++)
        {
            //max += idxStreet == 0 ? Random.Range(5, 8) : 2;
            max += idxStreet == 0 ? 5 : 2;

            for (; i < max; i++)
            {
                bool isNew = i == parent.childCount;
                var npc = isNew ? Instantiate(m_element.npc, parent)
                    : parent.GetChild(i).GetComponent<LobbyScreen_Castle_NPCComponent>();

                if (isNew)
                {
                    npc.Initialize(m_element.localPosTop.y, m_element.localPosBottom.y);
                }

                int idxPos = Random.Range(0, m_element.streets[idxStreet].posTargets.Count);
                if (isRebatch)
                {
                    npc.Spawn(GetTargetPosition(
                        idxStreet,
                        idxPos,
                        out idxPos));
                }

                npc.StartAsync(idxStreet, idxPos).Forget();
            }
        }

        for (; i < parent.childCount; i++)
            parent.GetChild(i).gameObject.SetActive(false);
    }

    public Vector3 GetTargetPosition(int _idxStreet, int _idxTarget, out int _resultIdxTarget)
    {
        if (m_element.streets.Count <= _idxStreet)
        {
            _resultIdxTarget = -1;
            return Vector3.zero;
        }

        bool isMerchant = _idxStreet == 2;

        var array = m_element.streets[_idxStreet].posTargets;

        bool isUp = Random.value > .5f;
        int idx = _idxTarget;

        int moveCount = isMerchant ? 1
            : Random.Range(1, Mathf.Min(5, array.Count - 1));

        int lastIdx = array.Count - 1;
        if (isUp)
        {
            idx -= moveCount;
            if (idx < 0)
            {
                if (isMerchant)
                    idx = lastIdx;
                else
                    idx *= -1;
            }

            if (_idxTarget == idx)
                idx += idx == 0 ? 1 : -1;
        }
        else
        {
            idx += moveCount;

            if (idx > lastIdx)
            {
                if (isMerchant)
                    idx = 0;
                else
                {
                    idx = lastIdx - (idx - lastIdx);
                    idx = Mathf.Max(0, _idxTarget);
                }

            }

            if (_idxTarget == idx)
                idx += idx == lastIdx ? -1 : 1;
        }

        _resultIdxTarget = idx;

        var result = array[_resultIdxTarget];
        var randomValue = m_randomPosRange;

        if (_idxStreet > 0)
            randomValue *= .5f;

        return result +
            new Vector3(Random.Range(-randomValue, randomValue),
            Random.Range(-randomValue, randomValue));
    }

    #region VALIDATE
    public void OnManualValidate() => m_element.Initialize(transform);

    [SerializeField, HideInInspector]
    ElementData m_element;

    [System.Serializable]
    struct ElementData
    {
        public List<StreetData> streets;

        public LobbyScreen_Castle_NPCComponent npc;

        public Vector3 localPosTop;
        public Vector3 localPosBottom;

        public Transform parentList;

        public void Initialize(Transform _transform)
        {
            var street = _transform.Find("Street");

            streets = new();
            for (int i = 0; i < street.childCount; i++)
            {
                StreetData streetData = new();
                streetData.idx = i;
                streetData.posTargets = street.GetChild(i).GetComponentsInChildren<Transform>()
                .Skip(1).Select(x => x.localPosition).ToList();
                streets.Add(streetData);
            }

            npc = _transform.GetComponent<LobbyScreen_Castle_NPCComponent>("List/Castle_NPC_Default");
            parentList = npc.transform.parent;

            localPosTop = _transform.Find("pos_npc_top").localPosition;
            localPosBottom = _transform.Find("pos_npc_bottom").localPosition;
        }
    }

    [System.Serializable]
    struct StreetData
    {
        public int idx;
        public List<Vector3> posTargets;
    }

    #endregion VALIDATE

}
