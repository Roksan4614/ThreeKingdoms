using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_String_Talk : Table_String_Base
{
    public Table_String_Talk(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public Queue<TableStringData> GetTalk(string _key, bool _isWithLast = false)
    {
        var key = _key + "_";

        Queue<TableStringData> result = new(m_list.FindAll(x => x.key.StartsWith(key))
            .FindAll(x =>
            {
                var split = x.key.Replace(key, "").Split("_");
                if (split.Length == 1)
                    return true;
                else if (_isWithLast == false || split[1] == (Configure.isPC ? "PC" : "MOBILE"))
                    return false;
                return true;
            }));

        return result;
    }

    public List<TableStringData> GetTalkAfterQuestion(string _key, int _index)
    {
        var key = _key.ToUpper() + "_";

        return m_list.FindAll(x => x.key.StartsWith(key.ToUpper()))
            .FindAll(x =>
            {
                var split = x.key.Replace(key, "").Split("_");
                if (split.Length > 1 && split[1] == _index.ToString())
                    return true;
                return false;
            });
    }
}
