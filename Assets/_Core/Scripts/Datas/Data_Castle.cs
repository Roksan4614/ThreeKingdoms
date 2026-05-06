using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Data_Castle
{
    Dictionary<CastleObjectType, CastleData> m_db;
    IReadOnlyDictionary<CastleObjectType, CastleData> db => m_db;

    const string c_key = "pp_castle_data";

    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();

        var data = PPWorker.Get<List<CastleData>>(c_key);
        if (data == null)
            data = new();

        m_db = data.ToDictionary(x => x.type, x => x);
    }

    public CastleData GetCaslteData(CastleObjectType _type)
    {
        if (m_db.ContainsKey(_type))
            return m_db[_type];

        UpdateBatchHero(new()
        {
            type = _type,
            heroes = new()
        });

        return GetCaslteData(_type);
    }

    public void UpdateBatchHero(CastleData _data)
    {
        if (m_db.ContainsKey(_data.type))
            m_db[_data.type] = _data;
        else
            m_db.Add(_data.type, _data);

        SaveData();
    }

    void SaveData()
    {
        PPWorker.Set(c_key, m_db.Values.ToList());
    }

    public struct CastleData
    {
        public CastleObjectType type;
        public List<string> heroes;
        public int level;
    }
}

