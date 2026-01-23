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

    public LayerMask m_bounceLayer;

    private void Start()
    {
        m_bounceLayer = LayerMask.GetMask("Camera_Bounce");
        m_camera = Camera.main;
    }

    public Transform parentHero => transform.Find("Heros/Team");
    public Transform parentEnemy => transform.Find("Heros/Enemy");

    public List<CharacterComponent> GetEnemyList()
    {
        List<CharacterComponent> result = new();
        var pEnemy = parentEnemy;
        for ( int i = 0; i < pEnemy.childCount; i++)
        {
            var character = pEnemy.GetChild(i).GetComponent<CharacterComponent>();
            if (character != null)
                result.Add(character);
        }

        return result;
    }

    public Vector3 GetBounceHorizontalPos(Vector3 _targetPos)
    {
        var edgeLB = m_camera.ViewportToWorldPoint(new Vector3(0, 0, -m_camera.transform.position.z));
        var edgeRT = m_camera.ViewportToWorldPoint(new Vector3(1, 1, -m_camera.transform.position.z));

        var distCameraHori = m_camera.transform.position.x - edgeLB.x;
        var distCameraVert = m_camera.transform.position.y - edgeLB.y;

        var pos = _targetPos;
        pos.x = edgeLB.x;
        m_left.transform.position = pos;

        pos.x = edgeRT.x;
        m_right.transform.position = pos;

        Collider2D collider;

        // аб
        collider = Physics2D.Raycast(_targetPos, Vector2.left, distCameraHori, m_bounceLayer).collider;

        if (collider != null)
            _targetPos.x = collider.transform.position.x + distCameraHori;
        else
        {
            collider = Physics2D.Raycast(_targetPos, Vector2.right, distCameraHori, m_bounceLayer).collider;

            if (collider != null)
                _targetPos.x = collider.transform.position.x - distCameraHori;
        }

        //// го
        collider = Physics2D.Raycast(_targetPos, Vector2.down, distCameraVert, m_bounceLayer).collider;

        if (collider != null)
            _targetPos.y = collider.transform.position.y + distCameraVert;
        else
        {
            // ╩С
            collider = Physics2D.Raycast(_targetPos, Vector2.up, distCameraVert, m_bounceLayer).collider;
            if (collider != null)
                _targetPos.y = collider.transform.position.y - distCameraVert;
        }

        return _targetPos;
    }
}
