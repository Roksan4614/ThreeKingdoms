using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapManager : MonoSingleton<MapManager>
{
    Camera m_camera;

    [SerializeField]
    Transform m_left;
    [SerializeField]
    Transform m_right;

    [SerializeField]
    Transform m_top;
    [SerializeField]
    Transform m_bottom;

    public LayerMask m_bounceLayer;

    private void Start()
    {
        m_bounceLayer = LayerMask.GetMask("BounceCamera");
        m_camera = Camera.main;
    }

    public Transform parentHero => transform.Find("Heros/Team");
    public Transform parentEnemy => transform.Find("Heros/Enemy");

    public Vector3 GetBounceHorizontalPos(Vector3 _targetPos)
    {
        var edgeLB = m_camera.ViewportToWorldPoint(new Vector3(0, 0, -m_camera.transform.position.z));
        var edgeRT = m_camera.ViewportToWorldPoint(new Vector3(1, 1, -m_camera.transform.position.z));

        var distCameraHori = m_camera.transform.position.x - edgeLB.x;
        var distCameraVert = m_camera.transform.position.y - edgeLB.y;

        m_left.transform.position = new Vector3(edgeLB.x, _targetPos.y, _targetPos.z);
        m_right.transform.position = new Vector3(edgeRT.x, _targetPos.y, _targetPos.z);

        m_bottom.transform.position = new Vector3(_targetPos.x, edgeLB.y, _targetPos.z);
        m_top.transform.position = new Vector3(_targetPos.x, edgeRT.y, _targetPos.z);

        // 좌
        Collider2D collider = null;

        // 중심에서 왼쪽있으 때만..
        if (_targetPos.x < 0)
        {
            collider = Physics2D.Raycast(_targetPos, Vector2.left, distCameraHori, m_bounceLayer).collider;
            if (collider != null)
                _targetPos.x = collider.transform.position.x + distCameraHori;
        }
        else
        {
            collider = Physics2D.Raycast(_targetPos, Vector2.right, distCameraHori, m_bounceLayer).collider;

            if (collider != null)
                _targetPos.x = collider.transform.position.x - distCameraHori;
        }

        // 하
        collider = Physics2D.Raycast(_targetPos, Vector2.down, distCameraVert, m_bounceLayer).collider;

        if (collider != null)
            _targetPos.y = collider.transform.position.y + distCameraVert;
        else
        {
            // 상
            collider = Physics2D.Raycast(_targetPos, Vector2.up, distCameraVert, m_bounceLayer).collider;
            if (collider != null)
                _targetPos.y = collider.transform.position.y - distCameraVert;
        }

        return _targetPos;
    }
}
