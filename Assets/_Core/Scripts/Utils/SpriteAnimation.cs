using NUnit.Framework.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SpriteAnimaion : MonoBehaviour
{
    public enum LoopType
    {
        none,
        loop,
        pingpong,
        pingpong_loop,
    }

    [SerializeField]
    EffectData m_effectData;

    [SerializeField]
    bool m_isAddEmptySprite = true;

    List<Sprite> m_sprites = new();

    Image m_imgEffect;
    SpriteRenderer m_sprEffect;

    private void Awake()
    {
        Sprite baseSprite = null;
        m_imgEffect = transform.GetComponent<Image>("Panel");
        if (m_imgEffect == null)
        {
            m_sprEffect = transform.GetComponent<SpriteRenderer>("Panel");
            baseSprite = m_sprEffect.sprite;
        }
        else
            baseSprite = m_imgEffect.sprite;

        string spriteSheetPath = AssetDatabase.GetAssetPath(baseSprite);
        m_sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(spriteSheetPath)
            .OfType<Sprite>()
            .ToList();

        if (m_isAddEmptySprite)
            m_sprites.Add(AssetLoader.Load<Sprite>("Icon/empty"));

    }

    private void OnEnable()
    {
        StartCoroutine(DoPlayAnimation());
    }

    public void Play()
    {
        gameObject.SetActive(true);
    }

    IEnumerator DoPlayAnimation()
    {
        Transform effect = m_imgEffect?.transform ?? m_sprEffect.transform;

        if (m_sprites.Count == 0)
        {
            effect.gameObject.SetActive(false);
            yield break;
        }

        effect.gameObject.SetActive(true);

        int increaseValue = 1;
        int indexSprite = 0;

        m_isForceStop = false;

        DateTime dtTimer = DateTime.Now;

        while (true)
        {
            if (m_imgEffect)
                m_imgEffect.sprite = m_sprites[indexSprite];
            else
                m_sprEffect.sprite = m_sprites[indexSprite];

            while ((DateTime.Now - dtTimer).TotalSeconds < m_effectData.duration)
                yield return null;

            dtTimer = DateTime.Now;
            indexSprite += increaseValue;

            if (m_sprites.Count == indexSprite)
            {
                if (m_effectData.loopType == LoopType.none)
                    break;
                else if (m_effectData.loopType == LoopType.loop)
                {
                    indexSprite = 0;
                    yield return new WaitForSeconds(m_effectData.delay);
                }
                else
                {
                    increaseValue *= -1;
                    if (m_effectData.loopType == LoopType.pingpong && increaseValue < 0 && indexSprite == 0)
                        break;

                    if (increaseValue > 0)
                        yield return new WaitForSeconds(m_effectData.delay);
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

        ResetScaleRot(effect);
        gameObject.SetActive(false);
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
