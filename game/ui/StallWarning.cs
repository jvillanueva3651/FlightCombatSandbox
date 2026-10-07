using System.Linq;
using Godot;

/// <summary>
/// The stall warning: STALL in a box below the middle of the screen while the jet is below stall
/// speed, and a voice saying "Stall warning" when the stall starts and every few seconds while it
/// lasts. The voice is Windows' own text-to-speech, its Zira voice (the user's pick), standing in
/// for a recording until task 6.26. Where there's no text-to-speech, as in headless runs, it stays
/// silent.
/// </summary>
public partial class StallWarning : Control
{
    const float BelowMiddle = 150;  // pixels from the middle of the screen down to the box's middle
    const float BoxWidth = 110, BoxHeight = 36;  // pixels
    const int FontSize = 28;
    const float RepeatTime = 3;  // seconds from one "Stall warning" to the next
    const int Volume = 100;      // from 0 to 100
    const float Rate = 1.1f;     // how fast it speaks: 1 is normal

    Jet _jet;
    string _voice;
    float _sinceSpoken = RepeatTime;  // seconds since the voice last spoke, so a first stall speaks at once

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        if (DisplayServer.HasFeature(DisplayServer.Feature.TextToSpeech))
            _voice = DisplayServer.TtsGetVoices().FirstOrDefault(voice => voice["name"].AsString().Contains("Zira"))?["id"].AsString()
                ?? DisplayServer.TtsGetVoicesForLanguage("en").FirstOrDefault();
    }

    public override void _Process(double delta)
    {
        Visible = !_jet.ShotDown && _jet.Handling.StallDepth(_jet.Speed) > 0;
        _sinceSpoken += (float)delta;
        if (!Visible)
            return;
        QueueRedraw();
        // Counted from the last time it spoke, so a speed wavering at stall speed can't make it chatter.
        if (_sinceSpoken < RepeatTime)
            return;
        _sinceSpoken = 0;
        if (_voice != null)
            DisplayServer.TtsSpeak("Stall warning", _voice, Volume, rate: Rate, interrupt: true);
    }

    public override void _Draw()
    {
        var middle = GetViewportRect().Size / 2 + new Vector2(0, BelowMiddle);
        var box = new Rect2(middle - new Vector2(BoxWidth, BoxHeight) / 2, BoxWidth, BoxHeight);
        DrawRect(box, Hud.Green, filled: false, width: 2);
        Hud.DrawText(this, new Vector2(box.Position.X, middle.Y + 10), "STALL", Hud.Green, FontSize, HorizontalAlignment.Center, BoxWidth);
    }
}
