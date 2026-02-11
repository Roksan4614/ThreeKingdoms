using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{

    Camera m_camera;

    [SerializeField] float m_smoothFactor = 5f;
    [SerializeField] float m_distMax = 1f;
    [SerializeField] float m_posCameraY = 3f;

    [SerializeField] Transform m_playerCameraPos;

    private void LateUpdate()
    {
        CameraMove();
    }

    public void CameraMove()
    {
        if (m_camera == null)
            m_camera = Camera.main;

        var cameraPos = m_camera.transform.position;

        // 땅에 있고, 아래를 눌르면 카메라를 아래로 내려서 보여주자
        {
            //if (Input.GetKey(KeyCode.DownArrow) && m_player.isGround)
            //    targetPos.y -= 5f;
            //else
            //targetPos.y += m_posCameraY;
        }

        // 카메라 바운스 체크
        var targetPos = MapManager.instance.GetBounceHorizontalPos(m_playerCameraPos.position);

        if (Vector2.Distance(targetPos, m_camera.transform.position) < 0.01f)
            return;

        targetPos.z = cameraPos.z;

        Vector3 posCamera = Vector3.Lerp(
            cameraPos,
            targetPos,
            m_smoothFactor * Time.deltaTime
        );

        //if (m_isColliderVerti == false)
        //{
        //    var dy = cameraPos.y - targetPos.y;
        //    if (Math.Abs(dy) > m_distMax)
        //        posCamera.y = targetPos.y + Math.Clamp(dy, -m_distMax, m_distMax);
        //}

        m_camera.transform.position = posCamera;
    }
}
