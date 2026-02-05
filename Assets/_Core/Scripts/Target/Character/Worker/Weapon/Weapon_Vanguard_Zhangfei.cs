using System;
using System.Collections;
using UnityEngine;

public class Weapon_Vanguard_Zhangfei : Character_Weapon
{
    private void Start()
    {
        m_durationSkill = 10f;
        m_dtOpenSkill.AddSeconds(m_durationSkill);
    }
}
