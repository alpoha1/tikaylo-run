using System;
using UnityEngine;

namespace TilkayoRun
{
    /// <summary>
    /// Полностью процедурный звук: ни одного внешнего аудиофайла.
    /// Все функции чистые (без вызовов Unity API, кроме ToClip), поэтому музыку можно генерировать в потоке.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int SR = 22050;
        const double TwoPi = Math.PI * 2.0;

        public static AudioClip ToClip(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static double Osc(int wave, double ph)
        {
            double x = ph / TwoPi;
            x -= Math.Floor(x);
            switch (wave)
            {
                case 0: return Math.Sin(ph);
                case 1: return x < 0.5 ? 0.6 : -0.6;
                case 2: return 4.0 * Math.Abs(x - 0.5) - 1.0;
                default: return 2.0 * x - 1.0;
            }
        }

        public static double Freq(int midi) { return 440.0 * Math.Pow(2.0, (midi - 69) / 12.0); }

        // ---------- Базовые голоса ----------
        static float[] Sweep(double f0, double f1, double len, double vol, int wave, double decay)
        {
            int n = (int)(len * SR);
            var b = new float[n];
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double f = f0 + (f1 - f0) * t;
                ph += TwoPi * f / SR;
                double env = Math.Pow(1.0 - t, decay) * Math.Min(1.0, i / (SR * 0.004));
                b[i] = (float)(Osc(wave, ph) * env * vol);
            }
            return b;
        }

        static float[] Noise(double len, double vol, double decay, double lp, int seed)
        {
            int n = (int)(len * SR);
            var b = new float[n];
            var r = new System.Random(seed);
            double prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double s = r.NextDouble() * 2.0 - 1.0;
                prev += (s - prev) * lp;
                b[i] = (float)(prev * Math.Pow(1.0 - t, decay) * vol);
            }
            return b;
        }

        static void Mix(float[] dst, float[] src, double startSec)
        {
            int o = (int)(startSec * SR);
            for (int i = 0; i < src.Length && o + i < dst.Length; i++) dst[o + i] += src[i];
        }

        static float[] Empty(double len) { return new float[(int)(len * SR)]; }

        // ---------- Звуковые эффекты ----------
        public static float[] Coin()
        {
            var d = Empty(0.36);
            Mix(d, Sweep(988, 988, 0.07, 0.5, 1, 1.0), 0);
            Mix(d, Sweep(1319, 1319, 0.28, 0.5, 1, 2.2), 0.06);
            Mix(d, Sweep(1976, 2637, 0.2, 0.12, 0, 2.0), 0.06);
            return d;
        }

        public static float[] Jump() { return Sweep(260, 640, 0.2, 0.55, 2, 1.2); }

        public static float[] Slide() { return Noise(0.24, 0.5, 1.4, 0.22, 5); }

        public static float[] Lane() { return Sweep(520, 720, 0.07, 0.25, 0, 1.0); }

        public static float[] Crash()
        {
            var d = Empty(0.7);
            Mix(d, Noise(0.65, 0.9, 2.0, 0.35, 11), 0);
            Mix(d, Sweep(170, 38, 0.55, 0.9, 3, 1.5), 0);
            return d;
        }

        public static float[] CaseOpen()
        {
            var d = Empty(1.5);
            int[] notes = { 72, 76, 79, 84, 88, 91 };
            for (int i = 0; i < notes.Length; i++)
                Mix(d, Sweep(Freq(notes[i]), Freq(notes[i]), 0.3, 0.32, 2, 1.8), i * 0.075);
            double t = notes.Length * 0.075 + 0.05;
            int[] chord = { 72, 76, 79, 84 };
            for (int i = 0; i < chord.Length; i++)
                Mix(d, Sweep(Freq(chord[i]), Freq(chord[i]), 0.9, 0.22, 0, 1.4), t);
            Mix(d, Noise(0.5, 0.18, 2.5, 0.8, 21), t);
            return d;
        }

        public static float[] Click() { return Sweep(900, 560, 0.05, 0.4, 0, 2.0); }

        public static float[] Tick() { return Sweep(1500, 1500, 0.022, 0.32, 1, 1.0); }

        public static float[] NewRecord()
        {
            var d = Empty(1.6);
            int[] notes = { 67, 72, 76, 79 };
            for (int i = 0; i < notes.Length; i++)
                Mix(d, Sweep(Freq(notes[i]), Freq(notes[i]), 0.16, 0.35, 2, 1.0), i * 0.12);
            int[] chord = { 72, 76, 79, 84 };
            for (int i = 0; i < chord.Length; i++)
                Mix(d, Sweep(Freq(chord[i]), Freq(chord[i]), 0.9, 0.2, 1, 1.3), 0.5);
            return d;
        }

        // ---------- Музыка ----------
        static void Note(float[] buf, double start, double len, double freq, double vol, int wave, double attack, double release, double decay)
        {
            int s0 = (int)(start * SR);
            int n = (int)((len + release) * SR);
            double ph = 0, dph = TwoPi * freq / SR;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)SR;
                double env = Math.Min(1.0, t / Math.Max(0.001, attack));
                if (t > len) env *= Math.Max(0.0, 1.0 - (t - len) / Math.Max(0.001, release));
                if (decay > 0) env *= Math.Exp(-t * decay);
                int idx = (s0 + i) % buf.Length;
                buf[idx] += (float)(Osc(wave, ph) * env * vol);
                ph += dph;
            }
        }

        static void Kick(float[] buf, double start, double vol)
        {
            int s0 = (int)(start * SR);
            int n = (int)(0.22 * SR);
            double ph = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double f = 48.0 + 90.0 * Math.Exp(-t * 9.0);
                ph += TwoPi * f / SR;
                buf[(s0 + i) % buf.Length] += (float)(Math.Sin(ph) * Math.Pow(1.0 - t, 2.0) * vol);
            }
        }

        static void Hat(float[] buf, double start, double vol, System.Random r)
        {
            int s0 = (int)(start * SR);
            int n = (int)(0.04 * SR);
            double prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double s = r.NextDouble() * 2.0 - 1.0;
                double hp = s - prev; prev = s;
                buf[(s0 + i) % buf.Length] += (float)(hp * Math.Pow(1.0 - t, 2.0) * vol);
            }
        }

        static void Snare(float[] buf, double start, double vol, System.Random r)
        {
            int s0 = (int)(start * SR);
            int n = (int)(0.14 * SR);
            double prev = 0;
            for (int i = 0; i < n; i++)
            {
                double t = i / (double)n;
                double s = r.NextDouble() * 2.0 - 1.0;
                prev += (s - prev) * 0.6;
                buf[(s0 + i) % buf.Length] += (float)(prev * Math.Pow(1.0 - t, 2.2) * vol);
            }
        }

        /// <summary>Зацикленная мелодия: a-moll, прогрессия Am–F–C–G. run=true — бодрый вариант для забега.</summary>
        public static float[] Music(bool run)
        {
            int bpm = run ? 126 : 82;
            double beat = 60.0 / bpm;
            double barLen = beat * 4.0;
            const int bars = 8;
            var buf = new float[(int)(barLen * bars * SR)];
            var rnd = new System.Random(run ? 11 : 5);

            int[] roots = { 57, 53, 48, 55 };
            int[][] chords =
            {
                new[] { 0, 3, 7 },
                new[] { 0, 4, 7 },
                new[] { 0, 4, 7 },
                new[] { 0, 4, 7 }
            };
            int[] pent = { 0, 3, 5, 7, 10 };

            for (int bar = 0; bar < bars; bar++)
            {
                int ci = bar / 2;
                int root = roots[ci];
                int[] ch = chords[ci];
                double t0 = bar * barLen;

                if (bar % 2 == 0)
                {
                    for (int k = 0; k < ch.Length; k++)
                        Note(buf, t0, barLen * 2.0, Freq(root + 12 + ch[k]), run ? 0.045 : 0.07, 0, 0.5, 0.6, 0);
                }

                if (run)
                {
                    for (int e = 0; e < 8; e++)
                    {
                        int semis = (e % 4 == 2) ? 7 : 0;
                        Note(buf, t0 + e * beat * 0.5, beat * 0.45, Freq(root - 12 + semis), 0.17, 2, 0.01, 0.05, 2.5);
                    }
                }
                else
                {
                    Note(buf, t0, beat * 1.9, Freq(root - 12), 0.18, 2, 0.02, 0.2, 1.2);
                    Note(buf, t0 + beat * 2.0, beat * 1.9, Freq(root - 12 + 7), 0.14, 2, 0.02, 0.2, 1.2);
                }

                int steps = run ? 16 : 8;
                double step = barLen / steps;
                for (int s = 0; s < steps; s++)
                {
                    int idx = (s * 2 + bar + (s % 4 == 3 ? 1 : 0)) % ch.Length;
                    int note = root + 24 + ch[idx];
                    Note(buf, t0 + s * step, step * 0.9, Freq(note), run ? 0.07 : 0.06, 0, 0.004, 0.08, run ? 7.0 : 4.0);
                }

                if (bar % 2 == 1)
                {
                    for (int m = 0; m < 4; m++)
                    {
                        int p = pent[rnd.Next(pent.Length)];
                        Note(buf, t0 + m * beat, beat * 0.9, Freq(root + 36 + p), 0.05, 2, 0.01, 0.2, 2.0);
                    }
                }

                if (run)
                {
                    for (int b = 0; b < 4; b++)
                    {
                        Kick(buf, t0 + b * beat, 0.55);
                        Hat(buf, t0 + b * beat + beat * 0.5, 0.07, rnd);
                        if (b == 1 || b == 3) Snare(buf, t0 + b * beat, 0.16, rnd);
                    }
                }
                else
                {
                    Hat(buf, t0 + beat * 1.5, 0.03, rnd);
                    Hat(buf, t0 + beat * 3.5, 0.03, rnd);
                }
            }

            for (int i = 0; i < buf.Length; i++) buf[i] = (float)Math.Tanh(buf[i] * 1.15);
            return buf;
        }
    }
}
