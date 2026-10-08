using System;
using System.Buffers.Binary;
using Godot;

/// <summary>
/// The HUD box around the Player's current Target, with its distance in meters underneath: green, and
/// red with a fast beeping lock tone once the Target is locked on. Hidden when there's no Target or
/// the Target is behind the camera.
/// </summary>
public partial class TargetMarker : Control
{
    const float BoxSize = 40;  // pixels
    const int FontSize = 16;

    // The lock tone: a high beep repeated 8 times a second, made in code like the jet's sounds.
    const int Rate = 44100;            // samples per second
    const float ToneLoudness = 0.03f;  // how loud the beep peaks, as a fraction of the loudest a sound can be
    const int TonePitch = 1600;        // Hz
    const float BeepPeriod = 0.125f;   // seconds from one beep's start to the next
    const float BeepLength = 0.08f;    // seconds each beep sounds
    const float BeepFade = 0.005f;     // seconds each beep fades in and out over, so it doesn't click

    Jet _jet;
    Targeting _targeting;
    AudioStreamPlayer _tone;
    float _distance;  // meters

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _targeting = _jet.Targeting;
        _tone = new AudioStreamPlayer { Stream = LockTone() };
        AddChild(_tone);
    }

    public override void _Process(double delta)
    {
        if (_targeting.Locked != _tone.Playing)
        {
            if (_targeting.Locked)
                _tone.Play();
            else
                _tone.Stop();
        }

        var target = _targeting.Target;
        var camera = GetViewport().GetCamera3D();
        Visible = target != null && !camera.IsPositionBehind(target.GlobalPosition);
        if (!Visible)
            return;
        Position = camera.UnprojectPosition(target.GlobalPosition);
        _distance = _jet.GlobalPosition.DistanceTo(target.GlobalPosition);
        QueueRedraw();
    }

    // Drawn around the marker's position, which is the Target's spot on screen.
    public override void _Draw()
    {
        var color = _targeting.Locked ? Hud.Red : Hud.Green;
        DrawRect(new Rect2(-Vector2.One * BoxSize / 2, Vector2.One * BoxSize), color, filled: false, width: 2);
        Hud.DrawText(this, new Vector2(-BoxSize / 2, BoxSize / 2 + FontSize + 2), $"{_distance:0} m", color, FontSize);
    }

    // One beep and the silence after it, looped.
    static AudioStreamWav LockTone()
    {
        var samples = (int)(Rate * BeepPeriod);
        var data = new byte[samples * 2];
        for (var i = 0; i < samples; i++)
        {
            var t = (float)i / Rate;
            var envelope = Math.Clamp(Math.Min(t, BeepLength - t) / BeepFade, 0, 1);
            var sample = ToneLoudness * envelope * MathF.Sin(MathF.Tau * TonePitch * t);
            BinaryPrimitives.WriteInt16LittleEndian(data.AsSpan(i * 2), (short)(sample * short.MaxValue));
        }
        return new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = Rate,
            LoopMode = AudioStreamWav.LoopModeEnum.Forward,
            LoopEnd = samples,
        };
    }
}
