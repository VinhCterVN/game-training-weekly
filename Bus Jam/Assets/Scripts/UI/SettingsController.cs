using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Toggle _sound;
        [SerializeField] private Toggle _music;
        [SerializeField] private Toggle _haptic;
        private void OnEnable()
        {
            _sound.SetIsOnWithoutNotify(SettingsManager.IsSoundOn);
            _music.SetIsOnWithoutNotify(SettingsManager.IsMusicOn);
            _haptic.SetIsOnWithoutNotify(SettingsManager.IsHapticOn);
            _sound.onValueChanged.AddListener(Sound);
            _music.onValueChanged.AddListener(Music);
            _haptic.onValueChanged.AddListener(Haptic);
        }
        private void OnDisable()
        {
            _sound.onValueChanged.RemoveListener(Sound);
            _music.onValueChanged.RemoveListener(Music);
            _haptic.onValueChanged.RemoveListener(Haptic);
        }
        private void Sound(bool value) { SettingsManager.SetSound(value); Feedback(); }
        private void Music(bool value) { SettingsManager.SetMusic(value); Feedback(); }
        private void Haptic(bool value) { SettingsManager.SetHaptic(value); Feedback(); }
        private void Feedback() => GetComponentInParent<GameUI>().ClickFeedback();
    }
}
