using UnityEngine;

namespace BusJam
{
    public class GameAudio : MonoBehaviour
    {
        [SerializeField] private AudioConfig config;
        private AudioSource _sfxSource, _bgmSource;
        private AudioClip[] _clips;
        private bool[] _isProceduralClip;
        public bool Muted => !SettingsManager.IsSoundOn;

        public void Initialize(AudioConfig config = null)
        {
            if (config != null) this.config = config;
            if (this.config == null) this.config = Resources.Load<AudioConfig>("AudioConfig");

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = this.config != null ? this.config.BgmVolume : .09f;

            _clips = new AudioClip[8];
            _isProceduralClip = new bool[8];
            for (int i = 0; i < _clips.Length; i++)
            {
                var cue = (Cue)i;
                var clip = this.config != null ? this.config.GetClip(cue) : null;
                if (clip != null)
                {
                    _clips[i] = clip;
                    _isProceduralClip[i] = false;
                }
                else
                {
                    _clips[i] = Synthesize(cue);
                    _isProceduralClip[i] = true;
                }
            }

            SettingsManager.Changed += ApplySettings;
            ApplySettings();
        }

        public void Toggle()
        {
            SettingsManager.SetSound(!SettingsManager.IsSoundOn);
        }

        public void Play(Cue cue)
        {
            if (config != null && _sfxSource != null)
            {
                config.Play(cue, _sfxSource);
                if (SettingsManager.IsMusicOn && !_bgmSource.isPlaying) _bgmSource.Play();
            }
            else if (_sfxSource != null && SettingsManager.IsSoundOn)
            {
                _sfxSource.PlayOneShot(_clips[(int)cue], .36f);
                if (SettingsManager.IsMusicOn && !_bgmSource.isPlaying) _bgmSource.Play();
            }
        }

        private void ApplySettings()
        {
            _sfxSource.mute = !SettingsManager.IsSoundOn;
            _bgmSource.mute = !SettingsManager.IsMusicOn;
            if (SettingsManager.IsMusicOn && !_bgmSource.isPlaying) _bgmSource.Play();
            else if (!SettingsManager.IsMusicOn) _bgmSource.Pause();
        }

        private void OnDestroy()
        {
            SettingsManager.Changed -= ApplySettings;
            if (_clips != null)
            {
                for (int i = 0; i < _clips.Length; i++)
                {
                    if (_isProceduralClip != null && _isProceduralClip[i] && _clips[i] != null)
                    {
                        Destroy(_clips[i]);
                    }
                }
            }
            if (_bgmSource != null && _bgmSource.clip != null && (config == null || _bgmSource.clip != config.Bgm))
            {
                Destroy(_bgmSource.clip);
            }
        }

        // Original lightweight synthesized cues, allocated once during startup.
        private static AudioClip Synthesize(Cue cue)
        {
            int rate = 22050;
            float duration = cue == Cue.Win ? 1.1f : cue == Cue.Lose ? .65f : cue == Cue.Engine ? .55f : .15f;
            var data = new float[Mathf.CeilToInt(rate * duration)];
            float phase = 0;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate, p = t / duration;
                float hz = cue == Cue.Coin ? 1320 : cue == Cue.Tap ? 650 :
                    cue == Cue.Honk ? 330 :
                    cue == Cue.Step ? 950 :
                    cue == Cue.Bump ? 110 : 220;
                if (cue == Cue.Engine) hz = Mathf.Lerp(70, 220, p);
                if (cue == Cue.Win) hz = new[] { 523f, 659f, 784f, 1046f }[Mathf.Min(3, (int)(p * 4))];
                if (cue == Cue.Lose) hz = Mathf.Lerp(310, 130, p);
                phase += hz * 2 * Mathf.PI / rate;
                float envelope = Mathf.Min(t * 70, 1) * Mathf.Pow(1 - p, 1.5f);
                data[i] = (Mathf.Sin(phase) + .23f * Mathf.Sin(phase * 2)) * envelope * .6f;
            }

            var clip = AudioClip.Create(cue.ToString(), data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
