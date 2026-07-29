using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
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
            PlayAnimationAsync().Forget();
        else
            gameObject.SetActive(false);
    }

    public void Play(Action _onCompleted = null)
    {
        m_onCompleted = _onCompleted;

        if (gameObject.activeSelf == true)
        {
            PlayAnimationAsync().Forget();
        }
        else if (m_element.sprite.Length > 0)
            gameObject.SetActive(true);
    }

    CancellationTokenSource m_cts;
    async UniTask PlayAnimationAsync()
    {
        m_cts = m_cts.ReleaseCTS(true);
        var token = m_cts.Token;

        Transform effect = m_element.image?.transform ?? m_element.renderer.transform;

        if (m_element.sprite.Length == 0)
        {
            effect.gameObject.SetActive(false);
            return;
        }

        effect.gameObject.SetActive(true);

        int increaseValue = 1;
        int indexSprite = 0;

        var time = Time.time;

        var effectData = m_effectData;

        while (true)
        {
            if (m_element.image)
                m_element.image.sprite = m_element.sprite[indexSprite];
            else
                m_element.renderer.sprite = m_element.sprite[indexSprite];

            while (Time.time - time < effectData.duration)
                await UniTask.NextFrame(token);

            time = Time.time;
            indexSprite += increaseValue;

            if (m_element.sprite.Length == indexSprite)
            {
                if (effectData.loopType == LoopType.none)
                    break;
                else if (effectData.loopType == LoopType.loop)
                {
                    indexSprite = 0;
                    await UniTask.WaitForSeconds(effectData.delay, cancellationToken: token);
                }
                else
                {
                    increaseValue *= -1;
                    if (effectData.loopType == LoopType.pingpong && increaseValue < 0 && indexSprite == 0)
                        break;

                    if (increaseValue > 0)
                        await UniTask.WaitForSeconds(effectData.delay, cancellationToken: token);
                }

                ResetScaleRot(effect);
            }
        }

        m_onCompleted?.Invoke();
        ResetScaleRot(effect);
        gameObject.SetActive(false);

        m_cts = null;
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
        m_cts = m_cts.ReleaseCTS();
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

    public void OnManualValidate()
    {
        m_element.Initialize(transform);

        if (m_effectData.duration == 0)
            m_effectData.duration = 0.03f;
    }

    [SerializeField, HideInInspector]
    ElementData m_element;
    public ElementData element => m_element;

    [Serializable]
    public struct ElementData
    {
        public Image image;
        public SpriteRenderer renderer;
        //public bool isAddEmptySprite;
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
#if UNITY_EDITOR
                string spriteSheetPath = UnityEditor.AssetDatabase.GetAssetPath(baseSprite);

                var sprites = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(spriteSheetPath)
                    .OfType<Sprite>()
                    .OrderBy(_x => int.Parse(_x.name.Split("_").Last())).ToList();

                //if (isAddEmptySprite)
                sprites.Add(AssetLoader.Load<Sprite>("Icon/empty"));

                sprite = sprites.ToArray();
#endif
            }
            else
                sprite = null;
        }
    }
}
