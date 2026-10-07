using System.Linq;
using Godot;

/// <summary>
/// The radar minimap in the HUD's bottom-left corner: a heading-up square, so the way the jet is
/// heading is always straight up, with the jet as a green triangle a little below the middle. Every
/// other Unit in range shows as a triangle pointing the way it faces, red if hostile and blue if
/// allied, and the current Target has a box around it. Two lines and a faint ring mark the missile
/// seeker's cone and lock range: a Target inside both can be locked.
/// </summary>
public partial class Radar : Control
{
    const float Margin = 30;        // pixels from the screen's left and bottom edges
    const float Side = 170;         // pixels along each side of the square
    const float PlayerDown = 0.6f;  // how far down the square the jet sits, as a fraction of its side
    const float Range = 4000;       // meters from the jet up to the square's top edge
    const float PixelsPerMeter = Side * PlayerDown / Range;
    static readonly Color Blue = new(0.3f, 0.6f, 1);
    static readonly Color Faint = new(Hud.Green, 0.35f);

    Jet _jet;
    Targeting _targeting;

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _targeting = _jet.GetNode<Targeting>("Targeting");
    }

    public override void _Process(double delta) => QueueRedraw();

    /// <summary>
    /// Where a point in the world shows on the radar, in pixels from the jet's triangle: x to the
    /// right and y down, with the way the jet is heading straight up.
    /// </summary>
    public Vector2 RadarPoint(Vector3 world) => Flatten(world - _jet.GlobalPosition) * PixelsPerMeter;

    // A direction or offset in the world as seen on the radar, in meters, before scaling.
    Vector2 Flatten(Vector3 offset)
    {
        // The way the jet is heading over the ground: where its nose points, or its top when the nose
        // points almost straight up or down.
        var ahead = new Vector3(-_jet.GlobalBasis.Z.X, 0, -_jet.GlobalBasis.Z.Z);
        if (ahead.Length() < 0.01f)
            ahead = new Vector3(_jet.GlobalBasis.Y.X, 0, _jet.GlobalBasis.Y.Z);
        ahead = ahead.Normalized();
        return new Vector2(offset.Dot(ahead.Cross(Vector3.Up)), -offset.Dot(ahead));
    }

    public override void _Draw()
    {
        var square = new Rect2(Margin, GetViewportRect().Size.Y - Margin - Side, Side, Side);
        var player = square.Position + new Vector2(Side / 2, Side * PlayerDown);
        DrawRect(square, new Color(0, 0, 0, 0.3f));
        DrawRect(square, Hud.Green, filled: false, width: 1.5f);

        // The seeker's cone, out to the square's edge, and its lock range.
        var seeker = Targeting.Seeker;
        foreach (var side in new[] { -1, 1 })
        {
            var direction = Vector2.Up.Rotated(side * Mathf.DegToRad(seeker.SeekerCone));  // a cone under 90 degrees, so it rises
            var reach = Mathf.Min((player.Y - square.Position.Y) / -direction.Y, Side / 2 / Mathf.Abs(direction.X));
            DrawLine(player, player + direction * reach, Faint, 1, antialiased: true);
        }
        DrawArc(player, seeker.LockRange * PixelsPerMeter, 0, Mathf.Tau, 64, Faint, 1, antialiased: true);

        var hostiles = Unit.Hostiles(GetTree(), _jet.Team).ToHashSet();
        foreach (var unit in GetTree().GetNodesInGroup(Unit.Group).Cast<Unit>())
        {
            if (unit == _jet || unit.Health == 0)
                continue;
            var at = player + RadarPoint(unit.GlobalPosition);
            if (!square.Grow(-6).HasPoint(at))
                continue;  // out of range
            DrawMarker(at, Flatten(-unit.GlobalBasis.Z), hostiles.Contains(unit) ? Hud.Red : Blue);
            if (unit == _targeting.Target)
                DrawRect(new Rect2(at - new Vector2(8, 8), 16, 16), Hud.Green, filled: false, width: 1.5f);
        }
        DrawMarker(player, Vector2.Up, Hud.Green);
    }

    // A small triangle at `at` pointing along `facing`, or up when the Unit faces straight up or down.
    void DrawMarker(Vector2 at, Vector2 facing, Color color)
    {
        var tip = facing.Length() < 0.01f ? Vector2.Up : facing.Normalized();
        var across = tip.Orthogonal() * 4.5f;
        DrawColoredPolygon(new[] { at + tip * 7, at - tip * 5 + across, at - tip * 5 - across }, color);
    }
}
