using System;
using System.Collections;
using UnityEngine;

public class Weapon_Sentinel_Zhayun : Weapon_Sentinel
{
    private void Start()
    {
        m_durationSkill = 10f;
        m_dtOpenSkill.AddSeconds(m_durationSkill);
    }
}
