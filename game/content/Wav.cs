using System;
using System.Buffers.Binary;
using Godot;

/// <summary>
/// Sounds made in code rather than loaded from files, standing in until real sounds come from Packs:
/// samples from -1 to 1 packed into a 16-bit WAV, and the beep the HUD's tones are made of.
/// </summary>
public static class Wav
{
    /// <summary>Samples per second.</summary>
    public const int Rate = 44100;

    /// <summary>The samples as a WAV that plays once or, with <paramref name="loop"/>, loops from its end to its start.</summary>
    public static AudioStreamWav Pack(float[] samples, bool loop)
    {
        var data = new byte[samples.Length * 2];
        for (var i = 0; i < samples.Length; i++)
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(samples[i] * short.MaxValue));
        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = Rate,
            LoopMode = loop ? AudioStreamWav.LoopModeEnum.Forward : AudioStreamWav.LoopModeEnum.Disabled,
            LoopEnd = loop ? samples.Length : 0,
        };
    }

    /// <summary>
    /// <paramref name="period"/> seconds of samples: a beep at <paramref name="pitch"/> Hz peaking at
    /// <paramref name="loudness"/> for its first <paramref name="length"/> seconds, fading in and out
    /// over <paramref name="fade"/> seconds so it doesn't click, then silence.
    /// </summary>
    public static float[] Beep(float loudness, int pitch, float length, float fade, float period)
    {
        var samples = new float[(int)(Rate * period)];
        for (var i = 0; i < samples.Length; i++)
        {
            var t = (float)i / Rate;
            var envelope = Math.Clamp(Math.Min(t, length - t) / fade, 0, 1);
            samples[i] = loudness * envelope * MathF.Sin(MathF.Tau * pitch * t);
        }
        return samples;
    }
}
