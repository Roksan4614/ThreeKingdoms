using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.U2D;
using UnityEngine.UI;

public class SpriteAnimaion : MonoBehaviour, IValidatable
{
    [Serializable]
    public enum LoopType
    {
        none,
        loop,
        pingpong,
        pingpong_loop,
    }


    Action m_onCompleted;

#if UNITY_EDITOR
    public void OnManualValidate()
    {
        m_element.Initialize(transform);

        //if (m_effectData.duration == 0)
            m_effectData.duration = 0.03f;
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
        if (m_element.sprite.Length > 0)
            m_coPlay = StartCoroutine(DoPlayAnimation());
        else
            gameObject.SetActive(false);
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

        DateTime dtTimer = DateTime.Now;

        var effectData = m_effectData;

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
        if (m_effectData.isFlipLoop)
        {
            var scale = _trns.localScale;
            scale.x *= -1;
            _trns.localScale = scale;
        }

        if (m_effectData.isRotateLoop)
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

    public void Stop()
    {
        if (m_coPlay != null)
        {
            StopCoroutine(m_coPlay);
            m_coPlay = null;
        }
        gameObject.SetActive(false);

        ResetScaleRot(m_element.image?.transform ?? m_element.renderer.transform);
    }

    [Serializable]
    struct EffectData
    {
        public float duration;
        public float delay;
        public LoopType loopType;
        public bool isFlipLoop;
        public bool isRotateLoop;
    }

    [SerializeField]
    EffectData m_effectData;

    [SerializeField, HideInInspector]
    ElementData m_element;

    [Serializable]
    struct ElementData
    {
        public Image image;
        public SpriteRenderer renderer;
        public bool isAddEmptySprite;
        public Sprite[] sprite;

        public void Initialize(Transform _transform)
        {
            image = _transform.GetComponent<Image>("Panel");
            renderer = _transform.GetComponent<SpriteRenderer>("Panel");

            Sprite baseSprite =
                image ? image.sprite :
                renderer ? renderer.sprite : null;

            if (baseSprite != null)
            {
                string spriteSheetPath = UnityEditor.AssetDatabase.GetAssetPath(baseSprite);

                var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
                    .OfType<Sprite>()
                    .OrderBy(_x => int.Parse(_x.name.Split("_").Last())).ToList();

                if (isAddEmptySprite)
                    sprites.Add(AssetLoader.Load<Sprite>("Icon/empty"));

                sprite = sprites.ToArray();
            }
            else
                sprite = null;
        }
    }
}
