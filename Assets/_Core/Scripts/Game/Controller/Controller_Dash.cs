using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Threading;
using UnityEngine;

public partial class ControllerManager
{
    int m_dashRemainCount = 0;

    CancellationTokenSource m_ctsDash;

    void DashButtonInitalize()
    {
        if (m_isKeyboardMode)
        {

        }
    }

    public void OnButton_Dash(bool _isMouse)
    {
        if (isSwitch == false || m_dashRemainCount == 0 || m_mainHero.isLive == false)
            return;

        m_dashRemainCount--;
        UpdateDashCount();

        var targetPos = Vector3.zero;
        if (_isMouse)
            targetPos = CameraManager.instance.GetMousePosition();

        m_mainHero.move.Dash(targetPos);

        m_element.panelDash.localScale = Vector3.one;
        m_element.panelDash.DOPunchScale(Vector3.one * .1f, 0.1f);
    }

    public async UniTask DashTimerStartAsync()
    {
        if (m_ctsDash != null)
        {
            m_ctsDash.Cancel();
            m_ctsDash.Dispose();
        }
        m_ctsDash = new();
        var token = m_ctsDash.Token;

        m_dashRemainCount = 0;
        UpdateDashCount();

        float startTime = 0, endTime = 0;

        float cooldown = 1f; //m_mainHero.data.dashCooldown * m_mainHero.data.dashCooldownRate;
        int mspaceValue = (int)(m_element.txtDashTimer.fontSize * 0.5f);

        while (true)
        {
            if (m_dashRemainCount < 2)
            {
                if (m_element.imgDashTimer.gameObject.activeSelf == false)
                {
                    startTime = Time.time;
                    endTime = cooldown + startTime;

                    m_element.imgDashTimer.gameObject.SetActive(true);
                    m_element.txtDashTimer.gameObject.SetActive(true);
                }

                // 퍼센트 구하기!!
                float progress = (Time.time - startTime) / (endTime - startTime);

                if (progress > 1f)
                {
                    var iconCount = m_element.iconDashCount[m_dashRemainCount];
                    iconCount.transform.DOPunchScale(Vector3.one * 0.1f, 0.2f);

                    m_dashRemainCount++;
                    UpdateDashCount();

                    startTime = Time.time;
                    endTime = cooldown + startTime;
                }
                else
                {
                    var RemainTime = endTime - Time.time;

                    m_element.txtDashTimer.text =
                        Utils.MSpace(RemainTime.ToString(RemainTime > 10 ? "0" : "0.0"), mspaceValue);
                    m_element.imgDashTimer.fillAmount = 1 - progress;
                }
            }
            else if (m_element.imgDashTimer.gameObject.activeSelf == true)
            {
                m_element.imgDashTimer.gameObject.SetActive(false);
                m_element.txtDashTimer.gameObject.SetActive(false);
            }

            await UniTask.WaitForEndOfFrame(cancellationToken: token);
        }
    }

    void UpdateDashCount()
    {
        for (int i = 0; i < m_element.iconDashCount.Count; i++)
            m_element.iconDashCount[i].SetActive(i < m_dashRemainCount);
    }
}
