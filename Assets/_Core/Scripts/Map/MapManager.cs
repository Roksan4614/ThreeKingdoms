using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : Singleton<MapManager>, IValidatable
{
    Camera m_camera;

    private void Start()
    {
        m_camera = Camera.main;
    }

    public async UniTask FadeDimm(bool _isActive, float _duration = 0.2f)
        => await m_element.dimm.DOFade(_isActive ? 1 : 0, _duration).AsyncWaitForCompletion();

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);
    }
#endif

    public Vector3 GetBounceHorizontalPos(Vector3 _targetPos)
    {
        var edgeLB = m_camera.ViewportToWorldPoint(new Vector3(0, 0, -m_camera.transform.position.z));
        var edgeRT = m_camera.ViewportToWorldPoint(new Vector3(1, 1, -m_camera.transform.position.z));

        var distCameraHori = m_camera.transform.position.x - edgeLB.x;
        var distCameraVert = m_camera.transform.position.y - edgeLB.y;

        Collider2D collider = null;

        if (_targetPos.x < 0)
        {
            // аб
            collider = Physics2D.Raycast(_targetPos, Vector2.left, distCameraHori, m_element.bounceLayer).collider;

            if (collider != null)
                _targetPos.x = collider.transform.position.x + distCameraHori;
        }
        else
        {
            // ©Л
            collider = Physics2D.Raycast(_targetPos, Vector2.right, distCameraHori, m_element.bounceLayer).collider;

            if (collider != null)
                _targetPos.x = collider.transform.position.x - distCameraHori;
        }

        if (_targetPos.y > 0)
        {
            // ╩С
            collider = Physics2D.Raycast(_targetPos, Vector2.up, distCameraVert, m_element.bounceLayer).collider;

            if (collider != null)
                _targetPos.y = collider.transform.position.y - distCameraVert;
        }
        else
        {
            // го
            collider = Physics2D.Raycast(_targetPos, Vector2.down, distCameraVert, m_element.bounceLayer).collider;

            if (collider != null)
                _targetPos.y = collider.transform.position.y + distCameraVert;
        }

        return _targetPos;
    }

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public LayerMask bounceLayer;
        public Transform hero;
        public Transform enemy;

        public Image dimm;

        public void Initialize(Transform _transform)
        {
            bounceLayer = LayerMask.GetMask("BouceCamera");
            hero = _transform.Find("Heros/Team");
            enemy = _transform.Find("Heros/Enemy");

            dimm = GameObject.Find("Canvas/DimmMap").GetComponent<Image>();
        }

        public Transform pEnemy => enemy;
        public Transform pHero => hero;
    }
}
