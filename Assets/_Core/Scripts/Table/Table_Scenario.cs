using System.Collections.Generic;
using UnityEngine;

public class Table_Scenario : BaseTable<string, TableScenarioData>
{
    public Table_Scenario(List<TableScenarioData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public TableScenarioData GetScenario(StageManager.LoadData_Stage _stage, int _phaseIdx, bool _isStart)
        => Get(_stage.GetKey(_phaseIdx, _isStart));

    public string[] GetNextScenarios(string _key)
        => Get(_key).nextScenario.Replace(" ", "").Split(',');

    public string GetChoiceTextNextScenario(string _key)
        => TableManager.stringScenario.GetString($"CHOICE_{_key}");
}

public enum ScenarioStartType
{
    Start, End,
}

public struct TableScenarioData
{
    public string key;
    public string fromStage;
    public string nextScenario;

    public bool isActive => key.IsNullOrEmpty() == false;
}
