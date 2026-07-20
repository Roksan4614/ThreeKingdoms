using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoSingleton<CameraManager>
{
    [SerializeField]
    Camera m_camera;

    List<Transform> m_pointer = new();

    public Camera main => m_camera;
    const float c_smoothFactor = 5f;

    [SerializeField] Transform m_playerCameraPos;

    float m_addPosY = 0;
    float m_addSmoothFactor = 0;

    float m_addPosY_Landscape = 0;

    private void Start()
    {
        m_pointer.Add(transform.Find("Pointer"));

        DontDestroyOnLoad(this);

        Signal.instance.ConnectMainHero.connectLambda =
            new(this, _ =>
            {
                m_playerCameraPos = _.cameraPos;
            });

        Signal.instance.ChangeDisplayMode.connectLambda =
            new(this, _isLandscape =>
            {
                m_camera.fieldOfView = _isLandscape ? 100 : 110;
                m_addPosY_Landscape = _isLandscape ? 1 : 0;
            });
    }

    private void Update()
    {
        if (m_camera == null)
            return;

        if (Input.touchCount == 0)
        {
            //var mousePos = Input.touchCount > 1 ? (Vector3)Input.GetTouch(Input.touchCount - 1).position : Input.mousePosition;
            var mousePos = Input.mousePosition;
            mousePos.z = -m_camera.transform.position.z;
            var pos = m_camera.ScreenToWorldPoint(mousePos);
            pos.z = 0;

            m_pointer[0].position = pos;
        }
        else
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Vector3 touchPos = Input.GetTouch(i).position;
                touchPos.z = -m_camera.transform.position.z;
                var pos = m_camera.ScreenToWorldPoint(touchPos);
                pos.z = 0;

                if (i == m_pointer.Count)
                {
                    var pointer = Instantiate(m_pointer[0], transform);
                    m_pointer.Add(pointer);
                }
                m_pointer[i].position = pos;
            }
        }
    }

    private void LateUpdate()
    {
        if (m_camera == null || MapManager.instance == null || m_playerCameraPos == null)
            return;

        if (m_isShake == false)
            CameraMove();
    }

    public void SetCameraPosTarget(Transform _target, bool _isForce = true)
    {
        m_playerCameraPos = _target;

        if (_target != null && _isForce == true)
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

    public static Vector3 posPointer => m_instance.m_pointer[0].position;
    public static Transform pointer => m_instance.m_pointer[0];

    public static Vector3 GetPosPointer(int _fingerId)
        => m_instance.GetPosPointerManager(_fingerId);

    Vector3 GetPosPointerManager(int _fingerId)
    {
        if (_fingerId == -1)
            return m_pointer[0].position;

        for (int i = 0; i < Input.touchCount; i++)
        {
            if (Input.GetTouch(i).fingerId == _fingerId)
            {
                Vector3 touchPos = Input.GetTouch(i).position;
                touchPos.z = -m_camera.transform.position.z;
                var pos = m_camera.ScreenToWorldPoint(touchPos);
                pos.z = 0;

                if (i == m_pointer.Count)
                {
                    var pointer = Instantiate(m_pointer[0], transform);
                    pointer.position = pos;

                    m_pointer.Add(pointer);
                }

                return pos;
            }
        }

        return Vector3.zero;
    }

    public void ScreenLogTouchPosition()
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);

            Vector3 touchPos = touch.position;
            touchPos.z = -m_camera.transform.position.z;
            var pos = m_instance.m_camera.ScreenToWorldPoint(touchPos);
            pos.z = 0;
        }
    }

    //public Vector3 GetMousePosition()
    //{
    //    if (m_camera == null)
    //        return Vector3.zero;

    //    var mousePos = Input.touchCount > 1 ? (Vector3)Input.GetTouch(Input.touchCount - 1).position : Input.mousePosition;
    //    mousePos.z = -m_camera.transform.position.z;
    //    var pos = m_camera.ScreenToWorldPoint(mousePos);
    //    pos.z = 0;

    //    return pos;
    //}

    public void SetAddPosY(float? _addPosY = null, float? _addSmoothFactor = null)
    {
        if (_addPosY != null)
            m_addPosY = _addPosY.Value;
        if (_addSmoothFactor != null)
            m_addSmoothFactor = _addSmoothFactor.Value;
    }
}
