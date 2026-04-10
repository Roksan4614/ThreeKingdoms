using Cysharp.Threading.Tasks;
using UnityEngine;

public class Data_Stat
{
    public Data_Stat_Relic relic { get; private set; } = new();

    public async UniTask InitializeAsync()
    {
        await relic.InitializeAsync();
    }
}
