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
                _mutePlayer = CreateTonePlayer(420, 60);
                _unmutePlayer = CreateTonePlayer(840, 60);
                _mutePlayer.Load();
                _unmutePlayer.Load();
                _isInitialized = true;
            }
            catch
            {
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

    private static SoundPlayer CreateTonePlayer(int frequency, int durationMs)
    {
        int sampleRate = 44100;
        int numSamples = (sampleRate * durationMs) / 1000;
        short[] samples = new short[numSamples];

        for (int i = 0; i < numSamples; i++)
        {
            double t = (double)i / sampleRate;
            double envelope = 1.0;
            if (i < 200) envelope = (double)i / 200.0;
            else if (i > numSamples - 200) envelope = (double)(numSamples - i) / 200.0;

            samples[i] = (short)(Math.Sin(2.0 * Math.PI * frequency * t) * 10000.0 * envelope);
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
        bw.Write((short)1);
        bw.Write((short)1);
        bw.Write(sampleRate);
        bw.Write(sampleRate * 2);
        bw.Write((short)2);
        bw.Write((short)16);

        bw.Write(new char[] { 'd', 'a', 't', 'a' });
        bw.Write(subChunk2Size);
        foreach (short sample in samples)
        {
            bw.Write(sample);
        }

        return ms.ToArray();
    }
}
