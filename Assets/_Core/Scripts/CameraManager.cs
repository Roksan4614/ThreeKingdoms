using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
    [SerializeField]
    Camera m_camera;
    public Camera main => m_camera;
    const float c_smoothFactor = 5f;

    [SerializeField] Transform m_playerCameraPos;

    private void Start()
    {
        DontDestroyOnLoad(this);

        Signal.instance.ConnectMainHero.connectLambda =
            new(this, _ => m_playerCameraPos = _.element.cameraPos);
    }

    private void LateUpdate()
    {
        if (m_camera == null || MapManager.instance == null || m_playerCameraPos == null)
            return;

        if (m_isShake == false)
            CameraMove();
    }

    public void SetCameraPosTarget()
        => CameraMove(true);

    public void CameraMove(bool _isForce = false)
    {
        // 카메라 바운스 체크
        var targetPos = MapManager.instance.GetBounceHorizontalPos(m_playerCameraPos.position);

        if (Vector2.Distance(targetPos, m_camera.transform.position) < 0.01f)
            return;

        var cameraPos = m_camera.transform.position;
        targetPos.z = cameraPos.z;

        Vector3 posCamera = _isForce ? targetPos : Vector3.Lerp(
            cameraPos,
            targetPos,
            c_smoothFactor * Time.deltaTime
        );

        m_camera.transform.position = posCamera;
    }

    bool m_isShake;
    public void Shake(bool _isForceShake = false)
    {
        ShakeAsync(_isForceShake).Forget();
    }

    Tween m_tween;
    public async UniTask ShakeAsync(bool _isForceShake = false)
    {
        m_tween?.Kill();

        if (ControllerManager.instance.isActive == true && _isForceShake == false)
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

    public Vector3 GetMousePosition()
    {
        if (m_camera == null)
            return Vector3.zero;

        var mousePos = Input.mousePosition;
        mousePos.z = -m_camera.transform.position.z;
        var pos = m_camera.ScreenToWorldPoint(mousePos);
        pos.z = 0;

        return pos;
    }
}
