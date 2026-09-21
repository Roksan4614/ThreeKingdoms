using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table_String_Story : Table_String_Base
{
    public Table_String_Story(List<TableStringData> _table) : base(_table)
    {
        //m_dictionary = m_list.ToDictionary(x => x.key, x => x, StringComparer.Ordinal);
    }
}
