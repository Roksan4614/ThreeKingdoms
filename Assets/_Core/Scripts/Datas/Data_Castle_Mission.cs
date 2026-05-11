using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class Data_Castle_Mission
{
    public async UniTask InitializeAsync()
    {
        await UniTask.Yield();


    }

    public struct CastleMissionData
    {
        public string key;
        public List<string> heroes;
        public GradeType grade;
        public long tickStart;

        TableCastleMissionData m_dbData;
        public TableCastleMissionData dbData
        {
            get
            {
                if (m_dbData.isActive == false)
                    m_dbData = TableManager.castleMisson.Get(key);
                return m_dbData;
            }
        }
    }
}
