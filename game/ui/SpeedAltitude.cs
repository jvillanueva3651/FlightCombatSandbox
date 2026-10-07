using Godot;

/// <summary>
/// The jet's speed in km/h and its altitude above sea level in meters, each in a box either side of
/// the middle of the screen, pointing in toward it, with SPEED or ALT above it.
/// </summary>
public partial class SpeedAltitude : Control
{
    const float FromMiddle = 150;  // pixels from the middle of the screen to each box's point
    const float BoxWidth = 84, BoxHeight = 28, PointLength = 10;  // pixels
    const int FontSize = 20, LabelSize = 16;

    Jet _jet;

    public override void _Ready() => _jet = ((Hud)Owner).Jet;

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var middle = GetViewportRect().Size / 2;
        Box(middle + new Vector2(-FromMiddle, 0), -1, "SPEED", $"{_jet.Speed:0}");
        Box(middle + new Vector2(FromMiddle, 0), 1, "ALT", $"{_jet.GlobalPosition.Y:0}");
    }

    // A box with its point at `point` and its body on one side of it: -1 for the left, 1 for the right.
    void Box(Vector2 point, float side, string label, string value)
    {
        var inner = point.X + side * PointLength;  // where the body meets the point
        var outer = inner + side * BoxWidth;
        var top = point.Y - BoxHeight / 2;
        var bottom = point.Y + BoxHeight / 2;
        DrawPolyline(new Vector2[] { point, new(inner, top), new(outer, top), new(outer, bottom), new(inner, bottom), point }, Hud.Green, 2);
        var left = Mathf.Min(inner, outer);
        Hud.DrawText(this, new Vector2(left, bottom - 6), value, Hud.Green, FontSize, HorizontalAlignment.Center, BoxWidth);
        Hud.DrawText(this, new Vector2(left, top - 6), label, Hud.Green, LabelSize);
    }
}
