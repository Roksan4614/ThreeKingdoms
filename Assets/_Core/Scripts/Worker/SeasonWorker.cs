using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SeasonWorker
{
    static SeasonWorker m_instance;
    public static SeasonWorker instance => m_instance ??= new();
    public static void Release()
        => m_instance = null;

    const string c_key = "pp_season_data";
    SeasonData m_data;

    public System.DateTime dtEnd => m_data.dtEnd;

    public async UniTask InitailizeAsync()
    {
        m_data = PPWorker.Get<SeasonData>(c_key, false);

        if (m_data == null)
        {
            await UniTask.NextFrame();
            m_data = new();
            m_data.idx = 1;
            m_data.tickEnd = Utils.GetNextMonthMidnight(1).Ticks;
            m_data.tickStart = m_data.dtEnd.AddMonths(-1).Ticks;
            SaveData();
        }
    }

    void SaveData()
        => PPWorker.Set(c_key, m_data, false);

    [JsonObject(MemberSerialization.OptIn)]
    class SeasonData
    {
        [JsonProperty] public int idx;

        [JsonProperty] public long tickStart;
        [JsonProperty] public long tickEnd;

        public System.DateTime dtStart => Utils.GetDateTime(tickStart);
        public System.DateTime dtEnd => Utils.GetDateTime(tickEnd);
    }
}
