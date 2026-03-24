using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_StringTalk : Table_String
{
    public Table_StringTalk(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public Queue<TableStringData> GetTalk(string _key, bool _isPC, bool _isWithLast = true)
    {
        var key = _key + "_";

        Queue<TableStringData> result = new(m_list.Where(x => x.key.StartsWith(key))
            .Where(x =>
            {
                var split = x.key.Replace(key, "").Split("_");
                if (split.Length == 1)
                    return true;
                else if (_isWithLast == false || split[1] == (_isPC ? "MOBILE" : "PC"))
                    return false;
                return true;
            }).ToList());

        return result;
    }

    public List<TableStringData> GetTalkAfterQuestion(string _key, int _index)
    {
        var key = _key + "_";

        return m_list.Where(x => x.key.StartsWith(key))
            .Where(x =>
            {
                var split = x.key.Replace(key, "").Split("_");
                if (split.Length > 1 && split[1] == _index.ToString())
                    return true;
                return false;
            }).ToList();
    }
}
