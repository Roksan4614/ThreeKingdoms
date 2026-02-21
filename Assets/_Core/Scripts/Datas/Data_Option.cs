using Cysharp.Threading.Tasks;
using UnityEngine;

public enum OptionType
{

}

public enum LanguegeType
{
    Korean,
    English,
}

public class Data_Option
{
    public OptionData data { get; private set; }
    public void Initialize()
    {
        data = PPWorker.Get<OptionData>(PlayerPrefsType.OPTION, false);
    }

    public struct OptionData
    {
        public string hash;
        public LanguegeType languge;

        public bool isActive => hash.IsNullOrEmpty() == false;
    }
}
