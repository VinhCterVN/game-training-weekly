using UnityEngine;

namespace BusJam
{
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "Bus Jam/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("SFX Cues")]
        [SerializeField] private AudioClip _tap;
        [SerializeField] private AudioClip _honk;
        [SerializeField] private AudioClip _engine;
        [SerializeField] private AudioClip _step;
        [SerializeField] private AudioClip _bump;
        [SerializeField] private AudioClip _win;
        [SerializeField] private AudioClip _lose;
        [SerializeField] private AudioClip _coin;

        [Header("Music")]
        [SerializeField] private AudioClip _bgm;

        [Header("Volume Settings")]
        [Range(0f, 1f)] [SerializeField] private float _sfxVolume = 0.36f;
        [Range(0f, 1f)] [SerializeField] private float _bgmVolume = 0.09f;

        public float SfxVolume => _sfxVolume;
        public float BgmVolume => _bgmVolume;
        public AudioClip Bgm => _bgm;

        public AudioClip GetClip(Cue cue) => cue switch
        {
            Cue.Tap => _tap,
            Cue.Honk => _honk,
            Cue.Engine => _engine,
            Cue.Step => _step,
            Cue.Bump => _bump,
            Cue.Win => _win,
            Cue.Lose => _lose,
            Cue.Coin => _coin,
            _ => null
        };

        public void Play(Cue cue, AudioSource source = null, float volumeScale = 1f)
        {
            if (!SettingsManager.IsSoundOn) return;
            var clip = GetClip(cue);
            if (clip == null) return;

            float volume = _sfxVolume * volumeScale;
            if (source != null)
            {
                source.PlayOneShot(clip, volume);
            }
            else
            {
                AudioSource.PlayClipAtPoint(clip, Vector3.zero, volume);
            }
        }

        public void Play(AudioSource source, Cue cue, float volumeScale = 1f)
        {
            Play(cue, source, volumeScale);
        }

        public void PlayMusic(AudioSource source)
        {
            if (source == null || !SettingsManager.IsMusicOn || _bgm == null) return;
            source.clip = _bgm;
            source.loop = true;
            source.volume = _bgmVolume;
            source.Play();
        }
    }
}
