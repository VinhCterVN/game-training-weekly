using System;
using UnityEngine;
using UnityEngine.UI;

namespace BusJam
{
    public sealed class SettingsController : MonoBehaviour
    {
        [SerializeField] private Toggle sound;
        [SerializeField] private Toggle music;
        [SerializeField] private Toggle haptic;

        private void Start()
        {
            Debug.Log("Initialized SettingsController");
        }

        private void OnEnable()
        {
            sound.SetIsOnWithoutNotify(SettingsManager.IsSoundOn);
            music.SetIsOnWithoutNotify(SettingsManager.IsMusicOn);
            haptic.SetIsOnWithoutNotify(SettingsManager.IsHapticOn);
            sound.onValueChanged.AddListener(Sound);
            music.onValueChanged.AddListener(Music);
            haptic.onValueChanged.AddListener(Haptic);
        }
        private void OnDisable()
        {
            sound.onValueChanged.RemoveListener(Sound);
            music.onValueChanged.RemoveListener(Music);
            haptic.onValueChanged.RemoveListener(Haptic);
        }
        private void Sound(bool value) { SettingsManager.SetSound(value); Feedback(); }
        private void Music(bool value) { SettingsManager.SetMusic(value); Feedback(); }
        private void Haptic(bool value) { SettingsManager.SetHaptic(value); Feedback(); }
        private void Feedback() => GetComponentInParent<GameUI>().ClickFeedback();
    }
}
