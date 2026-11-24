using System;
using System.IO;
using UnityEngine;

namespace RootAccess.EditorTools
{
    /// <summary>Procedural placeholder sound synthesis (oscillators, noise, envelopes) and 16-bit PCM WAV writing.</summary>
    public static class ProceduralAudio
    {
        public const int SampleRate = 44100;

        public enum Wave
        {
            Sine,
            Square,
            Saw,
            Triangle,
            Noise
        }

        public static float[] Buffer(float seconds) => new float[Mathf.CeilToInt(seconds * SampleRate)];

        /// <summary>Adds an oscillator with a linear frequency sweep and attack/decay envelope.</summary>
        public static void AddTone(float[] buffer, Wave wave, float startHz, float endHz, float startSec, float lengthSec,
            float volume, float attack = 0.005f, float release = 0.05f, int seed = 1)
        {
            var random = new System.Random(seed);
            int start = Mathf.FloorToInt(startSec * SampleRate);
            int length = Mathf.FloorToInt(lengthSec * SampleRate);
            double phase = 0;
            float lowpass = 0f;
            for (int i = 0; i < length && start + i < buffer.Length; i++)
            {
                float t = i / (float)SampleRate;
                float progress = i / (float)Mathf.Max(1, length);
                float frequency = Mathf.Lerp(startHz, endHz, progress);
                phase += frequency / SampleRate;
                float p = (float)(phase - Math.Floor(phase));
                float sample = wave switch
                {
                    Wave.Sine => Mathf.Sin(p * Mathf.PI * 2f),
                    Wave.Square => p < 0.5f ? 0.6f : -0.6f,
                    Wave.Saw => (p * 2f - 1f) * 0.6f,
                    Wave.Triangle => 1f - 4f * Mathf.Abs(p - 0.5f),
                    _ => (float)(random.NextDouble() * 2.0 - 1.0)
                };

                if (wave == Wave.Noise)
                {
                    float cutoff = Mathf.Clamp01(frequency / 8000f);
                    lowpass += (sample - lowpass) * cutoff;
                    sample = lowpass;
                }

                float env = Envelope(t, lengthSec, attack, release);
                buffer[start + i] += sample * volume * env;
            }
        }

        public static float Envelope(float t, float length, float attack, float release)
        {
            float a = attack > 0f ? Mathf.Clamp01(t / attack) : 1f;
            float r = release > 0f ? Mathf.Clamp01((length - t) / release) : 1f;
            return a * r;
        }

        public static void Normalize(float[] buffer, float peak = 0.85f)
        {
            float max = 0.0001f;
            foreach (float s in buffer) max = Mathf.Max(max, Mathf.Abs(s));
            float gain = peak / max;
            for (int i = 0; i < buffer.Length; i++) buffer[i] *= gain;
        }

        /// <summary>Crossfades the tail into the head so the clip loops without a click.</summary>
        public static void MakeLoopable(float[] buffer, float fadeSeconds = 0.05f)
        {
            int fade = Mathf.Min(buffer.Length / 4, Mathf.FloorToInt(fadeSeconds * SampleRate));
            for (int i = 0; i < fade; i++)
            {
                float t = i / (float)fade;
                int tail = buffer.Length - fade + i;
                buffer[i] = buffer[i] * t + buffer[tail] * (1f - t);
            }
        }

        public static void WriteWav(string fullPath, float[] samples)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
            using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(stream);
            int dataSize = samples.Length * 2;
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + dataSize);
            writer.Write(new[] { 'W', 'A', 'V', 'E' });
            writer.Write(new[] { 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(dataSize);
            foreach (float sample in samples)
                writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }
    }
}
