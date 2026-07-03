using System.Collections.Generic;
using UnityEngine;

public class Table_String_Story : Table_String_Base
{
    public Table_String_Story(List<TableStringData> _table) : base(_table)
    {
        SetDictionary(x => x.key);
    }
}
