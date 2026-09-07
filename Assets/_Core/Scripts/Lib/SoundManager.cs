using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SoundManager : MonoSingleton<SoundManager>
{
    protected List<AudioData> m_audioData = new();

    private Dictionary<SoundType, AsyncOperationHandle<AudioClip>> m_dicSound = new();

    private void Start()
        => StartAsync().Forget();

    protected override void OnDestroy()
    {
        foreach (var h in m_dicSound)
            h.Value.Release();

        base.OnDestroy();
    }

    async UniTask StartAsync()
    {
        Signal.instance.OptionUpdate.connect = SlotOptionUpdate;

        // 5개만 미리 만들어 놓을까나?
        for (int i = 0; i < 5; i++)
        {
            AudioData audioData = new();
            audioData.source = transform.AddComponent<AudioSource>();
            m_audioData.Add(audioData);
        }

        List<UniTask> tasks = new();
        for (var i = SoundType.BGM_START + 1; i < SoundType.BGM_FINISHED; i++)
            tasks.Add(LoadAssetAsync(i));
        for (var i = SoundType.SFX_START + 1; i < SoundType.SFX_FINISHED; i++)
            tasks.Add(LoadAssetAsync(i));

        await UniTask.WhenAll(tasks);
    }

    public bool IsPlay(SoundType _type)
        => m_audioData.FindIndex(x => x.source.isPlaying == true && x.soundType.Equals(_type)) > -1;

    public static void StopAll()
    {
        var playingAudio = instance.m_audioData.FindAll(x => x.source.isPlaying == true);

        foreach (var audio in playingAudio)
            audio.source.Stop();
    }

    public static void Play(SoundType _type, float _volume = 1f, bool _isLoop = false)
    {
        instance.SetPlayAsync(_type, _volume, _isLoop).Forget();
    }
    async UniTask SetPlayAsync(SoundType _type, float _volume = 1f, bool _isLoop = false)
    {
        var audioClip = await LoadAssetAsync(_type);

        if (audioClip == null)
        {
            IngameLog.AddError("SOUND: PLAY FAILED: Cant find SoundType: " + _type);
            return;
        }

        var audioData = m_audioData.Find(x => x.source.isPlaying == false);

        if (audioData == null)
        {
            audioData = new();
            audioData.source = transform.AddComponent<AudioSource>();
            m_audioData.Add(audioData);

            IngameLog.Add("ADD AUDIOSOURCE!! " + $"({m_audioData.Count})");
        }

        audioData.soundType = _type;
        audioData.isSFX = _type > SoundType.SFX_START && _type < SoundType.SFX_FINISHED;
        audioData.volume = _volume;

        audioData.source.loop = _isLoop;
        audioData.source.volume = _volume;
        audioData.source.mute = audioData.isSFX ? DataManager.option.isMute_SFX : DataManager.option.isMute_BGM;

        audioData.source.clip = audioClip;
        audioData.source.Play();
    }

    public async UniTask<AudioClip> LoadAssetAsync(SoundType _soundType)
    {
        if (m_dicSound.ContainsKey(_soundType))
            return m_dicSound[_soundType].Result;

        await AddressableManager.instance.LoadAssetAsync<AudioClip>(_result =>
        {
            foreach (var data in _result)
            {
                if (m_dicSound.ContainsKey(_soundType) == false)
                    m_dicSound.Add(_soundType, data.Value);
            }

            if (m_dicSound.ContainsKey(_soundType) == false)
                m_dicSound.Add(_soundType, default);
        }, null, $"Sound/{_soundType}.prefab");

        return m_dicSound.ContainsKey(_soundType) ? m_dicSound[_soundType].Result : null;
    }

    public void DeleteAsset(SoundType _soundType)
    {
        if (m_dicSound.ContainsKey(_soundType))
        {
            m_audioData.Find(x => x.soundType == _soundType)?.source.Stop();

            m_dicSound[_soundType].Release();
            m_dicSound.Remove(_soundType);
        }
    }

    void SlotOptionUpdate(OptionType _optionType)
    {
        List<AudioData> audio = null;
        bool isMute = false;

        switch (_optionType)
        {
            case OptionType.MUTE_SOUND_BGM:
                audio = m_audioData.FindAll(x => x.source.isPlaying == true && x.isSFX == false);
                break;
            case OptionType.MUTE_SOUND_SFX:
                audio = m_audioData.FindAll(x => x.source.isPlaying == true && x.isSFX == true);
                break;
            default: return;
        }

        if (audio != null)
        {
            isMute = DataManager.option.IsOn(_optionType);
            foreach (var a in audio)
                a.source.mute = isMute;
        }
    }

    protected class AudioData
    {
        public SoundType soundType;
        public AudioSource source;
        public float volume;

        public bool isSFX;
        public bool isLoop;
    }
}


public enum SoundType
{
    BGM_START,
    BGM_FINISHED,

    SFX_START,
    SFX_FINISHED,

    MAX,
}