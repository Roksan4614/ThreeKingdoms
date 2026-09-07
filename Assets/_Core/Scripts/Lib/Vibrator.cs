using UnityEngine;
using System.Runtime.InteropServices;

public static class Vibrator
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] static extern void IOS_InitHaptics();
    [DllImport("__Internal")] static extern void IOS_PlayVibration(float intensity, float duration);
    [DllImport("__Internal")] static extern void IOS_PlayHaptic(float intensity, float sharpness);
    [DllImport("__Internal")] static extern void IOS_PlayHapticPattern(float intensity, float sharpness, int count, float interval);
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
    public static AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
    public static AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    public static AndroidJavaObject vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
#endif

    private static bool _iosInitialized = false;

    public enum E_VibrateType
    {
        LIGHT,
        MEDIUM,
        HEAVY,
    }

    static void EnsureIOSInit()
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (!_iosInitialized)
        {
            IOS_InitHaptics();
            _iosInitialized = true;
        }
#endif
    }

    static bool IsIOS12()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return UnityEngine.iOS.Device.systemVersion.StartsWith("12");
#else
        return false;
#endif
    }

    public static void Play(E_VibrateType _vibrateType, int durationMiliseconds = 0)
    {
        Debug.Log("Vibrator Play " + _vibrateType + ", " + durationMiliseconds);
        int _ampliude = 0;
        int _milisec = 0;

        switch (_vibrateType)
        {
            case E_VibrateType.LIGHT:
                _ampliude = Application.platform == RuntimePlatform.IPhonePlayer ? 30 : 80;
                _milisec = 200;
                break;
            case E_VibrateType.MEDIUM:
                _ampliude = Application.platform == RuntimePlatform.IPhonePlayer ? 70 : 170;
                _milisec = 400;
                break;
            case E_VibrateType.HEAVY:
                _ampliude = Application.platform == RuntimePlatform.IPhonePlayer ? 100 : 255;
                _milisec = 1000;
                break;
            default:
                _ampliude = 30;
                _milisec = 100;
                break;
        }

        if (durationMiliseconds != 0) _milisec = durationMiliseconds;
        Play(_milisec, _ampliude);
    }

    public static void Play(long _milliseconds = 100, int _ampliude = 30)
    {
        Debug.Log("Vibrator Play " + _milliseconds + ", " + _ampliude);
#if UNITY_EDITOR
#elif UNITY_ANDROID
        if (!isInteractable) return;
        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", _milliseconds, _ampliude);
        vibrator.Call("vibrate", vibrationEffect);
#elif UNITY_IOS
        if (!isInteractable) return;
        if (IsIOS12()) { Handheld.Vibrate(); return; }
        EnsureIOSInit();
        float intensity = Mathf.Clamp01((float)_ampliude / 100f);
        float duration = Mathf.Max(0.02f, (float)_milliseconds / 1000f);
        IOS_PlayVibration(intensity, duration);
#endif
    }

    // ≈π! «— πÊ
    public static void PlayHaptic(float intensity = 1f, float sharpness = 1f)
    {
        if (DataManager.option.isHaptic == false) return;
        Debug.Log($"Vibrator PlayHaptic intensity={intensity}, sharpness={sharpness}");
#if UNITY_EDITOR
#elif UNITY_ANDROID
        int amplitude = Mathf.Clamp((int)(intensity * sharpness * 255), 1, 255);
        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createOneShot", (long)30, amplitude);
        vibrator.Call("vibrate", vibrationEffect);
#elif UNITY_IOS
        if (IsIOS12()) { Handheld.Vibrate(); return; }
        EnsureIOSInit();
        IOS_PlayHaptic(intensity, sharpness);
#endif
    }

    // µÊµÊµÊµÊ ∆–≈œ
    public static void PlayHapticPattern(float intensity = 1f, float sharpness = 1f, int count = 4, float interval = 0.1f)
    {
        if (DataManager.option.isHaptic == false) return;
        Debug.Log($"Vibrator PlayHapticPattern count={count}, interval={interval}");
#if UNITY_EDITOR
#elif UNITY_ANDROID
        // æ»µÂ∑Œ¿ÃµÂ °Ê waveform¿∏∑Œ ∆–≈œ »‰≥ª≥ø
        int amplitude = Mathf.Clamp((int)(intensity * sharpness * 255), 1, 255);
        long onTime = 30;
        long offTime = (long)(interval * 1000) - onTime;
        offTime = Mathf.Max(10, (int)offTime);

        long[] timings = new long[count * 2];
        int[] amplitudes = new int[count * 2];
        for (int i = 0; i < count; i++)
        {
            timings[i * 2]     = onTime;
            timings[i * 2 + 1] = offTime;
            amplitudes[i * 2]     = amplitude;
            amplitudes[i * 2 + 1] = 0;
        }

        AndroidJavaClass vibrationEffectClass = new AndroidJavaClass("android.os.VibrationEffect");
        AndroidJavaObject vibrationEffect = vibrationEffectClass.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1);
        vibrator.Call("vibrate", vibrationEffect);
#elif UNITY_IOS
        if (IsIOS12()) { Handheld.Vibrate(); return; }
        EnsureIOSInit();
        IOS_PlayHapticPattern(intensity, sharpness, count, interval);
#endif
    }

    public static void Cancel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        vibrator.Call("cancel");
#endif
    }
}