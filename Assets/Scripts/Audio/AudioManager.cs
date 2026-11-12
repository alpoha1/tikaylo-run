using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace TilkayoRun
{
    public enum Sfx { Coin, Jump, Crash, CaseOpen, Click, NewRecord, Tick, Slide, Lane }

    /// <summary>
    /// Звуки и музыка. По умолчанию всё синтезируется кодом.
    /// Чтобы использовать свои файлы, положи AudioClip в Assets/Resources/Audio/ с именами:
    /// sfx_coin, sfx_jump, sfx_crash, sfx_case, sfx_click, sfx_record, sfx_tick, sfx_slide, sfx_lane, music_menu, music_run.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        AudioSource music;
        AudioSource[] pool;
        int poolIdx;
        readonly Dictionary<Sfx, AudioClip> sfx = new Dictionary<Sfx, AudioClip>();
        AudioClip menuClip, runClip, menuOverride, runOverride;
        float[] menuData, runData;
        readonly object gate = new object();
        bool wantRun;
        AudioClip playing;

        public void Init()
        {
            I = this;
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.playOnAwake = false;
            pool = new AudioSource[8];
            for (int i = 0; i < pool.Length; i++)
            {
                pool[i] = gameObject.AddComponent<AudioSource>();
                pool[i].playOnAwake = false;
            }

            sfx[Sfx.Coin] = Pick("sfx_coin", ProceduralAudio.Coin);
            sfx[Sfx.Jump] = Pick("sfx_jump", ProceduralAudio.Jump);
            sfx[Sfx.Crash] = Pick("sfx_crash", ProceduralAudio.Crash);
            sfx[Sfx.CaseOpen] = Pick("sfx_case", ProceduralAudio.CaseOpen);
            sfx[Sfx.Click] = Pick("sfx_click", ProceduralAudio.Click);
            sfx[Sfx.NewRecord] = Pick("sfx_record", ProceduralAudio.NewRecord);
            sfx[Sfx.Tick] = Pick("sfx_tick", ProceduralAudio.Tick);
            sfx[Sfx.Slide] = Pick("sfx_slide", ProceduralAudio.Slide);
            sfx[Sfx.Lane] = Pick("sfx_lane", ProceduralAudio.Lane);

            menuOverride = Resources.Load<AudioClip>("Audio/music_menu");
            runOverride = Resources.Load<AudioClip>("Audio/music_run");

            // музыка синтезируется в фоновом потоке, чтобы не тормозить запуск
            var th = new Thread(GenerateMusic) { IsBackground = true };
            th.Start();
            ApplySettings();
        }

        AudioClip Pick(string resName, Func<float[]> synth)
        {
            var c = Resources.Load<AudioClip>("Audio/" + resName);
            if (c != null) return c;
            return ProceduralAudio.ToClip(resName, synth());
        }

        void GenerateMusic()
        {
            try
            {
                var a = ProceduralAudio.Music(false);
                lock (gate) { menuData = a; }
                var b = ProceduralAudio.Music(true);
                lock (gate) { runData = b; }
            }
            catch (Exception e) { Debug.LogWarning("Music synth failed: " + e.Message); }
        }

        void Update()
        {
            bool changed = false;
            lock (gate)
            {
                if (menuData != null) { menuClip = ProceduralAudio.ToClip("music_menu", menuData); menuData = null; changed = true; }
                if (runData != null) { runClip = ProceduralAudio.ToClip("music_run", runData); runData = null; changed = true; }
            }
            if (changed) RefreshMusic();
        }

        public void PlayMusic(bool run)
        {
            wantRun = run;
            RefreshMusic();
        }

        void RefreshMusic()
        {
            AudioClip want = wantRun ? (runOverride != null ? runOverride : runClip) : (menuOverride != null ? menuOverride : menuClip);
            if (want == null) want = wantRun ? (menuOverride != null ? menuOverride : menuClip) : null;
            if (want == null) return;
            if (playing != want)
            {
                playing = want;
                music.clip = want;
                music.Play();
            }
            else if (!music.isPlaying)
            {
                music.Play();
            }
        }

        public void ApplySettings()
        {
            var d = SaveSystem.Data;
            music.volume = d.musicVolume * 0.6f;
            music.mute = !d.musicOn;
        }

        public void Play(Sfx s, float pitch = 1f, float vol = 1f)
        {
            var d = SaveSystem.Data;
            if (!d.sfxOn) return;
            AudioClip clip;
            if (!sfx.TryGetValue(s, out clip) || clip == null) return;
            var src = pool[poolIdx];
            poolIdx = (poolIdx + 1) % pool.Length;
            src.pitch = pitch;
            src.volume = d.sfxVolume * vol;
            src.clip = clip;
            src.Play();
        }

        /// <summary>Вибрация (только Android; Handheld.Vibrate вызывает добавление разрешения VIBRATE).</summary>
        public static void Vibrate()
        {
            if (!SaveSystem.Data.vibration) return;
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }
    }
}
