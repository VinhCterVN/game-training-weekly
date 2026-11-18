using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Toggle sound;
        [SerializeField] private Toggle music;
        [SerializeField] private Toggle haptic;

        [Header("Toggle Visuals")]
        [SerializeField] private Image soundFrame;
        [SerializeField] private Image musicFrame;
        [SerializeField] private Image hapticFrame;
        [SerializeField] private Image soundIcon;
        [SerializeField] private Image musicIcon;
        [SerializeField] private Image hapticIcon;
        [SerializeField] private Color activeFrameColor = Color.white;
        [SerializeField] private Color inactiveFrameColor = new Color(0.55f, 0.58f, 0.65f, 1f);
        [SerializeField] private Color activeIconColor = Color.white;
        [SerializeField] private Color inactiveIconColor = new Color(0.75f, 0.78f, 0.82f, 0.55f);

        private void OnEnable()
        {
            SyncState();
            if (sound != null) sound.onValueChanged.AddListener(OnSoundChanged);
            if (music != null) music.onValueChanged.AddListener(OnMusicChanged);
            if (haptic != null) haptic.onValueChanged.AddListener(OnHapticChanged);
        }

        private void OnDisable()
        {
            if (sound != null) sound.onValueChanged.RemoveListener(OnSoundChanged);
            if (music != null) music.onValueChanged.RemoveListener(OnMusicChanged);
            if (haptic != null) haptic.onValueChanged.RemoveListener(OnHapticChanged);
        }

        private void SyncState()
        {
            bool soundOn = SettingsManager.IsSoundOn;
            bool musicOn = SettingsManager.IsMusicOn;
            bool hapticOn = SettingsManager.IsHapticOn;

            if (sound != null) sound.SetIsOnWithoutNotify(soundOn);
            if (music != null) music.SetIsOnWithoutNotify(musicOn);
            if (haptic != null) haptic.SetIsOnWithoutNotify(hapticOn);

            ApplyVisual(soundFrame, soundIcon, soundOn);
            ApplyVisual(musicFrame, musicIcon, musicOn);
            ApplyVisual(hapticFrame, hapticIcon, hapticOn);
        }

        private void OnSoundChanged(bool value)
        {
            SettingsManager.SetSound(value);
            ApplyVisual(soundFrame, soundIcon, value);
            Feedback();
        }

        private void OnMusicChanged(bool value)
        {
            SettingsManager.SetMusic(value);
            ApplyVisual(musicFrame, musicIcon, value);
            Feedback();
        }

        private void OnHapticChanged(bool value)
        {
            SettingsManager.SetHaptic(value);
            ApplyVisual(hapticFrame, hapticIcon, value);
            Feedback();
        }

        private void ApplyVisual(Image frame, Image icon, bool isOn)
        {
            if (frame != null) frame.color = isOn ? activeFrameColor : inactiveFrameColor;
            if (icon != null) icon.color = isOn ? activeIconColor : inactiveIconColor;
        }

        private void Feedback()
        {
            var ui = GetComponentInParent<GameUI>();
            if (ui != null) ui.ClickFeedback();
        }
    }
}
