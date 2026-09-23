using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace MicMute;

public static class AudioFeedback
{
    private static SoundPlayer? _mutePlayer;
    private static SoundPlayer? _unmutePlayer;
    private static byte[]? _rawMuteWavBytes;
    private static byte[]? _rawUnmuteWavBytes;
    private static int _volumePercent = 100;
    private static bool _isInitialized;
    private static readonly object _initLock = new object();
    private static readonly object _playLock = new object();

    public static int CurrentVolume => _volumePercent;

    public static void Initialize()
    {
        if (_isInitialized) return;
        lock (_initLock)
        {
            if (_isInitialized) return;
            try
            {
                _rawMuteWavBytes = LoadRawWavBytes("MicMute.sounds.mute.wav", isMuted: true);
                _rawUnmuteWavBytes = LoadRawWavBytes("MicMute.sounds.unmute.wav", isMuted: false);
                ApplyVolumeLocked(_volumePercent);
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                DiagnosticLogger.LogError("AudioFeedback initialization failed", ex);
            }
        }
    }

    public static void SetVolume(int volumePercent)
    {
        int clamped = Math.Clamp(volumePercent, 0, 100);
        lock (_initLock)
        {
            if (_isInitialized && _volumePercent == clamped) return;
            _volumePercent = clamped;
            if (_isInitialized)
            {
                ApplyVolumeLocked(clamped);
            }
        }
    }

    private static void ApplyVolumeLocked(int volumePercent)
    {
        float factor = volumePercent / 100.0f;
        byte[] muteBytes = ScaleWavVolume(_rawMuteWavBytes, factor);
        byte[] unmuteBytes = ScaleWavVolume(_rawUnmuteWavBytes, factor);

        lock (_playLock)
        {
            _mutePlayer = new SoundPlayer(new MemoryStream(muteBytes));
            _unmutePlayer = new SoundPlayer(new MemoryStream(unmuteBytes));
            try { _mutePlayer.Load(); } catch { }
            try { _unmutePlayer.Load(); } catch { }
        }
    }

    public static void Play(bool isMuted)
    {
        if (_volumePercent <= 0) return;
        Task.Run(() =>
        {
            try
            {
                if (!_isInitialized) Initialize();
                if (_volumePercent <= 0) return;
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

    private static byte[] LoadRawWavBytes(string resourceName, bool isMuted)
    {
        // 1. Try loading from embedded assembly manifest resource
        try
        {
            using Stream? stream = typeof(AudioFeedback).Assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using MemoryStream ms = new MemoryStream();
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }
        catch { }

        // 2. Try loading from disk relative to application directory
        try
        {
            string fileName = isMuted ? "mute.wav" : "unmute.wav";
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sounds", fileName);
            if (File.Exists(diskPath))
            {
                return File.ReadAllBytes(diskPath);
            }
        }
        catch { }

        // 3. Fallback: Synthesize a smooth, acoustic Discord-like two-tone chime
        return SynthesizeDiscordChimeBytes(isMuted, 1.0);
    }

    public static byte[] ScaleWavVolume(byte[]? wavBytes, float volume)
    {
        if (wavBytes == null || wavBytes.Length < 44) return wavBytes ?? Array.Empty<byte>();
        float safeVolume = Math.Clamp(volume, 0.0f, 1.0f);
        if (safeVolume >= 0.999f) return (byte[])wavBytes.Clone();

        byte[] scaled = (byte[])wavBytes.Clone();
        int dIdx = FindDataChunkIndex(scaled);
        if (dIdx >= 0 && dIdx + 8 <= scaled.Length)
        {
            int dataSize = BitConverter.ToInt32(scaled, dIdx + 4);
            int start = dIdx + 8;
            int end = Math.Min(start + dataSize, scaled.Length);
            if (safeVolume <= 0.001f)
            {
                Array.Clear(scaled, start, end - start);
            }
            else
            {
                for (int i = start; i + 1 < end; i += 2)
                {
                    short sample = BitConverter.ToInt16(scaled, i);
                    short newSample = (short)Math.Clamp((int)Math.Round(sample * safeVolume), short.MinValue, short.MaxValue);
                    scaled[i] = (byte)(newSample & 0xFF);
                    scaled[i + 1] = (byte)((newSample >> 8) & 0xFF);
                }
            }
        }
        return scaled;
    }

    private static int FindDataChunkIndex(byte[] bytes)
    {
        for (int i = 12; i <= bytes.Length - 8; i++)
        {
            if (bytes[i] == 0x64 && bytes[i + 1] == 0x61 && bytes[i + 2] == 0x74 && bytes[i + 3] == 0x61)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Fallback acoustic chime generator: Synthesizes a soft, harmonic two-tone chime
    /// inspired by Discord's smooth marimba earcon (two-tone descending for mute, ascending for unmute).
    /// </summary>
    public static SoundPlayer CreateDiscordChimePlayer(bool isMuted, double volume = 1.0)
    {
        byte[] wavBytes = SynthesizeDiscordChimeBytes(isMuted, volume);
        return new SoundPlayer(new MemoryStream(wavBytes));
    }

    public static byte[] SynthesizeDiscordChimeBytes(bool isMuted, double volume = 1.0)
    {
        const int sampleRate = 44100;
        const int totalDurationMs = 240;
        int numSamples = (sampleRate * totalDurationMs) / 1000;
        short[] samples = new short[numSamples];
        double safeVol = Math.Clamp(volume, 0.0, 1.0);

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

            double clamped = Math.Clamp(sampleVal * 12000.0 * safeVol, -32767.0, 32767.0);
            samples[i] = (short)clamped;
        }

        return CreateWavBytes(samples, sampleRate);
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
