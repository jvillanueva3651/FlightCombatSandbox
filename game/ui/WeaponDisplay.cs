using System.Linq;
using Godot;

/// <summary>
/// The HUD's bottom-right corner: the weapons and their counts, with ">" on the selected one, and
/// the damage the jet has taken, above the jet's silhouette seen from above with a missile symbol at
/// each Hardpoint. A symbol is filled while its Hardpoint is loaded, fills from the bottom while it
/// reloads, and stays empty once the count has no missile left for it.
/// </summary>
public partial class WeaponDisplay : Control
{
    const float Margin = 30;     // pixels from the screen's right and bottom edges
    const float Width = 150;     // pixels across the rows
    const int FontSize = 18;
    const float RowHeight = 24;  // pixels from one row's baseline to the next
    const float PixelsPerMeter = 5;  // how big the silhouette is drawn

    // The gray-box jet (jet.tscn) seen from above, in its own meters: x to the right, z toward the tail.
    static readonly Vector2[] Silhouette =
    {
        new(0, -9), new(0.8f, -6), new(0.8f, -0.75f), new(5, -0.75f), new(5, 2.75f), new(0.8f, 2.75f),
        new(0.8f, 4.4f), new(2.3f, 4.4f), new(2.3f, 6), new(-2.3f, 6), new(-2.3f, 4.4f), new(-0.8f, 4.4f),
        new(-0.8f, 2.75f), new(-5, 2.75f), new(-5, -0.75f), new(-0.8f, -0.75f), new(-0.8f, -6),
    };

    // A missile symbol pointing up, in pixels around its middle: nose, body and fins.
    static readonly Vector2[] MissileSymbol =
    {
        new(0, -17), new(3, -11), new(3, 9), new(6, 14), new(6, 17), new(-6, 17), new(-6, 14), new(-3, 9), new(-3, -11),
    };
    const float BodyTop = -11, SymbolBottom = 17;  // where a reloading symbol's fill rises between
    const float SymbolNudge = 6;  // pixels a symbol sits out from its Hardpoint, so it hangs off the wing

    Jet _jet;
    MissileLauncher _launcher;

    public override void _Ready()
    {
        _jet = ((Hud)Owner).Jet;
        _launcher = _jet.GetNode<MissileLauncher>("MissileLauncher");
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        var corner = GetViewportRect().Size - new Vector2(Margin, Margin);  // the bottom right of what's drawn
        var left = corner.X - Width;

        // The silhouette, its tail at the bottom, under the middle of the rows.
        var origin = new Vector2(left + Width / 2, corner.Y - 6 * PixelsPerMeter);  // where the jet's own (0, 0) is drawn
        var outline = Silhouette.Select(point => origin + point * PixelsPerMeter).ToArray();
        DrawColoredPolygon(outline, new Color(Hud.Green, 0.25f));
        DrawPolyline(outline.Append(outline[0]).ToArray(), Hud.Green, 1.5f, antialiased: true);
        for (var i = 0; i < _launcher.Hardpoints.Count; i++)
        {
            var at = _jet.ToLocal(_launcher.Hardpoints[i].GlobalPosition);
            DrawMissile(origin + new Vector2(at.X, at.Z) * PixelsPerMeter + new Vector2(Mathf.Sign(at.X) * SymbolNudge, 0), _launcher.Readiness(i));
        }

        // The rows, above the silhouette's nose.
        var damage = Mathf.RoundToInt(100f * (_jet.MaxHealth - _jet.Health) / _jet.MaxHealth);
        var damageRow = origin.Y - 9 * PixelsPerMeter - 16;
        DrawRow(left, damageRow, "DMG", $"{damage}%");
        DrawRow(left, damageRow - RowHeight - 8, "MSL", $"{_launcher.Count}", selected: true);
        DrawRow(left, damageRow - 2 * RowHeight - 8, "GUN", "---");
    }

    // The name on the left, the value on the right, and ">" in front of the selected weapon.
    void DrawRow(float left, float baseline, string name, string value, bool selected = false)
    {
        if (selected)
            Hud.DrawText(this, new Vector2(left - 14, baseline), ">", Hud.Green, FontSize);
        Hud.DrawText(this, new Vector2(left, baseline), name, Hud.Green, FontSize);
        Hud.DrawText(this, new Vector2(left, baseline), value, Hud.Green, FontSize, HorizontalAlignment.Right, Width);
    }

    void DrawMissile(Vector2 at, float readiness)
    {
        var symbol = MissileSymbol.Select(point => at + point).ToArray();
        if (readiness >= 1)
        {
            DrawColoredPolygon(symbol, Hud.Green);
            return;
        }
        DrawPolyline(symbol.Append(symbol[0]).ToArray(), new Color(Hud.Green, 0.5f), 1.5f, antialiased: true);
        var filled = readiness * (SymbolBottom - BodyTop);
        DrawRect(new Rect2(at.X - 3, at.Y + SymbolBottom - filled, 6, filled), Hud.Green);
    }
}
