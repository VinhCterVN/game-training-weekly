using UnityEngine;

namespace TuberLevels
{
    public enum Sound
    {
        Click,
        Pour,
        Complete,
        Win,
        Error
    }

    public sealed class AudioManager : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip[] _clips;
        public bool Muted { get; private set; }

        public void Configure(AudioSource source, AudioClip[] clips)
        {
            _source = source;
            _clips = clips;
        }

        public void Initialize()
        {
            Muted = PlayerPrefs.GetInt("TubeLevels.Muted", 0) == 1;
        }

        public void Play(Sound sound)
        {
            if (!Muted && _clips[(int)sound] != null) _source.PlayOneShot(_clips[(int)sound], .65f);
        }

        public void ToggleMute()
        {
            Muted = !Muted;
            if (Muted) _source.Stop();
            PlayerPrefs.SetInt("TubeLevels.Muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}