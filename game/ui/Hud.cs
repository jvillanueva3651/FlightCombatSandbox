using Godot;

/// <summary>
/// The Player's HUD, in Ace Combat's green. Its parts read the Player's jet from here and draw their
/// text with <see cref="DrawText"/>.
/// </summary>
public partial class Hud : CanvasLayer
{
    /// <summary>Ace Combat's HUD green.</summary>
    public static readonly Color Green = new(0.35f, 1, 0.45f);

    /// <summary>The HUD's red, for a lock and for hostile Units.</summary>
    public static readonly Color Red = new(1, 0.25f, 0.2f);

    /// <summary>The Player's jet.</summary>
    [Export] public Jet Jet { get; set; }

    /// <summary>
    /// Draws text for a HUD part from its _Draw, outlined in black so it reads against a bright sky.
    /// <paramref name="at"/> is the left end of the baseline, of the text or of the
    /// <paramref name="width"/> it's aligned within.
    /// </summary>
    public static void DrawText(CanvasItem part, Vector2 at, string text, Color color, int fontSize,
        HorizontalAlignment align = HorizontalAlignment.Left, float width = -1)
    {
        part.DrawStringOutline(ThemeDB.FallbackFont, at, text, align, width, fontSize, size: 4, modulate: Colors.Black);
        part.DrawString(ThemeDB.FallbackFont, at, text, align, width, fontSize, color);
    }
}
