using System.Linq;
using Godot;

/// <summary>
/// The missile warning: MISSILE in a red box above the middle of the screen while a missile homes on
/// the jet, and an alarm whose beeps come faster as the nearest one closes in. Both stop as soon as
/// none does: dodged, exploded or burnt out. The alarm is quieter and lower than the lock tone, since
/// it's the sound the Player hears most.
/// </summary>
public partial class MissileWarning : Control
{
    const float AboveMiddle = 150;  // pixels from the middle of the screen up to the box's middle, as far as STALL's sits below
    const float BoxWidth = 150, BoxHeight = 36;  // pixels
    const int FontSize = 28;

    // The alarm: one soft beep, played again each time the next is due.
    const float Loudness = 0.015f;  // how loud each beep peaks, as a fraction of the loudest a sound can be: half the lock tone's
    const int Pitch = 1000;         // Hz: lower than the lock tone's 1600, so less piercing and easy to tell apart
    const float BeepLength = 0.08f; // seconds each beep sounds
    const float BeepFade = 0.015f;  // seconds each beep fades in and out over: softer than the lock tone's
    const float FarGap = 0.5f, NearGap = 0.12f;    // seconds from one beep to the next, with the nearest missile from FarRange out and from NearRange in
    const float FarRange = 2000, NearRange = 300;  // meters

    Jet _jet;
    AudioStreamPlayer _alarm;
    float _sinceBeep = float.PositiveInfinity;  // seconds since the last beep; endless while silent, so a warning beeps at once

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _alarm = new AudioStreamPlayer { Stream = Wav.Pack(Wav.Beep(Loudness, Pitch, BeepLength, BeepFade, BeepLength), loop: false) };
        AddChild(_alarm);
    }

    public override void _Process(double delta)
    {
        var nearest = Missile.Homing(GetTree(), _jet).Select(missile => missile.GlobalPosition.DistanceTo(_jet.GlobalPosition)).DefaultIfEmpty(float.PositiveInfinity).Min();
        Visible = nearest < float.PositiveInfinity;
        if (!Visible)
        {
            _sinceBeep = float.PositiveInfinity;
            return;
        }
        QueueRedraw();
        _sinceBeep += (float)delta;
        if (_sinceBeep < Mathf.Lerp(NearGap, FarGap, Mathf.Clamp((nearest - NearRange) / (FarRange - NearRange), 0, 1)))
            return;
        _sinceBeep = 0;
        _alarm.Play();
    }

    public override void _Draw()
    {
        var middle = GetViewportRect().Size / 2 - new Vector2(0, AboveMiddle);
        var box = new Rect2(middle - new Vector2(BoxWidth, BoxHeight) / 2, BoxWidth, BoxHeight);
        DrawRect(box, Hud.Red, filled: false, width: 2);
        Hud.DrawText(this, new Vector2(box.Position.X, middle.Y + 10), "MISSILE", Hud.Red, FontSize, HorizontalAlignment.Center, BoxWidth);
    }
}
