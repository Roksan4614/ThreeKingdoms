using UnityEngine;

public class Configure : MonoSingleton<Configure>
{
    public bool isBooted { get; set; }

    // 서버시간 - 로컬시간 = 서버와 로컬과의 시간차
    public float timeGapFromServer { get; set; }

    bool m_isPC;
    public static bool isPC => instance.m_isPC;

    public void SetPC(bool _isPC) => m_isPC = _isPC;
}
