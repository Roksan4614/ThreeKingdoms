using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_StringTalk : Table_String
{
    public Table_StringTalk(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }

    public Queue<TableStringData> GetTalk(string _key, bool _isPC)
    {
        var key = _key + "_";

        Queue<TableStringData> result = new (m_list.Where(x => x.key.StartsWith(key))
            .Where(x =>
            {
                var split = x.key.Replace(key, "").Split("_");
                if (split.Length == 1)
                    return true;
                else if (split[1] == (_isPC ? "MOBILE" : "PC"))
                    return false;
                return true;
            }).ToList());

        return result;
    }
}
