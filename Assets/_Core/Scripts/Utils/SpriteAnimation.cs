using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D;
using UnityEngine.UI;

public class SpriteAnimaion : MonoBehaviour
{
    [Serializable]
    struct ElementData
    {
        public Image image;
        public SpriteRenderer renderer;
        public bool isAddEmptySprite;
        public Sprite[] sprite;

        public EffectData effectData;
    }

    [SerializeField]
    ElementData m_element;

    public enum LoopType
    {
        none,
        loop,
        pingpong,
        pingpong_loop,
    }


    Action m_onCompleted;

#if UNITY_EDITOR
    private void OnValidate()
    {
        m_element.image = transform.GetComponent<Image>("Panel");
        m_element.renderer = transform.GetComponent<SpriteRenderer>("Panel");

        Sprite baseSprite =
            m_element.image ? m_element.image.sprite :
            m_element.renderer ? m_element.renderer.sprite : null;

        if (baseSprite != null)
        {
            string spriteSheetPath = UnityEditor.AssetDatabase.GetAssetPath(baseSprite);

            var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
                .OfType<Sprite>()
                .ToList();


            if (m_element.isAddEmptySprite)
                sprites.Add(AssetLoader.Load<Sprite>("Icon/empty"));

            m_element.sprite = sprites.ToArray();
        }
        else
            m_element.sprite = null;

            UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private void Awake()
    {
        if (m_element.sprite == null)
        {
            gameObject.SetActive(false);
            return;
        }
    }

    private void OnEnable()
    {
        m_coPlay = StartCoroutine(DoPlayAnimation());
    }

    public void Play(Action _onCompleted = null)
    {
        m_onCompleted = _onCompleted;

        if (gameObject.activeSelf == true)
        {
            if (m_coPlay != null)
                StopCoroutine(m_coPlay);

            m_coPlay = StartCoroutine(DoPlayAnimation());
        }
        else if (m_element.sprite.Length > 0)
            gameObject.SetActive(true);
    }

    Coroutine m_coPlay;
    IEnumerator DoPlayAnimation()
    {
        Transform effect = m_element.image?.transform ?? m_element.renderer.transform;

        if (m_element.sprite.Length == 0)
        {
            effect.gameObject.SetActive(false);
            yield break;
        }

        effect.gameObject.SetActive(true);

        int increaseValue = 1;
        int indexSprite = 0;

        m_isForceStop = false;

        DateTime dtTimer = DateTime.Now;

        var effectData = m_element.effectData;

        while (true)
        {
            if (m_element.image)
                m_element.image.sprite = m_element.sprite[indexSprite];
            else
                m_element.renderer.sprite = m_element.sprite[indexSprite];

            while ((DateTime.Now - dtTimer).TotalSeconds < effectData.duration)
                yield return null;

            dtTimer = DateTime.Now;
            indexSprite += increaseValue;

            if (m_element.sprite.Length == indexSprite)
            {
                if (effectData.loopType == LoopType.none)
                    break;
                else if (effectData.loopType == LoopType.loop)
                {
                    indexSprite = 0;
                    yield return new WaitForSeconds(effectData.delay);
                }
                else
                {
                    increaseValue *= -1;
                    if (effectData.loopType == LoopType.pingpong && increaseValue < 0 && indexSprite == 0)
                        break;

                    if (increaseValue > 0)
                        yield return new WaitForSeconds(effectData.delay);
                }

                if (m_isForceStop == true)
                {
                    m_callbackForceStop?.Invoke();
                    m_callbackForceStop = null;
                    break;
                }

                ResetScaleRot(effect);
            }
        }

        m_onCompleted?.Invoke();
        ResetScaleRot(effect);
        gameObject.SetActive(false);

        m_coPlay = null;
    }

    void ResetScaleRot(Transform _trns)
    {
        if (m_element.effectData.isFlipLoop)
        {
            var scale = _trns.localScale;
            scale.x *= -1;
            _trns.localScale = scale;
        }

        if (m_element.effectData.isRotateLoop)
            _trns.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

    }

    public void SetColor(Color _color)
    {
        if (m_element.image)
            m_element.image.color = _color;
        else if (m_element.renderer)
            m_element.renderer.color = _color;
    }

    public Color GetColor()
    {
        return m_element.image?.color ?? m_element.renderer?.color ?? Color.white;
    }


    UnityAction m_callbackForceStop;
    bool m_isForceStop = false;
    public void Stop(UnityAction _callback = null)
    {
        m_callbackForceStop = _callback;
        m_isForceStop = true;
    }


    [Serializable]
    public struct EffectData
    {
        public float duration;
        public float delay;
        public LoopType loopType;
        public bool isFlipLoop;
        public bool isRotateLoop;
    }
}
