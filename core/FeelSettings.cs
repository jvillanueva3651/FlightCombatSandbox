namespace Core;

/// <summary>
/// Settings shared by every aircraft that shape how flying feels, from the base Pack's feel.json.
/// The dead zone is a fraction of the stick's travel, and the response curve runs from 0 (straight)
/// to 1 (softest).
/// </summary>
public sealed record FeelSettings
{
    public required float DeadZone { get; set; }
    public required float ResponseCurve { get; set; }

    public static FeelSettings FromJson(string json) => PackLoader.Read<FeelSettings>(json);

    /// <summary>
    /// The stick position (-1 to 1) after the response curve: a blend of a straight line and a cube, so
    /// the middle of the stick gets finer as the curve grows while small nudges still respond, and a
    /// full push stays full.
    /// </summary>
    public float Curve(float stick) => stick * (1 - ResponseCurve + ResponseCurve * stick * stick);
}
