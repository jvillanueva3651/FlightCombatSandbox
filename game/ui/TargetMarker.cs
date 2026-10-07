using Godot;

/// <summary>
/// The HUD box around the Player's current Target, with its distance in meters underneath. Hidden
/// when there's no Target or the Target is behind the camera.
/// </summary>
public partial class TargetMarker : Control
{
    static readonly Color Green = new(0.35f, 1, 0.45f);  // Ace Combat's HUD green
    const float BoxSize = 40;  // pixels
    const int FontSize = 16;

    Jet _jet;
    Targeting _targeting;
    float _distance;  // meters

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _targeting = _jet.GetNode<Targeting>("Targeting");
    }

    public override void _Process(double delta)
    {
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
        DrawRect(new Rect2(-Vector2.One * BoxSize / 2, Vector2.One * BoxSize), Green, filled: false, width: 2);
        var text = $"{_distance:0} m";
        var at = new Vector2(-BoxSize / 2, BoxSize / 2 + FontSize + 2);
        DrawStringOutline(ThemeDB.FallbackFont, at, text, fontSize: FontSize, size: 4, modulate: Colors.Black);
        DrawString(ThemeDB.FallbackFont, at, text, fontSize: FontSize, modulate: Green);
    }
}
