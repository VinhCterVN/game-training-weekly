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
            Debug.Log("[Settings] OnEnable called");

            Debug.Log($"Sound reference: {sound}");
            Debug.Log($"Music reference: {music}");
            Debug.Log($"Haptic reference: {haptic}");

            sound.onValueChanged.AddListener(Sound);
            music.onValueChanged.AddListener(Music);
            haptic.onValueChanged.AddListener(Haptic);

            sound.SetIsOnWithoutNotify(SettingsManager.IsSoundOn);
            music.SetIsOnWithoutNotify(SettingsManager.IsMusicOn);
            haptic.SetIsOnWithoutNotify(SettingsManager.IsHapticOn);
        }

        private void Sound(bool value)
        {
            Debug.Log($"[Settings] Sound toggled: {value}");
            SettingsManager.SetSound(value);
            Feedback();
        }

        private void Music(bool value)
        {
            Debug.Log($"[Settings] Music toggled: {value}");
            SettingsManager.SetMusic(value);
            Feedback();
        }

        private void Haptic(bool value)
        {
            Debug.Log($"[Settings] Haptic toggled: {value}");
            SettingsManager.SetHaptic(value);
            Feedback();
        }
        private void Feedback() => GetComponentInParent<GameUI>().ClickFeedback();
    }
}
