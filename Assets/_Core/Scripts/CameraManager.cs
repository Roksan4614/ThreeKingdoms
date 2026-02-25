using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{

    Camera m_camera;

    [SerializeField] float m_smoothFactor = 5f;
    //[SerializeField] float m_distMax = 1f;
    //[SerializeField] float m_posCameraY = 3f;

    [SerializeField] Transform m_playerCameraPos;

    private void Start()
    {
        Signal.instance.ConnectMainHero.connectLambda =
            new(this, _ => m_playerCameraPos = _.element.cameraPos);
    }

    private void LateUpdate()
    {
#if UNITY_EDITOR
        if (MapManager.instance == null || m_playerCameraPos == null)
            return;
#endif

        if (m_isShake == false)
            CameraMove();

        if (Input.GetKeyDown(KeyCode.Z))
            Shake();
    }

    public void SetCameraPosTarget()
        => CameraMove(true);

    public void CameraMove(bool _isForce = false)
    {
        if (m_camera == null)
            m_camera = Camera.main;


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

        var cameraPos = m_camera.transform.position;
        targetPos.z = cameraPos.z;

        Vector3 posCamera = _isForce ? targetPos : Vector3.Lerp(
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

    bool m_isShake;
    public void Shake()
    {
        ShakeAsync().Forget();
    }

    Tween m_tween;
    public async UniTask ShakeAsync()
    {
        m_tween?.Kill();

        if (ControllerManager.instance.isActive == true)
            return;

        int count = 3;
        m_isShake = true;
        while (count > 0)
        {
            m_tween = m_camera.DOShakePosition(.05f, 0.1f, 5);
            await m_tween.AsyncWaitForCompletion();
            count--;
        }
        m_isShake = false;
    }
}
