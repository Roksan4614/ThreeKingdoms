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

    float m_addPosY = 0;
    float m_addSmoothFactor = 0;

    float m_addPosY_Landscape = 0;

    private void Start()
    {
        DontDestroyOnLoad(this);

        Signal.instance.ConnectMainHero.connectLambda =
            new(this, _ => m_playerCameraPos = _.element.cameraPos);

        Signal.instance.ChangeDisplayMode.connectLambda =
            new(this, _isLandscape => {
                m_camera.fieldOfView = _isLandscape ? 100 : 110;
                m_addPosY_Landscape = _isLandscape ? 1 : 0;
            });
    }

    private void LateUpdate()
    {
        if (m_camera == null || MapManager.instance == null || m_playerCameraPos == null)
            return;

        if (m_isShake == false)
            CameraMove();
    }

    public void SetCameraPosTarget(Transform _target = null, bool _isForce = true)
    {
        if (_target != null)
            m_playerCameraPos = _target;

        CameraMove(_isForce);
    }

    public void CameraMove(bool _isForce = false)
    {
        // 카메라 바운스 체크
        var targetPos = MapManager.instance.GetBounceHorizontalPos(m_playerCameraPos.position);
        targetPos.y += m_addPosY + m_addPosY_Landscape;

        if (Vector2.Distance(targetPos, m_camera.transform.position) < 0.01f)
        {
            m_addSmoothFactor = 0;
            return;
        }

        var cameraPos = m_camera.transform.position;
        targetPos.z = cameraPos.z;

        Vector3 posCamera = _isForce ? targetPos : Vector3.Lerp(
            cameraPos,
            targetPos,
            (c_smoothFactor + m_addSmoothFactor) * Time.deltaTime
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

        //if (ControllerManager.instance.isDoing == true && _isForceShake == false)
        //    return;

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

        var mousePos = Input.touchCount > 1 ? (Vector3)Input.GetTouch(Input.touchCount - 1).position : Input.mousePosition;
        mousePos.z = -m_camera.transform.position.z;
        var pos = m_camera.ScreenToWorldPoint(mousePos);
        pos.z = 0;

        return pos;
    }

    public void SetAddPosY(float _addPosY, float _addSmoothFactor)
    {
        m_addPosY = _addPosY;
        m_addSmoothFactor = _addSmoothFactor;
    }
}
