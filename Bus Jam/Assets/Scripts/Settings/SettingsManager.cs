using System;
using UnityEngine;

namespace BusJam
{
    public static class SettingsManager
    {
        public static event Action Changed;
        public static bool IsSoundOn => PlayerPrefs.GetInt("BusJam.Sound", PlayerPrefs.GetInt("BusJam.Muted", 0) == 0 ? 1 : 0) == 1;
        public static bool IsMusicOn => PlayerPrefs.GetInt("BusJam.Music", 1) == 1;
        public static bool IsHapticOn => PlayerPrefs.GetInt("BusJam.Haptic", 1) == 1;
        public static void SetSound(bool value) => Save("BusJam.Sound", value);
        public static void SetMusic(bool value) => Save("BusJam.Music", value);
        public static void SetHaptic(bool value) => Save("BusJam.Haptic", value);

        private static void Save(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }

    public enum HapticCue { Light, Success, Warning }

    public static class HapticFeedback
    {
        private static float _nextTime;
        public static void Play(HapticCue cue)
        {
            if (!SettingsManager.IsHapticOn || Time.realtimeSinceStartup < _nextTime) return;
            _nextTime = Time.realtimeSinceStartup + .08f;
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
            {
                if (vibrator == null || !vibrator.Call<bool>("hasVibrator")) return;
                long duration = cue == HapticCue.Light ? 12 : cue == HapticCue.Success ? 35 : 65;
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                    {
                        using (var effect = new AndroidJavaClass("android.os.VibrationEffect"))
                        using (var pulse = effect.CallStatic<AndroidJavaObject>("createOneShot", duration, cue == HapticCue.Light ? 45 : 100))
                            vibrator.Call("vibrate", pulse);
                    }
                    else vibrator.Call("vibrate", duration);
                }
            }
#elif UNITY_IOS && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
