using System.Linq;
using Godot;

/// <summary>
/// The radar in the HUD's bottom-left corner. Each press of the touchpad switches it to the next of
/// three modes, easing from one to the other:
/// - default: a small square that turns with the jet, so the way it's heading is always up, with the
///   jet a little below the middle and 4 km of view ahead;
/// - zoomed out: the same square, seeing three times as far;
/// - whole map: the square grows as far as it can without covering the SPEED box and shows the
///   whole terrain north-up, with a grid every 2 km; the jet's arrow moves across it, held at the
///   edge when the jet flies off the terrain.
/// Every other Unit in view shows as a triangle pointing the way it faces, red if hostile and blue
/// if allied, and the current Target has a box around it. In the first two modes, two lines and a
/// faint ring mark the missile seeker's cone and lock range: a Target inside both can be locked.
/// </summary>
public partial class Radar : Control
{
    enum Mode { Default, ZoomedOut, WholeMap }

    const float Margin = 30;         // pixels from the screen's left and bottom edges
    const float Side = 170;          // pixels along each side of the square, until it grows for the whole map
    const float WholeHeight = 0.8f;  // the whole map's largest side, as a fraction of the screen's height
    const float Gap = 10;            // pixels the whole map keeps clear of the SPEED box
    const float JetDown = 0.6f;      // how far down the square the jet sits, as a fraction of its side, until the whole map
    const float Range = 4000;        // meters from the jet up to the square's top edge in the default mode
    const float ZoomOut = 3;         // how many times farther the zoomed-out mode sees
    const float GridStep = 2000;     // meters between the whole map's grid lines
    const float Easing = 0.1f;       // seconds a change of mode takes to get two thirds of the way; three of them, nearly all
    static readonly Color Blue = new(0.3f, 0.6f, 1);
    static readonly Color Faint = new(Hud.Green, 0.35f);

    Jet _jet;
    Targeting _targeting;
    Mode _mode;
    float _scale = Side * JetDown / Range;  // pixels per meter, easing to the mode's
    float _whole;  // from 0 to 1: how far the view has eased into the whole map's size, north-up and centering

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _targeting = _jet.Targeting;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed(Controls.RadarMap))
            _mode = (Mode)(((int)_mode + 1) % 3);
        var ease = 1 - Mathf.Exp(-(float)delta / Easing);
        var scale = _mode switch
        {
            Mode.Default => Side * JetDown / Range,
            Mode.ZoomedOut => Side * JetDown / Range / ZoomOut,
            _ => WholeSide / TestTerrain.Size,
        };
        _scale = Mathf.Lerp(_scale, scale, ease);
        _whole = Mathf.Lerp(_whole, _mode == Mode.WholeMap ? 1 : 0, ease);
        QueueRedraw();
    }

    // Where a point in the world shows on the screen, in the radar's view as it is now.
    Vector2 ToScreen(Vector3 world)
    {
        var box = Box();
        var spin = Spin();
        var jet = Flat(_jet.GlobalPosition);
        var jetSpot = box.Position + new Vector2(box.Size.X / 2, box.Size.Y * JetDown);  // where the jet sits until the whole map
        var mapSpot = box.GetCenter() + jet.Rotated(spin) * _scale;                       // where it sits with the terrain's middle in the middle
        return jetSpot.Lerp(mapSpot, _whole) + (Flat(world) - jet).Rotated(spin) * _scale;
    }

    // As big as fits left of the SPEED box, up to most of the screen's height.
    float WholeSide => Mathf.Min(GetViewportRect().Size.Y * WholeHeight, GetViewportRect().Size.X / 2 - SpeedAltitude.Reach - Gap - Margin);

    Rect2 Box()
    {
        var side = Mathf.Lerp(Side, WholeSide, _whole);
        return new Rect2(Margin, GetViewportRect().Size.Y - Margin - side, side, side);
    }

    // Radians the view turns the world: heading-up turns it so the jet's heading points up, north-up
    // doesn't turn it at all.
    float Spin() => -Mathf.Atan2(Ahead().X, -Ahead().Y) * (1 - _whole);

    // The way the jet is heading over the ground, north up: where its nose points, or its top when the
    // nose points almost straight up or down.
    Vector2 Ahead()
    {
        var ahead = Flat(-_jet.GlobalBasis.Z);
        return (ahead.Length() < 0.01f ? Flat(_jet.GlobalBasis.Y) : ahead).Normalized();
    }

    // A point or direction in the world seen from above, north (-Z) up and east to the right.
    static Vector2 Flat(Vector3 v) => new(v.X, v.Z);

    public override void _Draw()
    {
        var box = Box();
        var spin = Spin();
        DrawRect(box, new Color(0, 0, 0, 0.3f));
        DrawRect(box, Hud.Green, filled: false, width: 1.5f);

        // The whole map's grid, appearing only as the view settles, so no line sticks out of the
        // square while it's still growing.
        var grid = new Color(Faint, Faint.A * Mathf.Clamp((_whole - 0.8f) / 0.2f, 0, 1));
        if (grid.A > 0.01f)
            for (var at = -TestTerrain.Size / 2; at <= TestTerrain.Size / 2; at += GridStep)
            {
                DrawGridLine(box, new Vector3(at, 0, -TestTerrain.Size / 2), new Vector3(at, 0, TestTerrain.Size / 2), grid);
                DrawGridLine(box, new Vector3(-TestTerrain.Size / 2, 0, at), new Vector3(TestTerrain.Size / 2, 0, at), grid);
            }

        // The seeker's cone out to the square's edge and its lock range, fading out for the whole map.
        var jetAt = ToScreen(_jet.GlobalPosition);
        var heading = Ahead().Rotated(spin);
        var faint = new Color(Faint, Faint.A * (1 - _whole));
        if (faint.A > 0.01f)
        {
            var seeker = Targeting.Seeker;
            foreach (var side in new[] { -1, 1 })
            {
                var direction = heading.Rotated(side * Mathf.DegToRad(seeker.SeekerCone));
                DrawLine(jetAt, jetAt + direction * ToEdge(box, jetAt, direction), faint, 1, antialiased: true);
            }
            DrawArc(jetAt, seeker.LockRange * _scale, 0, Mathf.Tau, 64, faint, 1, antialiased: true);
        }

        var inside = box.Grow(-6);
        var hostiles = Unit.Hostiles(GetTree(), _jet.Team).ToHashSet();
        foreach (var unit in Unit.All(GetTree()))
        {
            if (unit == _jet || unit.Health == 0)
                continue;
            var at = ToScreen(unit.GlobalPosition);
            if (!inside.HasPoint(at))
                continue;  // out of view
            DrawMarker(at, Flat(-unit.GlobalBasis.Z).Rotated(spin), hostiles.Contains(unit) ? Hud.Red : Blue);
            if (unit == _targeting.Target)
                DrawRect(new Rect2(at - new Vector2(8, 8), 16, 16), Hud.Green, filled: false, width: 1.5f);
        }
        DrawMarker(jetAt.Clamp(inside.Position, inside.End), heading, Hud.Green);  // held at the edge when off the map
    }

    void DrawGridLine(Rect2 box, Vector3 from, Vector3 to, Color color) =>
        DrawLine(ToScreen(from).Clamp(box.Position, box.End), ToScreen(to).Clamp(box.Position, box.End), color);

    // How far from `from` along `direction` the square's edge is.
    static float ToEdge(Rect2 box, Vector2 from, Vector2 direction)
    {
        var x = direction.X > 0 ? (box.End.X - from.X) / direction.X : direction.X < 0 ? (box.Position.X - from.X) / direction.X : float.MaxValue;
        var y = direction.Y > 0 ? (box.End.Y - from.Y) / direction.Y : direction.Y < 0 ? (box.Position.Y - from.Y) / direction.Y : float.MaxValue;
        return Mathf.Max(Mathf.Min(x, y), 0);
    }

    // A small triangle at `at` pointing along `facing`, or up when the Unit faces straight up or down.
    void DrawMarker(Vector2 at, Vector2 facing, Color color)
    {
        var tip = facing.Length() < 0.01f ? Vector2.Up : facing.Normalized();
        var across = tip.Orthogonal() * 4.5f;
        DrawColoredPolygon(new[] { at + tip * 7, at - tip * 5 + across, at - tip * 5 - across }, color);
    }
}
