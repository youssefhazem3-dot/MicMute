using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace MicMute;

public static class AudioFeedback
{
    private static SoundPlayer? _mutePlayer;
    private static SoundPlayer? _unmutePlayer;
    private static bool _isInitialized;
    private static readonly object _initLock = new object();
    private static readonly object _playLock = new object();

    public static void Initialize()
    {
        if (_isInitialized) return;
        lock (_initLock)
        {
            if (_isInitialized) return;
            try
            {
                _mutePlayer = LoadSoundPlayer("MicMute.sounds.mute.wav", isMuted: true);
                _unmutePlayer = LoadSoundPlayer("MicMute.sounds.unmute.wav", isMuted: false);
                _mutePlayer.Load();
                _unmutePlayer.Load();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("AudioFeedback initialization failed", ex);
            }
        }
    }

    public static void Play(bool isMuted)
    {
        Task.Run(() =>
        {
            try
            {
                if (!_isInitialized) Initialize();
                lock (_playLock)
                {
                    if (isMuted)
                    {
                        _mutePlayer?.Play();
                    }
                    else
                    {
                        _unmutePlayer?.Play();
                    }
                }
            }
            catch
            {
                try { SystemSounds.Beep.Play(); } catch { }
            }
        });
    }

    private static SoundPlayer LoadSoundPlayer(string resourceName, bool isMuted)
    {
        // 1. Try loading from embedded assembly manifest resource
        try
        {
            using Stream? stream = typeof(AudioFeedback).Assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;
                return new SoundPlayer(ms);
            }
        }
        catch
        {
        }

        // 2. Try loading from disk relative to application directory
        try
        {
            string fileName = isMuted ? "mute.wav" : "unmute.wav";
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", fileName);
            if (File.Exists(diskPath))
            {
                byte[] bytes = File.ReadAllBytes(diskPath);
                return new SoundPlayer(new MemoryStream(bytes));
            }
        }
        catch
        {
        }

        // 3. Fallback: Synthesize a smooth, acoustic Discord-like two-tone chime
        return CreateDiscordChimePlayer(isMuted);
    }

    /// <summary>
    /// Fallback acoustic chime generator: Synthesizes a soft, harmonic two-tone chime
    /// inspired by Discord's smooth marimba earcon (two-tone descending for mute, ascending for unmute).
    /// </summary>
    public static SoundPlayer CreateDiscordChimePlayer(bool isMuted)
    {
        const int sampleRate = 44100;
        const int totalDurationMs = 240;
        int numSamples = (sampleRate * totalDurationMs) / 1000;
        short[] samples = new short[numSamples];

        // Mute: Descending melody (E5 ~659Hz -> A4 440Hz)
        // Unmute: Ascending melody (A4 440Hz -> E5 ~659Hz)
        double note1Freq = isMuted ? 659.25 : 440.0;
        double note2Freq = isMuted ? 440.0 : 659.25;

        int note1Start = 0;
        int note2Start = (sampleRate * 70) / 1000; // 70ms offset

        for (int i = 0; i < numSamples; i++)
        {
            double sampleVal = 0.0;

            // Note 1 contribution
            if (i >= note1Start)
            {
                double dt = (double)(i - note1Start) / sampleRate;
                double attack = dt < 0.004 ? Math.Sin(dt / 0.004 * (Math.PI / 2.0)) : 1.0;
                double decay = Math.Exp(-16.0 * dt);
                double noteSample = Math.Sin(2.0 * Math.PI * note1Freq * dt)
                                  + 0.18 * Math.Sin(4.0 * Math.PI * note1Freq * dt)
                                  + 0.04 * Math.Sin(6.0 * Math.PI * note1Freq * dt);
                sampleVal += noteSample * attack * decay;
            }

            // Note 2 contribution
            if (i >= note2Start)
            {
                double dt = (double)(i - note2Start) / sampleRate;
                double attack = dt < 0.004 ? Math.Sin(dt / 0.004 * (Math.PI / 2.0)) : 1.0;
                double decay = Math.Exp(-14.0 * dt);
                double noteSample = Math.Sin(2.0 * Math.PI * note2Freq * dt)
                                  + 0.18 * Math.Sin(4.0 * Math.PI * note2Freq * dt)
                                  + 0.04 * Math.Sin(6.0 * Math.PI * note2Freq * dt);
                sampleVal += noteSample * attack * decay;
            }

            double clamped = Math.Clamp(sampleVal * 12000.0, -32767.0, 32767.0);
            samples[i] = (short)clamped;
        }

        byte[] wavBytes = CreateWavBytes(samples, sampleRate);
        return new SoundPlayer(new MemoryStream(wavBytes));
    }

    private static byte[] CreateWavBytes(short[] samples, int sampleRate)
    {
        using MemoryStream ms = new MemoryStream();
        using BinaryWriter bw = new BinaryWriter(ms);

        int subChunk2Size = samples.Length * 2;
        int chunkSize = 36 + subChunk2Size;

        bw.Write(new char[] { 'R', 'I', 'F', 'F' });
        bw.Write(chunkSize);
        bw.Write(new char[] { 'W', 'A', 'V', 'E' });

        bw.Write(new char[] { 'f', 'm', 't', ' ' });
        bw.Write(16);
        bw.Write((short)1); // PCM format
        bw.Write((short)1); // Mono
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2); // Block align
        bw.Write((short)16); // Bits per sample

        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(subChunk2Size);
        foreach (short sample in samples)
        {
            bw.Write(sample);
        }

        return ms.ToArray();
    }
}
