using UnityEngine;

namespace BusJam
{
    public class GameAudio : MonoBehaviour
    {
        private AudioSource _sfxSource, _bgmSource;
        private AudioClip[] _clips;
        public bool Muted => !SettingsManager.IsSoundOn;

        public void Initialize()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.volume = .09f;
            _bgmSource.clip = MakeMusic();
            _clips = new AudioClip[8];
            for (int i = 0; i < _clips.Length; i++) _clips[i] = Synthesize((Cue)i);
            SettingsManager.Changed += ApplySettings;
            ApplySettings();
        }

        public void Toggle()
        {
            SettingsManager.SetSound(!SettingsManager.IsSoundOn);
        }

        public void Play(Cue cue)
        {
            if (_sfxSource != null && SettingsManager.IsSoundOn)
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

        private static AudioClip MakeMusic()
        {
            const int rate = 22050;
            var data = new float[rate * 8];
            float[] notes = { 261.63f, 329.63f, 392f, 329.63f, 293.66f, 349.23f, 440f, 392f };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)rate;
                float beat = t % 1;
                data[i] = Mathf.Sin(t * notes[(int)t] * Mathf.PI * 2) * Mathf.Sin(beat * Mathf.PI) * .4f;
            }
            var clip = AudioClip.Create("Bus Jam - calm loop", data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private void OnDestroy()
        {
            SettingsManager.Changed -= ApplySettings;
            if (_clips != null) foreach (var clip in _clips) if (clip != null) Destroy(clip);
            if (_bgmSource != null && _bgmSource.clip != null) Destroy(_bgmSource.clip);
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
