using System;
using System.Collections;
using UnityEngine;

public class Weapon_Champion_Guanyu : Weapon_Champion
{
    private void Start()
    {
        m_durationSkill = 10f;
        m_dtOpenSkill.AddSeconds(m_durationSkill);
    }
}
