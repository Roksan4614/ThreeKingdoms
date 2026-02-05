using UnityEngine;

public class Weapon_Commander_Liubei : Weapon_Commander
{
    private void Start()
    {
        m_durationSkill = 10f;
        m_dtOpenSkill.AddSeconds(m_durationSkill);
    }
}
