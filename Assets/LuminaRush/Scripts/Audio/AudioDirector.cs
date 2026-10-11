using UnityEngine;

namespace LuminaRush
{
    public sealed class AudioDirector : MonoBehaviour
    {
        AudioSource music;
        AudioSource sfx;
        AudioClip coin;
        AudioClip jump;
        AudioClip slide;
        AudioClip hit;
        AudioClip ui;
        AudioClip unlock;
        AudioClip start;
        float musicVolume = 0.55f;
        float sfxVolume = 0.85f;
        bool ducked;

        public void Build()
        {
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            sfx.spatialBlend = 0f;
            coin = Tone("coin", 0.12f, 880f, 1320f, 0.35f);
            jump = Noise("jump", 0.16f, 0.4f);
            slide = Noise("slide", 0.18f, 0.22f);
            hit = Thud();
            ui = Tone("ui", 0.06f, 620f, 620f, 0.25f);
            unlock = Arpeggio();
            start = Tone("start", 0.2f, 440f, 880f, 0.3f);
            music.clip = MusicLoop();
            ApplyVolumes();
            music.Play();
        }

        public void SetMusic(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            ApplyVolumes();
        }

        public void SetSfx(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        public void SetDuck(bool duck)
        {
            ducked = duck;
            ApplyVolumes();
        }

        public void PlayCoin() { Play(coin, 0.7f); }
        public void PlayJump() { Play(jump, 0.55f); }
        public void PlaySlide() { Play(slide, 0.45f); }
        public void PlayHit() { Play(hit, 0.85f); }
        public void PlayUi() { Play(ui, 0.45f); }
        public void PlayUnlock() { Play(unlock, 0.7f); }
        public void PlayStart() { Play(start, 0.6f); }

        void Play(AudioClip clip, float scale)
        {
            if (clip == null || sfxVolume <= 0.001f)
                return;
            sfx.PlayOneShot(clip, sfxVolume * scale);
        }

        void ApplyVolumes()
        {
            if (music != null)
                music.volume = ducked ? musicVolume * 0.15f : musicVolume;
        }

        static AudioClip Tone(string name, float duration, float from, float to, float gain)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                float freq = Mathf.Lerp(from, to, t);
                float env = Mathf.Sin(t * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * (i / (float)rate)) * env * gain;
            }
            return Clip(name, rate, data);
        }

        static AudioClip Noise(string name, float duration, float gain)
        {
            int rate = 22050;
            int samples = Mathf.Max(1, Mathf.RoundToInt(rate * duration));
            float[] data = new float[samples];
            float hold = 0f;
            uint state = (uint)(name.GetHashCode() & 0x7fffffff);
            for (int i = 0; i < samples; i++)
            {
                if ((i & 7) == 0)
                {
                    state = state * 1664525u + 1013904223u;
                    hold = (state & 65535) / 65535f * 2f - 1f;
                }
                float t = i / (float)samples;
                data[i] = hold * Mathf.Sin(t * Mathf.PI) * gain;
            }
            return Clip(name, rate, data);
        }

        static AudioClip Thud()
        {
            int rate = 22050;
            int samples = Mathf.RoundToInt(rate * 0.28f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)rate;
                float env = Mathf.Exp(-t * 12f);
                data[i] = Mathf.Sin(2f * Mathf.PI * 90f * t) * env * 0.8f;
            }
            return Clip("hit", rate, data);
        }

        static AudioClip Arpeggio()
        {
            int rate = 22050;
            float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
            int samples = Mathf.RoundToInt(rate * 0.42f);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)samples;
                int note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(t * notes.Length));
                float local = t * notes.Length - note;
                float env = Mathf.Sin(Mathf.Clamp01(local) * Mathf.PI);
                data[i] = Mathf.Sin(2f * Mathf.PI * notes[note] * (i / (float)rate)) * env * 0.28f;
            }
            return Clip("unlock", rate, data);
        }

        static AudioClip MusicLoop()
        {
            int rate = 22050;
            float seconds = 8f;
            int samples = Mathf.RoundToInt(rate * seconds);
            float[] data = new float[samples];
            float[] melody = { 523.25f, 587.33f, 659.25f, 783.99f, 659.25f, 587.33f, 523.25f, 440f };
            float[] bass = { 130.81f, 196f, 174.61f, 146.83f };
            float beat = seconds / 8f;
            for (int i = 0; i < samples; i++)
            {
                float time = i / (float)rate;
                int step = Mathf.FloorToInt(time / beat) % 8;
                float local = (time % beat) / beat;
                float env = 0.35f + 0.65f * Mathf.Sin(Mathf.Clamp01(local) * Mathf.PI);
                float lead = Mathf.Sin(2f * Mathf.PI * melody[step] * time) * env * 0.16f;
                float low = Mathf.Sin(2f * Mathf.PI * bass[step % bass.Length] * time) * 0.12f;
                float hat = ((i / 180) % 2 == 0 ? 0.03f : 0f) * Mathf.Sin(2f * Mathf.PI * 1800f * time) * (local < 0.08f ? 1f : 0f);
                data[i] = Mathf.Clamp(lead + low + hat, -1f, 1f);
            }
            var clip = Clip("music", rate, data);
            return clip;
        }

        static AudioClip Clip(string name, int rate, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
