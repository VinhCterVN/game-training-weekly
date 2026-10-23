using UnityEngine;

namespace BusJam
{
    public class GameAudio : MonoBehaviour
    {
        private AudioSource source;
        private AudioClip[] clips;
        public bool Muted { get; private set; }

        public void Initialize()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            clips = new AudioClip[7];
            for (int i = 0; i < clips.Length; i++) clips[i] = Synthesize((Cue)i);
            Muted = PlayerPrefs.GetInt("BusJam.Muted", 0) == 1;
            source.mute = Muted;
        }

        public void Toggle()
        {
            Muted = !Muted;
            source.mute = Muted;
            PlayerPrefs.SetInt("BusJam.Muted", Muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void Play(Cue cue)
        {
            if (source != null) source.PlayOneShot(clips[(int)cue], .36f);
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
                float hz = cue == Cue.Tap ? 650 :
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