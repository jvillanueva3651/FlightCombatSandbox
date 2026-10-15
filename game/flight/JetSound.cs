using System;
using System.Linq;
using Godot;

/// <summary>
/// Placeholder engine and wind sounds for its parent jet, made in code instead of loaded from files.
/// The engine roars and whines higher and louder as the throttle spools it up, easing like a real
/// engine; the wind rushes higher and louder with speed.
/// </summary>
public partial class JetSound : Node
{
    const float Loudness = 0.027f; // how loud each sound peaks, as a fraction of the loudest a sound can be
    const float WindLevel = 0.3f;  // the wind at top speed, as a fraction of the engine at full throttle
    const float SpoolTime = 0.6f;  // seconds the engine takes to get about two thirds of the way to a new throttle

    Jet _jet;
    AudioStreamPlayer _engine, _wind;
    float _spool = 0.5f;  // how spooled up the engine is: 0 at full L2, 0.5 with neither trigger, 1 at full R2

    public override void _Ready()
    {
        _jet = GetParent<Jet>();
        _engine = Play(Loop(cutoff: 300, whine: 1500, whineLevel: 0.15f, seed: 1));  // a deep roar with a turbine whine
        _wind = Play(Loop(cutoff: 1200, whine: 0, whineLevel: 0, seed: 2));          // a brighter rush
    }

    // Runs after the jet's own _Process, since children process after their parent.
    public override void _Process(double delta)
    {
        // The engine follows the throttle: quiet at full L2, louder with neither trigger, and loudest by far
        // at full R2.
        _spool = Mathf.Lerp(_spool, 0.5f + 0.5f * _jet.Throttle, 1 - Mathf.Exp(-(float)delta / SpoolTime));
        _engine.PitchScale = 0.6f + 0.8f * _spool;
        _engine.VolumeDb = Mathf.LinearToDb(0.2f + 0.8f * _spool * _spool);

        // The wind follows speed, quieter than the engine.
        var wind = Mathf.Clamp(_jet.Speed / _jet.Handling.TopSpeed, 0, 1);
        _wind.PitchScale = 0.5f + wind;
        _wind.VolumeDb = Mathf.LinearToDb(WindLevel * wind);

        // Being shot down silences the jet until it starts again.
        _engine.StreamPaused = _wind.StreamPaused = _jet.ShotDown;
    }

    AudioStreamPlayer Play(AudioStream stream)
    {
        var player = new AudioStreamPlayer { Stream = stream };
        AddChild(player);
        player.Play();
        return player;
    }

    // Two seconds of sound that loops without a click: noise smoothed down to the sounds below about
    // cutoff Hz, mixed with a tone at whine Hz (a whole number, so it fits the loop exactly).
    static AudioStreamWav Loop(float cutoff, int whine, float whineLevel, int seed)
    {
        var random = new Random(seed);
        var noise = new float[Wav.Rate * 2];
        for (var i = 0; i < noise.Length; i++)
            noise[i] = random.NextSingle() * 2 - 1;

        // A running average keeps the low sounds; going round twice makes the end flow into the start.
        var smoothing = 1 - MathF.Exp(-MathF.Tau * cutoff / Wav.Rate);
        var smoothed = new float[noise.Length];
        var y = 0f;
        for (var pass = 0; pass < 2; pass++)
            for (var i = 0; i < noise.Length; i++)
                smoothed[i] = y += smoothing * (noise[i] - y);
        var peak = smoothed.Max(MathF.Abs);

        var samples = new float[noise.Length];
        for (var i = 0; i < noise.Length; i++)
            samples[i] = Loudness * ((1 - whineLevel) * smoothed[i] / peak + whineLevel * MathF.Sin(MathF.Tau * whine * i / Wav.Rate));
        return Wav.Pack(samples, loop: true);
    }
}
